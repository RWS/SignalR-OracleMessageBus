using System.Data;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IDbProviderFactory
    {
        IDbConnection CreateConnection();
        IDataParameter CreateParameter();
    }
}