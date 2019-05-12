using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using TelegramBotJenkinsJobManager.Extensions;
using Serilog;

namespace TelegramBotJenkinsJobManager
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            var polling = configuration.GetValue<bool>("telegram:polling");
            if (polling)
            {
                await new HostBuilder()
                      .ConfigureServices((hostContext, services) =>
                      {
                          services.RegisterSerilog(configuration);
                          services.RegisterServices(configuration);
                      })
                      .RunConsoleAsync();
            }
            else
            {
                await WebHost.CreateDefaultBuilder(args)
                      .UseStartup<Startup>()
                      .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration))
                      .Build()
                      .RunAsync();
            }
        }
    }
}
