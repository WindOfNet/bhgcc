using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace bhgcc
{
    public class App : IHostedService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<App> logger;
        private readonly ILogger<Worker> workerLogger;
        private readonly IBahaService bahaService;
        private readonly ILineNotifyService lineNotifyService;
        private ManualResetEvent manualResetEvent = new ManualResetEvent(false);

        public App(IConfiguration configuration, ILogger<App> logger, ILogger<Worker> workerLogger, IBahaService bahaService, ILineNotifyService lineNotifyService)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.workerLogger = workerLogger;
            this.bahaService = bahaService;
            this.lineNotifyService = lineNotifyService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var appsettings = configuration.Get<Appsettings>();
            var workers = appsettings.Worker;
            foreach (var setting in workers)
            {
                var worker = new Worker(bahaService, lineNotifyService, setting, workerLogger, appsettings.Cycle, appsettings.LineNotifyToken);
                worker.Start();
            }

            Console.CancelKeyPress += (sender, e) =>
                manualResetEvent.Set();

            manualResetEvent.WaitOne();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
