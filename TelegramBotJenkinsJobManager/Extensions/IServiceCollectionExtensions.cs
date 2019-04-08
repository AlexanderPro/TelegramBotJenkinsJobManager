﻿using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using TelegramBotJenkinsJobManager.Services;
using MihaZupan;

namespace TelegramBotJenkinsJobManager.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
        {
            var protocol = configuration.GetValue<string>("jenkins:protocol");
            var fqdn = configuration.GetValue<string>("jenkins:fqdn");
            var userName = configuration.GetValue<string>("jenkins:auth:userName");
            var token = configuration.GetValue<string>("jenkins:auth:token");
            var botToken = configuration.GetValue<string>("telegram:botToken");
            var proxyHostName = configuration.GetValue<string>("telegram:httpproxy:address");
            var proxyPort = configuration.GetValue<int?>("telegram:httpproxy:port");
            var menu = configuration.GetSection("telegram:jobMenu").Get<IList<MenuItem>>();
            var allowedChatIds = configuration.GetSection("telegram:allowedchatids").Get<IList<long>>();
            var httpProxy = (IWebProxy)null;
            var socks5Proxy = (HttpToSocks5Proxy)null;
            var address = configuration.GetValue<string>("telegram:httpproxy:address");
            var port = configuration.GetValue<int?>("telegram:httpproxy:port");
            if (address != null && port != null)
            {
                httpProxy = new WebProxy(address, port.Value);
            }
            address = configuration.GetValue<string>("telegram:socks5proxy:address");
            port = configuration.GetValue<int?>("telegram:socks5proxy:port");
            if (address != null && port != null)
            {
                socks5Proxy = new HttpToSocks5Proxy(address, port.Value, 
                    configuration.GetValue<string>("telegram:socks5proxy:userName"),
                    configuration.GetValue<string>("telegram:socks5proxy:password"));
                if (configuration.GetValue<bool?>("telegram:socks5proxy:resolveHostnamesLocally") != null)
                {
                    socks5Proxy.ResolveHostnamesLocally = configuration.GetValue<bool?>("telegram:socks5proxy:resolveHostnamesLocally").Value;
                }
            }

            services.AddScoped<IJenkinsService>(ctx => new JenkinsService(protocol, fqdn, userName, token));
            services.AddScoped(ctx => new TelegramBotClient(botToken, httpProxy ?? socks5Proxy));
            services.AddScoped<ITelegramResponseHandler>(ctx => {
                var telegramClient = ctx.GetService<TelegramBotClient>();
                var jenkinsService = ctx.GetService<IJenkinsService>();
                return new TelegramResponseHandler(telegramClient, allowedChatIds, protocol, fqdn, menu, jenkinsService);
            });

            return services;
        }
    }
}
