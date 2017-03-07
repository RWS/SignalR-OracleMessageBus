using System.Data;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class DbConnectionExtension
    {
        public static void CloseConnection(this IDbConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }
    }
}