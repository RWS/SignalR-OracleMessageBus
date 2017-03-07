using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IDbOperation
    {
        object ExecuteScalar();
        int ExecuteNonQuery();
        int ExecuteStoredProcedure();
        Task<int> ExecuteNonQueryAsync();
        IList<IDataParameter> Parameters { get; }
        int ExecuteReader(Action<IDataRecord, IDbOperation> processRecord);
    }
}