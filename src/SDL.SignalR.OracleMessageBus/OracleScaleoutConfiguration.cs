using System;
using Microsoft.AspNet.SignalR.Messaging;

namespace Sdl.SignalR.OracleMessageBus
{
    /// <summary>
    /// Settings for the Oracle scale-out message bus implementation.
    /// </summary>
    public class OracleScaleoutConfiguration : ScaleoutConfiguration
    {
        public OracleScaleoutConfiguration(string connectionString)
            : this(connectionString, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OracleScaleoutConfiguration"/> class.
        /// </summary>
        public OracleScaleoutConfiguration(string connectionString, bool useOracleDependency)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException("connectionString");
            }

            ConnectionString = connectionString;
            UseOracleDependency = useOracleDependency;
        }

        public OracleScaleoutConfiguration(string connectionString, int? oracleDependencyPort = null)
            : this(connectionString, true)
        {
            OracleDependencyPort = oracleDependencyPort;
        }

        /// <summary>
        /// The Oracle connection string to use.
        /// </summary>
        public string ConnectionString
        {
            get;
            private set;
        }

        /// <summary>
        /// Specifies to use Oracle Query Notification mechanism after the certain amount
        /// of attempts to get new messages from database has been made and do not use polling.
        /// </summary>
        public bool UseOracleDependency
        {
            get;
            private set;
        }

        /// <summary>
        /// Indicates the port number that the notification listener listens on, for database notifications.
        /// </summary>
        public int? OracleDependencyPort
        {
            get;
            private set;
        }
    }
}
