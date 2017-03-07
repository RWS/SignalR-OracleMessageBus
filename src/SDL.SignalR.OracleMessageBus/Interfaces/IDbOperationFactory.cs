using System.Data;
using System.Diagnostics;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IDbOperationFactory
    {
        IDbOperation CreateDbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory);

        IDbOperation CreateDbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory, params IDataParameter[] parameters);
    }
}