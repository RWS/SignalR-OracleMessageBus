using System.Data;
using System.Threading.Tasks;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class DbCommandExtensions
    {
        public static Task<int> ExecuteNonQueryAsync(this IDbCommand command)
        {
            var oracleCommand = command as OracleCommand;

            if (oracleCommand != null)
            {
                return oracleCommand.ExecuteNonQueryAsync();
            }

            return TaskAsyncHelper.FromResult(command.ExecuteNonQuery());
        }
    }
}