using System.Diagnostics;
using System.IO;
using System.Transactions;
using IsolationLevel = System.Transactions.IsolationLevel;
using System.Data.Common;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class OracleInstaller
    {
        private const string InstallScript = "install.sql";
        private readonly string _connectionString;
        private readonly TraceSource _trace;
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly IDbOperationFactory _dbOperationFactory;

        public OracleInstaller(string connectionString, TraceSource traceSource, IDbProviderFactory dbProviderFactory,
            IDbOperationFactory dbOperationFactory)
        {
            _connectionString = connectionString;
            _trace = traceSource;
            _dbProviderFactory = dbProviderFactory;
            _dbOperationFactory = dbOperationFactory;
        }

        public void Install()
        {
            _trace.TraceInformation("Start installing SignalR Oracle objects");

            string script = null;
            using (var resourceStream = GetType().Assembly.GetManifestResourceStream(string.Concat(GetType().Namespace, ".", InstallScript)))
            {
                var reader = new StreamReader(resourceStream);
                script = reader.ReadToEnd();
            }

            DbConnectionStringBuilder builder = new DbConnectionStringBuilder();
            builder.ConnectionString = _connectionString;
            string userId = builder["User Id"].ToString();
            script = script.Replace("TABLESPACE &TBLSPC_TDS_TABLES", string.Empty)
                           .Replace("&DBOWNER", userId.ToUpperInvariant());

            using (
                new TransactionScope(TransactionScopeOption.Required,
                    new TransactionOptions {IsolationLevel = IsolationLevel.Serializable}))
            {
                var command = _dbOperationFactory.CreateDbOperation(_connectionString, script, _trace, _dbProviderFactory);
                command.ExecuteNonQuery();
            }

            _trace.TraceInformation("SignalR Oracle objects installed");
        }
    }
}
