using System;
using System.Data;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IObservableDbOperation : IDbOperation, IDisposable
    {
        event Action Queried;
        event Action Changed;
        event Action<Exception> Faulted;
        void ExecuteReaderWithUpdates(Action<IDataRecord, IDbOperation> processRecord);
    }
}