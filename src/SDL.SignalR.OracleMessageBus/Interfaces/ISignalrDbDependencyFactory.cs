using System.Data;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface ISignalrDbDependencyFactory
    {
        ISignalRDbDependency CreateDbDependency(IDbCommand command, bool isNotifiedOnce, long timeoutInSec,
            bool isPersistent);
    }
}