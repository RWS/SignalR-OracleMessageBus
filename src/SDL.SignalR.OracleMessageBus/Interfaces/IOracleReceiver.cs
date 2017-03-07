using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IOracleReceiver : IDisposable
    {
        event Action Queried;
        event Action<ulong, ScaleoutMessage> Received;
        event Action<Exception> Faulted;
        void GetLastPayloadId();
        void StartReceivingUpdatesFromDb();
    }
}