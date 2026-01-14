using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameAssistant.Worker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}
