using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNet.SignalR.Messaging;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal static class OraclePayload
    {
        public static byte[] ToBytes(IList<Message> messages)
        {
            if (messages == null)
            {
                throw new ArgumentNullException("messages");
            }

            var message = new ScaleoutMessage(messages);
            return message.ToBytes();
        }

        public static ScaleoutMessage FromBytes(IDataRecord record)
        {
            var message = ScaleoutMessage.FromBytes(GetBinary(record, 1));
            return message;
        }
    
        private static byte[] GetBinary(IDataRecord reader, int ordinalIndex)
        {
            var oracleReader = reader as OracleDataReader;
            if (oracleReader == null)
            {
                throw new NotSupportedException();
            }

            return oracleReader.GetOracleBinary(ordinalIndex).Value;
        }
    }
}
