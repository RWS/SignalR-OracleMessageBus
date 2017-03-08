using System;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.AspNet.SignalR;

namespace Sdl.SignalR.OracleMessageBus
{
    public static class DependencyResolverExtensions
    {
        /// <summary>
        /// Use Oracle as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver.</param>
        /// <param name="connectionString">The connection string to the Oracle Server.</param>
        /// <param name="useOracleDependency">Flag to determine if Oracle dependency should be used.</param>
        /// <param name="oracleDependencyPort">The dependency port of the Oracle server.</param>
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseOracle(this IDependencyResolver resolver, string connectionString, bool useOracleDependency = false, int? oracleDependencyPort = null)
        {
            var configuration = new OracleScaleoutConfiguration(connectionString, useOracleDependency, oracleDependencyPort);

            return UseOracle(resolver, configuration);
        }

        /// <summary>
        /// Use Redis as the messaging backplane for scaling out of ASP.NET SignalR applications in a web farm.
        /// </summary>
        /// <param name="resolver">The dependency resolver</param>
        /// <param name="configuration">The Redis scale-out configuration options.</param> 
        /// <returns>The dependency resolver.</returns>
        public static IDependencyResolver UseOracle(this IDependencyResolver resolver, OracleScaleoutConfiguration configuration)
        {
            var bus = new Lazy<OracleMessageBus>(() => new OracleMessageBus(resolver, configuration));
            resolver.Register(typeof(IMessageBus), () => bus.Value);

            return resolver;
        }
    }
}
