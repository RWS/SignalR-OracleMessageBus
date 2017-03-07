using System.Data;
using System.Data.Common;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class DbProviderFactoryAdapter : IDbProviderFactory
    {
        private readonly DbProviderFactory _dbProviderFactory;

        public DbProviderFactoryAdapter(DbProviderFactory dbProviderFactory)
        {
            _dbProviderFactory = dbProviderFactory;
        }

        public IDbConnection CreateConnection()
        {
            return _dbProviderFactory.CreateConnection();
        }

        public IDataParameter CreateParameter()
        {
            return _dbProviderFactory.CreateParameter();
        }
    }
}