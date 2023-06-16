using bhgcc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

try
{
    Log.Information("APP start.");
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
        .UseSerilog()
        .Build()
        .Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}