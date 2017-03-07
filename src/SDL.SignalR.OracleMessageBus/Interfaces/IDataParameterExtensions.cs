using System.Data;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class DataParameterExtensions
    {
        public static IDataParameter Clone(this IDataParameter sourceParameter, IDbProviderFactory dbProviderFactory)
        {
            IDataParameter newParameter = dbProviderFactory.CreateParameter();

            newParameter.ParameterName = sourceParameter.ParameterName;
            newParameter.DbType = sourceParameter.DbType;
            newParameter.Value = sourceParameter.Value;
            newParameter.Direction = sourceParameter.Direction;

            var oracleSourceParameter = sourceParameter as OracleParameter;
            var oracleNewParameter = newParameter as OracleParameter;
            if (oracleNewParameter != null && oracleSourceParameter != null)
            {
                oracleNewParameter.OracleDbType = oracleSourceParameter.OracleDbType;
            }

            return newParameter;
        }
    }
}