using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using System;

namespace Sdl.SignalR.OracleMessageBus.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine(sender);
                Console.WriteLine(eventArgs);
            };

            string url = "http://localhost:8888/";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("CME Server running at {0}", url);
                Console.ReadLine();
            }


        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var hubConfiguration = new HubConfiguration
            {
                EnableDetailedErrors = true
            };

            GlobalHost.DependencyResolver.UseOracle("Data Source=ORA12101;User Id=testschema;Password=test123");
            app.MapSignalR(hubConfiguration);
        }
    }

    public class DummyHub : Hub
    {
        public void Foo(string msg)
        {
            Clients.All.FooBack(msg);
        }
    }
}
