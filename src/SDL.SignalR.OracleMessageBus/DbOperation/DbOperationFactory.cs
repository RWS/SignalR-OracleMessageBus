using System.Data;
using System.Diagnostics;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class DbOperationFactory : IDbOperationFactory
    {
        public IDbOperation CreateDbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory)
        {
            return new DbOperation(connectionString, commandText, traceSource, dbProviderFactory);
        }

        public IDbOperation CreateDbOperation(string connectionString, string commandText,
            TraceSource traceSource,
            IDbProviderFactory dbProviderFactory,
            params IDataParameter[] parameters)
        {
            return new DbOperation(connectionString, commandText, traceSource, dbProviderFactory, parameters);
        }
    }
}