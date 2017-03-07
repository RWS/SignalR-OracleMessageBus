using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class OracleStream : IDisposable
    {
        private readonly int _streamIndex;
        private readonly TraceSource _trace;
        private readonly IOracleSender _sender;
        private readonly IOracleReceiver _receiver;
        private readonly string _tracePrefix;
        private readonly BlockingCollection<MessageWrapper> _messageCollection;
        private readonly CancellationTokenSource _cts;

        public OracleStream(int streamIndex,
                            string connectionString,
                            bool useOracleDependency,
                            TraceSource traceSource,
                            IDbProviderFactory dbProviderFactory,
                            IDbOperationFactory dbOperationFactory,
                            IObservableDbOperationFactory observableDbOperationFactory)
        {
            _streamIndex = streamIndex;
            _trace = traceSource;
            _tracePrefix = string.Format(CultureInfo.InvariantCulture, "Stream {0} : ", _streamIndex);
            _cts = new CancellationTokenSource();

            _messageCollection = new BlockingCollection<MessageWrapper>();
            _sender = new OracleSender(connectionString, _trace, dbProviderFactory, dbOperationFactory);
            _receiver = new OracleReceiver(connectionString, useOracleDependency, _trace, dbProviderFactory, dbOperationFactory, observableDbOperationFactory);
            _receiver.Queried += () =>
            {
                if (Queried != null)
                {
                    Queried();
                }
            };
            _receiver.Faulted += ex =>
            {
                if (Faulted != null)
                {
                    Faulted(ex);
                }
            };
            _receiver.Received += OnReceived;
            StartReceiving(_cts.Token);
        }

        public event Action Queried;
        public event Action<Exception> Faulted;
        public event Action<ulong, ScaleoutMessage> Received;

        public void GetLastPayloadId()
        {
            _receiver.GetLastPayloadId();
        }

        public void StartReceivingUpdatesFromDb()
        {
            _receiver.StartReceivingUpdatesFromDb();
        }

        public Task Send(IList<Message> messages)
        {
            _trace.TraceVerbose("{0}Saving payload with {1} messages(s) to Oracle", _tracePrefix, messages.Count);
            return _sender.Send(messages);
        }

        public void Dispose()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }

            _trace.TraceInformation("{0}Disposing stream {1}", _tracePrefix, _streamIndex);
            _receiver.Dispose();
        }

        private void OnReceived(ulong id, ScaleoutMessage messages)
        {
            _messageCollection.Add(new MessageWrapper((long)id, messages));
        }

        private void StartReceiving(CancellationToken token)
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    try
                    {
                        MessageWrapper message = _messageCollection.Take(token);
                        if (Received != null)
                        {
                            Received((ulong)message.Id, message.Message);
                        }
                    }
                    catch (TaskCanceledException) { } // do not need to catch, because it is an expected error during shutdown.
                }
            }, TaskCreationOptions.LongRunning);
        }
    }
}
