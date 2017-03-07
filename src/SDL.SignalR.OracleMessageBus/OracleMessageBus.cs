using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR.Tracing;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    public class OracleMessageBus : ScaleoutMessageBus
    {
        private readonly string _connectionString;
        private readonly TraceSource _trace;
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly List<OracleStream> _streams = new List<OracleStream>();
        private readonly bool _useOracleDependency;
        private readonly IDbOperationFactory _dbOperationFactory;
        private readonly IObservableDbOperationFactory _observableDbOperationFactory;

        private bool _retryConnecting = true;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleMessageBus"/> class.
        /// </summary>
        /// <param name="resolver">The resolver to use.</param>
        /// <param name="configuration">The Oracle scale-out configuration options.</param>
        public OracleMessageBus(IDependencyResolver resolver, 
                                OracleScaleoutConfiguration configuration)
            : base(resolver, configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            if (configuration.UseOracleDependency && configuration.OracleDependencyPort.HasValue)
            {
                OracleDependency.Port = configuration.OracleDependencyPort.Value;
            }

            _connectionString = configuration.ConnectionString;
            _dbProviderFactory = OracleClientFactory.Instance.AsIDbProviderFactory(configuration.ConnectionString);

            var traceManager = resolver.Resolve<ITraceManager>();
            _trace = traceManager[typeof(OracleMessageBus).Name];

            _dbOperationFactory = new DbOperationFactory();
            _observableDbOperationFactory = new ObservableDbOperationFactory(null, new OracleDependencyManager(_dbProviderFactory, _trace),
                new SignalrDbDependencyFactory());
            _useOracleDependency = configuration.UseOracleDependency;

            Task.Run(() => Initialize());
        }

        /// <summary>
        /// Sends messages to the backplane.
        /// </summary>
        protected override Task Send(int streamIndex, IList<Message> messages)
        {
            return _streams[streamIndex].Send(messages);
        }

        protected override void Dispose(bool disposing)
        {
            _trace.TraceInformation("Oracle message bus disposing, disposing receiver");
            _cts.Cancel();
            for (var i = 0; i < _streams.Count; i++)
            {
                _streams[i].Dispose();
            }

            base.Dispose(disposing);
        }

        private void Initialize()
        {
            _trace.TraceInformation("Oracle message bus initializing");

            try
            {
                var installer = new OracleInstaller(_connectionString, _trace, _dbProviderFactory, _dbOperationFactory);
                installer.Install();
            }
            catch (Exception ex)
            {
                for (int i = 0; i < StreamCount; i++)
                {
                    OnError(0, ex);
                }

                _trace.TraceError("Error trying to install Oracle objects: {0}", ex);
            }

            for (int i = 0; i < StreamCount; i++)
            {
                int streamIndex = i;
                var stream = new OracleStream(streamIndex, _connectionString, _useOracleDependency, _trace,
                    _dbProviderFactory, _dbOperationFactory, _observableDbOperationFactory);

                _streams.Add(stream);

                stream.Queried += () => Open(streamIndex);
                stream.Faulted += ex => OnError(streamIndex, ex);
                stream.Received += (id, message) => OnReceived(streamIndex, id, message);

                StartReceiving(streamIndex);
            }
        }

        private void StartReceiving(int streamIndex)
        {
            Task.Run(() =>
            {
                if (!_retryConnecting)
                {
                    return;
                }

                _streams[streamIndex].GetLastPayloadId();
            })
            .Then(() =>
            {
                Open(streamIndex);
            })
            .ContinueWith(task =>
            {
                if (task.IsCompleted && !task.IsFaulted)
                {
                    _streams[streamIndex].StartReceivingUpdatesFromDb();
                }
                else
                {
                    task.Catch(ex =>
                    {
                        OracleException oracleException = ex.InnerException as OracleException;
                        // In case of "ORA-01017: invalid username/password; logon denied" we try ones.
                        // In case of another error we keep trying to start polling database.
                        if (oracleException != null && oracleException.Errors != null && oracleException.Errors.Count > 0 && oracleException.Errors[0].Number == 1017)
                        {
                            _retryConnecting = false;
                        }

                        OnError(streamIndex, ex);
                        Task.Delay(TimeSpan.FromSeconds(2)).Wait();
                        StartReceiving(streamIndex);
                    });
                }
            }, TaskContinuationOptions.LongRunning);
        }
    }
}