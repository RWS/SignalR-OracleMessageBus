using System;
using System.Data;
using System.Diagnostics;
using Microsoft.AspNet.SignalR.Messaging;
using Oracle.ManagedDataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class OracleReceiver : IOracleReceiver
    {
        public event Action Queried;
        public event Action<ulong, ScaleoutMessage> Received;
        public event Action<Exception> Faulted;

        private readonly string _connectionString;
        private readonly TraceSource _traceSource;
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly IDbOperationFactory _dbOperationFactory;
        private readonly IObservableDbOperationFactory _observableDbOperationFactory;

        private string _maxIdSql = "SIGNALR.PAYLOADID_GET";
        private string _selectSql = "SIGNALR.PAYLOAD_READ";
        private IObservableDbOperation _observableDbOperation;
        private volatile bool _disposed;
        private long? _lastPayloadId;
        private readonly bool _useOracleDependency;

        public OracleReceiver(string connectionString,
            bool useOracleDependency,
            TraceSource traceSource,
            IDbProviderFactory dbProviderFactory,
            IDbOperationFactory dbOperationFactory,
            IObservableDbOperationFactory observableDbOperationFactory)
        {
            _connectionString = connectionString;
            _useOracleDependency = useOracleDependency;
            _traceSource = traceSource;
            _dbProviderFactory = dbProviderFactory;
            _dbOperationFactory = dbOperationFactory;
            _observableDbOperationFactory = observableDbOperationFactory;
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_observableDbOperation != null)
                {
                    _observableDbOperation.Dispose();
                }

                _disposed = true;
                _traceSource.TraceInformation("OracleReceiver disposed");
            }
        }

        public void GetLastPayloadId()
        {
            if (!_lastPayloadId.HasValue)
            {
                IDataParameter parameter = _dbProviderFactory.CreateParameter();
                parameter.DbType = DbType.Int64;
                parameter.Direction = ParameterDirection.InputOutput;
                parameter.ParameterName = "oPayloadId";

                IDbOperation lastPayloadIdOperation = _dbOperationFactory.CreateDbOperation(_connectionString, _maxIdSql,
                    _traceSource, _dbProviderFactory, parameter);

                try
                {
                    lastPayloadIdOperation.ExecuteStoredProcedure();
                    _lastPayloadId = (long?) parameter.Value;

                    if (Queried != null)
                    {
                        Queried();
                    }

                    _traceSource.TraceVerbose("OracleReceiver started, initial payload id={0}", _lastPayloadId);
                }
                catch (Exception ex)
                {
                    if (Faulted != null)
                    {
                        Faulted(ex);
                    }

                    _traceSource.TraceError("OracleReceiver error starting: {0}", ex);

                    throw;
                }
            }
        }

        public void StartReceivingUpdatesFromDb()
        {
            lock (this)
            {
                if (_disposed)
                {
                    return;
                }

                IDataParameter parameter = _dbProviderFactory.CreateParameter();
                parameter.ParameterName = "iPayloadId";
                parameter.Value = _lastPayloadId;
                parameter.DbType = DbType.Decimal;
                parameter.Direction = ParameterDirection.Input;

                OracleParameter resultSetParameter = _dbProviderFactory.CreateParameter() as OracleParameter;
                if (resultSetParameter != null)
                {
                    resultSetParameter.ParameterName = "oRefCur";
                    resultSetParameter.OracleDbType = OracleDbType.RefCursor;
                    resultSetParameter.Direction = ParameterDirection.Output;
                }

                _observableDbOperation = _observableDbOperationFactory.ObservableDbOperation(_connectionString,
                    _selectSql, _useOracleDependency, _traceSource, _dbProviderFactory, parameter, resultSetParameter);
            }

            _observableDbOperation.Queried += () =>
            {
                if (Queried != null)
                {
                    Queried();
                }
            };

            _observableDbOperation.Faulted += ex =>
            {
                if (Faulted != null)
                {
                    Faulted(ex);
                }
            };

            _observableDbOperation.Changed += () =>
            {
                _traceSource.TraceInformation("Starting receive loop again to process updates");
                _observableDbOperation.ExecuteReaderWithUpdates(ProcessRecord);
            };

            _traceSource.TraceVerbose("Executing receive reader, initial payload ID parameter={0}",
                _observableDbOperation.Parameters[0].Value);
            _observableDbOperation.ExecuteReaderWithUpdates(ProcessRecord);
            _traceSource.TraceInformation("OracleReceiver.GetLastPayloadId returned");
        }

        private void ProcessRecord(IDataRecord record, IDbOperation dbOperation)
        {
            long id = record.GetInt64(0);
            ScaleoutMessage message = OraclePayload.FromBytes(record);

            _traceSource.TraceVerbose("OracleReceiver last payload ID={0}, new payload ID={1}", _lastPayloadId, id);

            if (id > _lastPayloadId + 1)
            {
                _traceSource.TraceError("Missed message(s) from Oracle. Expected payload ID {0} but got {1}.", _lastPayloadId + 1, id);
            }
            else if (id <= _lastPayloadId)
            {
                _traceSource.TraceInformation("Duplicate message(s) or payload ID reset from Oracle. Last payload ID {0}, this payload ID {1}", _lastPayloadId, id);
            }

            _lastPayloadId = id;
            dbOperation.Parameters[0].Value = _lastPayloadId;
            
            _traceSource.TraceVerbose("Updated receive reader initial payload ID parameter={0}", _observableDbOperation.Parameters[0].Value);
            _traceSource.TraceVerbose("Payload {0} containing {1} message(s) received", id, message.Messages.Count);

            if (Received != null)
            {
                Received((ulong) id, message);
            }
        }
    }
}
