using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bhgcc
{
    public interface IBahaService
    {
        string BaseUrl { get; }
        Task<string> GetCrawlTargetUrl(string boardCode, string searchTitle);
        Task<string> GetHtml(string url);
    }

    public class BahaService : IBahaService
    {
        public const string BaseUrl = "https://forum.gamer.com.tw";
        private readonly IHttpClientFactory httpClientFactory;

        public BahaService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        string IBahaService.BaseUrl => BaseUrl;

        public async Task<string> GetCrawlTargetUrl(string boardCode, string searchTitle)
        {
            var html = string.Empty;
            using (var http = httpClientFactory.CreateClient("baha"))
            {
                var uri = $"/B.php?bsn={boardCode}&qt=1&q={searchTitle}";
                html = await http.GetStringAsync(uri);
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);

                var firstSearchResultHref = htmlDoc.DocumentNode // root
                                                .SelectSingleNode("//table[@class='b-list']") // 找 table
                                                .SelectSingleNode("//tr[@class='b-list__row b-list-item b-imglist-item']") // 找到第一個項目                
                                                .SelectSingleNode("//td[@class='b-list__main']") // 找放 a 的 td
                                                .SelectSingleNode("a") // a
                                                .Attributes["href"].Value; // C.php?bsn=60596&snA=47062&tnum=132

                // 把多餘的參數截掉
                // 最後會長得像 /C.php?bsn=60596&snA=47062
                var targetPostUrl = firstSearchResultHref.Substring(0, firstSearchResultHref.LastIndexOf("&"));
                return $"/{targetPostUrl}&last=1";
            }
        }

        public async Task<string> GetHtml(string uri)
        {
            using (var http = httpClientFactory.CreateClient("baha"))
                return await http.GetStringAsync(uri);
        }
    }
}
