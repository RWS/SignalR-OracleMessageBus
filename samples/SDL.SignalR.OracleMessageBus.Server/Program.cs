using System;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Messaging;
using Microsoft.Owin.Hosting;
using Owin;

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

            string url = "http://localhost:8080/";
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

            OracleMessageBus messageBus = new OracleMessageBus(GlobalHost.DependencyResolver,
                new OracleScaleoutConfiguration("Data Source=ORA12101;User Id=kivlevcm2;Password=tridion", false));

            GlobalHost.DependencyResolver.Register(typeof(IMessageBus), () => messageBus);

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
