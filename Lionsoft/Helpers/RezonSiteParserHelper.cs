using HtmlAgilityPack;
using Lionsoft.Models;

namespace Lionsoft.Helpers
{
    public static class RezonSiteParserHelper
    {
        public static List<EventModel> GetEvents()
        {
            var url = "https://bo5.pl/rezon/calendar";
            var list = new List<EventModel>();
            var http = new HttpClient();
            var webData = http.GetAsync(url).Result.Content.ReadAsStringAsync().Result;
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(webData);
            foreach (var node in htmlDocument.DocumentNode.SelectNodes("//*[@id=\"mlevel0\"]/div/div/h4/a"))
            {
                var singleEvent = new EventModel()
                {
                    Name = node.InnerText
                };

                var id = node.Attributes.LastOrDefault().Value.Replace("#", string.Empty);
                foreach (var link in htmlDocument.DocumentNode.SelectNodes($"//*[@id=\"{id}\"]/div[1]/a"))
                {
                    var eventLink = new EventLinkModel()
                    {
                        Name = link.InnerText,
                        Link = $"https://bo5.pl{link.Attributes.FirstOrDefault().Value}"
                    };

                    singleEvent.Links.Add(eventLink);
                }

                list.Add(singleEvent);
            }

            return list;
        }
    }
}