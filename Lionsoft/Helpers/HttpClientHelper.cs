using HtmlAgilityPack;

namespace Lionsoft.Helpers
{
    public class HttpClientHelper
    {
        public async Task<HtmlDocument?> Get(string url)
        {
            try
            {
                var htmlDocument = new HtmlDocument();
                var http = new HttpClient();
                var webData = await http.GetAsync(url).Result.Content.ReadAsStreamAsync();
                htmlDocument.Load(webData);
                return htmlDocument;
            }
            catch { }

            return null;
        }
    }
}