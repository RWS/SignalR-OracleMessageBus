using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class OracleSender : IOracleSender
    {
        private readonly string _connectionString;
        private readonly string _insertSql;
        private readonly TraceSource _traceSource;
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly IDbOperationFactory _dbOperationFactory;

        public OracleSender(string connectionString,
            TraceSource traceSource,
            IDbProviderFactory dbProviderFactory,
            IDbOperationFactory dbOperationFactory)
        {
            _connectionString = connectionString;
            _insertSql = "SIGNALR.PAYLOAD_INSERT";
            _traceSource = traceSource;
            _dbProviderFactory = dbProviderFactory;
            _dbOperationFactory = dbOperationFactory;
        }

        public Task Send(IList<Message> messages)
        {
            if (messages == null || messages.Count == 0)
            {
                return MakeEmptyTask();
            }

            IDataParameter parameter = _dbProviderFactory.CreateParameter();
            parameter.ParameterName = "iPayload";
            parameter.Value = OraclePayload.ToBytes(messages);

            OracleParameter oracleParameter = parameter as OracleParameter;
            if (oracleParameter != null)
            {
                oracleParameter.OracleDbType = OracleDbType.Blob;
            }

            var operation = _dbOperationFactory.CreateDbOperation(_connectionString, _insertSql, _traceSource, _dbProviderFactory, parameter);
            return operation.ExecuteNonQueryAsync();
        }

        private Task MakeEmptyTask()
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetResult(null);
            return tcs.Task;
        }
    }
}
