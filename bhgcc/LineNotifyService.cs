using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace bhgcc
{
    public interface ILineNotifyService
    {
        Task Send(string token, string message);
    }

    public class LineNotifyService : ILineNotifyService
    {
        private readonly IHttpClientFactory httpClientFactory;

        public LineNotifyService(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }

        public async Task Send(string token, string message)
        {
            using (var http = httpClientFactory.CreateClient("line-notify"))
            {
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "message", message }
                    });

                var p = await http.PostAsync("/api/notify", httpContent);
                p.EnsureSuccessStatusCode();
            }
        }
    }
}
