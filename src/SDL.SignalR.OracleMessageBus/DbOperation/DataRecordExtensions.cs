using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class DataRecordExtensions
    {
        public static byte[] GetBinary(this IDataRecord reader, int ordinalIndex)
        {
            var oracleDataReader = reader as OracleDataReader;
            if (oracleDataReader == null)
            {
                throw new NotSupportedException();
            }

            return oracleDataReader.GetBinary(ordinalIndex);
        }
    }
}