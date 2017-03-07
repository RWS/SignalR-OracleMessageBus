using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Oracle.DataAccess.Client;

namespace Sdl.SignalR.OracleMessageBus
{
    internal class OracleDependencyManager : IOracleDependencyManager
    {
        private static readonly List<ISignalRDbDependency> DependencyList = new List<ISignalRDbDependency>();
        private static readonly object Sync = new object();
        private readonly IDbProviderFactory _dbProviderFactory;
        private readonly TraceSource _traceSource;

        /*
         * How to create DbProviderFactory
         * OracleClientFactory.Instance.AsIDbProviderFactory().CreateConnection())
         *
         */
        public OracleDependencyManager(IDbProviderFactory dbProviderFactory, TraceSource traceSource)
        {
            _dbProviderFactory = dbProviderFactory;
            _traceSource = traceSource;
        }

        public void RemoveRegistration(string connectionString)
        {
            lock (Sync)
            {
                if (DependencyList.Any())
                {
                    using (IDbConnection nc = _dbProviderFactory.CreateConnection())
                    {
                        nc.ConnectionString = connectionString;
                        nc.Open();

                        foreach (ISignalRDbDependency dependency in DependencyList)
                        {
                            try
                            {
                                var oracleConnection = nc as OracleConnection;
                                dependency.RemoveRegistration(oracleConnection);
                            }
                            catch (Exception ex)
                            {
                                _traceSource.TraceError(
                                    "Error during unregistering of oracle dependency. Details: {0}", ex.Message);
                            }
                        }

                        nc.CloseConnection();

                        DependencyList.Clear();
                    }
                }
            }
        }

        public void RegisterDependency(ISignalRDbDependency dep)
        {
            lock (Sync)
            {
                DependencyList.Add(dep);
            }
        }
    }
}