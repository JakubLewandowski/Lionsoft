    using HtmlAgilityPack;
using Lionsoft.Models;

namespace Lionsoft.Helpers
{
    public class Bo5ClubSiteParserHelper
    {
        public async IAsyncEnumerable<EventModel> GetEvents(string clubName)
        {
            var url = $"https://bo5.pl/{clubName}/calendar";
            var http = new HttpClient();
            var webData = await http.GetAsync(url).Result.Content.ReadAsStreamAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(webData);
            foreach (var node in htmlDocument.DocumentNode.SelectNodes("//*[@id=\"mlevel0\"]/div/div/h4/a") ?? Enumerable.Empty<HtmlNode>())
            {
                var singleEvent = new EventModel()
                {
                    Name = node.InnerHtml.RemoveHtmlTags()
                };

                var id = node.Attributes.LastOrDefault()?.Value.Replace("#", string.Empty);
                foreach (var link in htmlDocument.DocumentNode.SelectNodes($"//*[@id=\"{id}\"]/div[1]/a") ?? Enumerable.Empty<HtmlNode>())
                {
                    var eventLink = new EventLinkModel()
                    {
                        Name = link.InnerHtml.RemoveHtmlTags(),
                        Link = $"https://bo5.pl{link.Attributes.FirstOrDefault()?.Value}",
                        IsUpToDate = !link.OuterHtml.Contains("silver")
                    };

                    singleEvent.Links.Add(eventLink);
                }

                yield return singleEvent;
            }
        }
    }
}