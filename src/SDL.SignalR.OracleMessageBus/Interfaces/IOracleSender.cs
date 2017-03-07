using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus
{
    internal interface IOracleSender
    {
        Task Send(IList<Message> messages);
    }
}