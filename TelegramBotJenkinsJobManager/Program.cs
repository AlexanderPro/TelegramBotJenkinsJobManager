using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using TelegramBotJenkinsJobManager.Services;
using TelegramBotJenkinsJobManager.Extensions;

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
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            var polling = configuration.GetValue<bool>("telegram:polling");
            if (polling)
            {
                await new HostBuilder()
                      .ConfigureServices((hostContext, services) =>
                      {
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
