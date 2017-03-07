using System;

namespace Sdl.SignalR.OracleMessageBus
{
    [Serializable]
    public class OracleMessageBusException : Exception
    {
        public OracleMessageBusException(string message)
            : base(message)
        {

        }
    }
}