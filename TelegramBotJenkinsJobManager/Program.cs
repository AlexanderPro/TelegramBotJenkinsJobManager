using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Telegram.Bot;
using TelegramBotJenkinsJobManager.Services;
using TelegramBotJenkinsJobManager.Extensions;

namespace TelegramBotJenkinsJobManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(configuration).CreateLogger();
            var polling = configuration.GetValue<bool>("telegram:polling");
            if (polling)
            {
                var serviceProvider = new ServiceCollection()
                    .AddLogging()
                    .RegisterServices(configuration)
                    .BuildServiceProvider();

                var bot = new TelegramPollingBot(serviceProvider.GetService<TelegramBotClient>(), serviceProvider.GetService<ITelegramResponseHandler>());
                bot.Start();
                var so = new object();
                while (true)
                {
                    lock (so)
                    {
                        Monitor.Wait(so);
                    }
                }
            }
            else
            {
                CreateWebHostBuilder(args).Build().Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog((hostingContext, loggerConfiguration) => loggerConfiguration.ReadFrom.Configuration(hostingContext.Configuration));
    }
}
