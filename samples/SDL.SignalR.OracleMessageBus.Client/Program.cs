using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;

namespace Sdl.SignalR.OracleMessageBus.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            HubConnection hubConnection = new HubConnection("http://localhost:8080/");
            var hubProxy = hubConnection.CreateHubProxy("DummyHub");
            int received = 0;
            int sent = 0;
            hubProxy.On<string>("FooBack", str =>
            {
                received++;
                if (received%1000 == 0)
                {
                    Console.WriteLine("Received: {0}", received);
                }
            });

            hubConnection.Start().Wait();

            Parallel.For(0, 10, j =>
            {
                for (int i = 0; i < 10000; i++)
                {
                    hubProxy.Invoke("Foo", i.ToString()).Wait();
                    sent++;
                    if (sent%1000 == 0)
                    {
                        Console.WriteLine("Sent: {0}", sent);
                    }
                }
            });


            Console.ReadLine();
        }
    }
}
