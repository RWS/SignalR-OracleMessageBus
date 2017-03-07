namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IOracleDependencyManager
    {
        void RemoveRegistration(string connectionString);
        void RegisterDependency(ISignalRDbDependency dependency);
    }
}