using System.Data;
using System.Diagnostics;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class ObservableDbOperationFactory : IObservableDbOperationFactory
    {
        private readonly IDbBehavior _dbBehavior;
        private readonly IOracleDependencyManager _oracleDependencyManager;
        private readonly ISignalrDbDependencyFactory _signalrDbDependencyFactory;

        public ObservableDbOperationFactory(
            IDbBehavior dbBehavior,
            IOracleDependencyManager oracleDependencyManager,
            ISignalrDbDependencyFactory signalrDbDependencyFactory)

        {
            _dbBehavior = dbBehavior;
            _oracleDependencyManager = oracleDependencyManager;
            _signalrDbDependencyFactory = signalrDbDependencyFactory;
        }

        public IObservableDbOperation ObservableDbOperation(
            string connectionString,
            string commandText,
            bool useOracleDependency,
            TraceSource traceSource,
            IDbProviderFactory dbProviderFactory,
            params IDataParameter[] parameters)
        {
            return new ObservableDbOperation(connectionString, commandText, traceSource,
                _dbBehavior,
                dbProviderFactory,
                _oracleDependencyManager,
                _signalrDbDependencyFactory,
                useOracleDependency,
                parameters);
        }
    }
}
