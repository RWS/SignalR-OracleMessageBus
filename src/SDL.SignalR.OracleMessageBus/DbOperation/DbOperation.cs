using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class DbOperation : IDbOperation
    {
        public IList<IDataParameter> Parameters
        {
            get { return _parameters; }
        }

        protected TraceSource Trace { get; private set; }
        protected string ConnectionString { get; private set; }
        protected string CommandText { get; private set; }

        private readonly List<IDataParameter> _parameters = new List<IDataParameter>();
        private readonly IDbProviderFactory _dbProviderFactory;

        public DbOperation(string connectionString, string commandText, TraceSource traceSource, IDbProviderFactory dbProviderFactory)
        {
            ConnectionString = connectionString;
            CommandText = commandText;
            Trace = traceSource;
            _dbProviderFactory = dbProviderFactory;
        }

        public DbOperation(string connectionString, string commandText, TraceSource traceSource,
            IDbProviderFactory dbProviderFactory, params IDataParameter[] parameters) :
            this(connectionString, commandText, traceSource, dbProviderFactory)
        {
            if (parameters != null)
            {
                _parameters.AddRange(parameters);
            }
        }

        public object ExecuteScalar()
        {
            return Execute(cmd => cmd.ExecuteScalar());
        }

        public int ExecuteNonQuery()
        {
            return Execute(cmd => cmd.ExecuteNonQuery());
        }

        public int ExecuteStoredProcedure()
        {
            return Execute(cmd => cmd.ExecuteNonQuery(), CommandType.StoredProcedure, false);
        }

        public Task<int> ExecuteNonQueryAsync()
        {
            var tcs = new TaskCompletionSource<int>();
            Execute(cmd => cmd.ExecuteNonQueryAsync(), tcs, CommandType.StoredProcedure);
            return tcs.Task;
        }

        public int ExecuteReader(Action<IDataRecord, IDbOperation> processRecord)
        {
            return ExecuteReader(processRecord, null);
        }

        #region [ private ]

        protected virtual int ExecuteReader(Action<IDataRecord, DbOperation> processRecord, Action<IDbCommand> commandAction)
        {
            return Execute(cmd =>
            {
                if (commandAction != null)
                {
                    commandAction(cmd);
                }

                var reader = cmd.ExecuteReader();
                var count = 0;

                while (reader.Read())
                {
                    count++;
                    processRecord(reader, this);
                }

                return count;
            }, CommandType.StoredProcedure);
        }

        private T Execute<T>(Func<IDbCommand, T> commandFunc, CommandType commandType = CommandType.Text, bool cloneParameters = true)
        {
            T result = default(T);
            IDbConnection connection = null;

            try
            {
                connection = _dbProviderFactory.CreateConnection();
                connection.ConnectionString = ConnectionString;
                var command = CreateCommand(connection, cloneParameters);
                command.CommandType = commandType;
                connection.Open();
                TraceCommand(command);
                result = commandFunc(command);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error during connection open. Details: {0}", ex.ToString());
                throw;
            }
            finally
            {
                if (connection != null)
                {
                    connection.CloseConnection();
                    connection.Dispose();
                }
            }

            return result;
        }

        private void Execute<T>(Func<IDbCommand, Task<T>> commandFunc, TaskCompletionSource<T> tcs, CommandType commandType)
        {
            IDbConnection connection = null;
            try
            {
                connection = _dbProviderFactory.CreateConnection();
                connection.ConnectionString = ConnectionString;
                var command = CreateCommand(connection);
                command.CommandType = commandType;

                connection.Open();

                commandFunc(command)
                    .Then(result => tcs.SetResult(result))
                    .Catch((exception, o) =>
                    {
                        tcs.SetUnwrappedException(exception);

                    }, Trace)
                    .Finally(state =>
                    {
                        var conn = (DbConnection)state;
                        if (conn != null)
                        {
                            conn.CloseConnection();
                            conn.Dispose();
                        }
                    }, connection);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Error during connection open. Details: {0}", ex.ToString());

                if (connection != null)
                {
                    connection.CloseConnection();
                    connection.Dispose();
                }

                throw;
            }
        }

        protected virtual IDbCommand CreateCommand(IDbConnection connection, bool cloneParameters = true)
        {
            var command = connection.CreateCommand();
            command.CommandText = CommandText;

            if (Parameters != null && Parameters.Count > 0)
            {
                for (var i = 0; i < Parameters.Count; i++)
                {
                    command.Parameters.Add(cloneParameters ? Parameters[i].Clone(_dbProviderFactory) : Parameters[i]);
                }
            }

            return command;
        }

        private void TraceCommand(IDbCommand command)
        {
            if (Trace.Switch.ShouldTrace(TraceEventType.Verbose))
            {
                Trace.TraceVerbose("Created DbCommand: CommandType={0}, CommandText={1}, Parameters={2}", command.CommandType, command.CommandText,
                    command.Parameters.Cast<IDataParameter>()
                        .Aggregate(string.Empty, (msg, p) => string.Format(CultureInfo.InvariantCulture, "{0} [Name={1}, Value={2}]", msg, p.ParameterName, p.Value))
                );
            }
        }

        #endregion
    }
}