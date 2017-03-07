using System.Data.Common;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class DbProviderFactoryExtensions
    {
        public static IDbProviderFactory AsIDbProviderFactory(this DbProviderFactory dbProviderFactory, string connectionString)
        {
            using (OracleConnection oc = new OracleConnection(connectionString))
            {
                return new DbProviderFactoryAdapter(DbProviderFactories.GetFactory(oc));
            }
        }
    }
}