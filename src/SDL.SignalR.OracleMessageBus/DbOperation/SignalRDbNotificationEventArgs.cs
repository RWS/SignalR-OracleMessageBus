using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class SignalRDbNotificationEventArgs : ISignalRDbNotificationEventArgs
    {
        public int NotificationType { get { return (int) _e.Type; } }
        public int NotificationInfo { get { return (int) _e.Info; }  }
        public int NotificationSource { get { return (int) _e.Source; } }

        private readonly OracleNotificationEventArgs _e;

        public SignalRDbNotificationEventArgs(OracleNotificationEventArgs e)
        {
            _e = e;
        }
    }
}