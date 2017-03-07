using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class MessageWrapper
    {
        public MessageWrapper(long id, ScaleoutMessage message)
        {
            Id = id;
            Message = message;
        }

        public long Id { get; set; }
        public ScaleoutMessage Message { get; set; }
    }
}