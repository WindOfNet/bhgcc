using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;

namespace bhgcc
{
    public class Worker
    {
        private readonly IBahaService bahaService;
        private readonly ILineNotifyService lineNotifyService;
        private readonly ILogger logger;
        private readonly int cycle;
        private readonly string lineToken;

        public WorkerSetting WorkerSetting { get; }
        public string CrawlTargetUrl { get; private set; }
        public string LastCrawledResult { get; private set; }

        public Worker(IBahaService bahaService, ILineNotifyService lineNotifyService, WorkerSetting workerSetting, ILogger logger, int cycle, string lineToken)
        {
            this.bahaService = bahaService;
            this.lineNotifyService = lineNotifyService;
            this.WorkerSetting = workerSetting;
            this.logger = logger;
            this.cycle = cycle;
            this.lineToken = lineToken;
        }

        public void Start()
        {
            string workerName = this.WorkerSetting.Name;

            var timer = new Timer();
            timer.AutoReset = true;
            timer.Enabled = true;
            timer.Elapsed += new ElapsedEventHandler(async (sender, e) =>
            {
                timer.Interval = cycle;

                if (this.CrawlTargetUrl == null)
                {
                    this.CrawlTargetUrl = await bahaService.GetCrawlTargetUrl(this.WorkerSetting.BoardCode, this.WorkerSetting.SearchTitle);
                    logger.LogInformation($"{workerName}, target url: { $"{bahaService.BaseUrl}{this.CrawlTargetUrl}" ?? "__NO TARGET__"}");
                }

                if (this.CrawlTargetUrl == null)
                {
                    return;
                }

                try
                {
                    var html = await bahaService.GetHtml(this.CrawlTargetUrl);
                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(html);

                    var isLock = htmlDoc.DocumentNode
                                        .SelectSingleNode("//div[contains(@class, 'c-quick-reply')]") == null;

                    if (isLock)
                    {
                        logger.LogWarning($"※※※※※※※※ the topic ({this.CrawlTargetUrl}) is locked, will re-get in next time ※※※※※※※※");
                        this.CrawlTargetUrl = null;
                        return;
                    }

                    var contentNode =
                        htmlDoc.DocumentNode
                        .SelectSingleNode("//div[@id='BH-master']")
                        .SelectNodes("section[@class='c-section']")
                        .Where(q => q.Attributes["id"] != null).Last()
                        .SelectSingleNode(".//div[@class='c-article__content']");

                    var text = WebUtility.HtmlDecode(contentNode?.InnerText);

                    if (this.LastCrawledResult != null
                        && text != null
                        && text != this.LastCrawledResult
                        && (this.WorkerSetting.Keywords?.Any(q => text.Contains(q)) ?? false))
                    {
                        var detectKeyowrds = this.WorkerSetting.Keywords.Where(x => text.Contains(x));
                        logger.LogInformation($"detect keyword [{string.Join(", ", detectKeyowrds)}], send line notify ...");

                        var swapOut = Regex.Match(text, "【想?(換出|脫手)的遊戲】：.*?【", RegexOptions.Singleline).Value;
                        var swapIn = Regex.Match(text, "【想?(換得|入手)的遊戲】：.*?【", RegexOptions.Singleline).Value;
                        var thePoint = $"換出: {(detectKeyowrds.Any(x => swapOut.Contains(x)) ? string.Join(", ", detectKeyowrds.Where(x => swapOut.Contains(x))) : "-")}" +
                                        ", " +
                                       $"換得: {(detectKeyowrds.Any(x => swapIn.Contains(x)) ? string.Join(", ", detectKeyowrds.Where(x => swapIn.Contains(x))) : "-")}";
                        var title = WebUtility.HtmlDecode(htmlDoc.DocumentNode.SelectSingleNode("//title").InnerText);
                        var message = $"{thePoint}\n{title}\n{bahaService.BaseUrl}{this.CrawlTargetUrl}\n{text}";

                        await lineNotifyService.Send(lineToken, message);
                    }

                    this.LastCrawledResult = text;
                    logger.LogInformation($"{workerName}, {bahaService.BaseUrl}{this.CrawlTargetUrl}, analysed.");
                }
                catch (Exception ex)
                {
                    logger.LogError($"{workerName}, {bahaService.BaseUrl}{this.CrawlTargetUrl}, something error... \n{ex}");
                }
            });

            timer.Start();
        }
    }
}
