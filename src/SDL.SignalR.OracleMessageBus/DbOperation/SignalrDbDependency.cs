using System;
using Oracle.ManagedDataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class SignalrDbDependency : ISignalRDbDependency
    {
        private OracleDependency _dep;
        public SignalrDbDependency(OracleDependency dep)
        {
            _dep = dep;

            _dep.OnChange += (sender, args) =>
            {
                if (OnChanged != null)
                {
                    OnChanged(sender, new SignalRDbNotificationEventArgs(args));
                }
            };
        }

        public event EventHandler<ISignalRDbNotificationEventArgs> OnChanged;

        public void RemoveRegistration(OracleConnection conn)
        {
            _dep.RemoveRegistration(conn);
        }
    }
}