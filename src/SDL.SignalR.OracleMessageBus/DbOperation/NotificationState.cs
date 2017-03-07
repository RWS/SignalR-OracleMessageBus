namespace Sdl.SignalR.OracleMessageBus
{
    internal static class NotificationState
    {
        public const long Enabled = 0;
        public const long ProcessingUpdates = 1;
        public const long AwaitingNotification = 2;
        public const long NotificationReceived = 3;
        public const long Disabled = 4;
    }
}