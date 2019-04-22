using System;
using Oracle.ManagedDataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface ISignalRDbDependency
    {
        event EventHandler<ISignalRDbNotificationEventArgs> OnChanged;
        void RemoveRegistration(OracleConnection conn);
    }
}