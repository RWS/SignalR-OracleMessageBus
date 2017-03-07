using System;
using System.Data;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class SignalrDbDependencyFactory : ISignalrDbDependencyFactory
    {
        public ISignalRDbDependency CreateDbDependency(IDbCommand command, bool isNotifiedOnce, long timeoutInSec, bool isPersistent)
        {
            var oracleCommand = command as OracleCommand;
            if (oracleCommand == null)
            {
                throw new NotSupportedException();
            }

            OracleDependency od = new OracleDependency(oracleCommand, isNotifiedOnce, timeoutInSec, isPersistent);
            return new SignalrDbDependency(od);
        }
    }
}