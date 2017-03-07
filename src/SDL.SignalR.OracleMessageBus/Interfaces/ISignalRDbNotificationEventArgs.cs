namespace Sdl.SignalR.OracleMessageBus
{
    internal interface ISignalRDbNotificationEventArgs
    {
        int NotificationType { get; }
        int NotificationInfo { get; }
        int NotificationSource {get; }
    }
}