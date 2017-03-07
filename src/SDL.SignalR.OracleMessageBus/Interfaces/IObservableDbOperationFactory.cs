using System.Data;
using System.Diagnostics;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IObservableDbOperationFactory
    {
        IObservableDbOperation ObservableDbOperation(
            string connectionString,
            string commandText,
            bool useOracleDependency,
            TraceSource traceSource,
            IDbProviderFactory dbProviderFactory,
            params IDataParameter[] parameters);
    }
}