using System;
using System.Collections.Generic;
using System.Data;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IDbBehavior
    {
        IList<Tuple<int, int>> UpdateLoopRetryDelays { get; }
        void AddOracleDependency(IDbCommand command, Action<ISignalRDbNotificationEventArgs> callback);
    }
}