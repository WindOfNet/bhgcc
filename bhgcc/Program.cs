using bhgcc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;

Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, builder) =>
    {
        builder.AddUserSecrets<Program>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddHostedService<App>();
        services.AddTransient<IBahaService, BahaService>();
        services.AddTransient<ILineNotifyService, LineNotifyService>();
        services.AddHttpClient("baha", x =>
        {
            x.BaseAddress = new Uri("https://forum.gamer.com.tw");
            x.DefaultRequestHeaders.Add("User-Agent", "bhgcc");
        });
        services.AddHttpClient("line-notify", x =>
        {
            x.BaseAddress = new Uri("https://notify-api.line.me");
        });
    })
    .UseConsoleLifetime()
    .Build()
    .Run();