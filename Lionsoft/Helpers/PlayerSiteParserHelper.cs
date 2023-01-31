using HtmlAgilityPack;
using Lionsoft.Models;

namespace Lionsoft.Helpers
{
    public static class PlayerSiteParserHelper
    {
        public static PlayerDataModel GetPlayerData(string url)
        {
            var playerDataModel = new PlayerDataModel();
            var http = new HttpClient();
            var webData = http.GetAsync(url).Result.Content.ReadAsStringAsync().Result;
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(webData);
            playerDataModel.Name = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"ranking2\"]/div/div[1]/div/h2/span[2]").InnerText;
            int i = 0;
            var rankingModel = new RankingModel();

            foreach (HtmlNode col in htmlDocument.DocumentNode.SelectNodes("//table[@class='ranking']//tr//td"))
            {
                var data = col.InnerText.Trim();
                if (!string.IsNullOrEmpty(data))
                {
                    switch (i)
                    {
                        case 0:
                            rankingModel.Name = data;
                            break;
                        case 1:
                            rankingModel.Position = data;
                            break;
                        case 2:
                            rankingModel.Points = data;
                            break;
                        default:
                            break;
                    }

                    i++;
                }
                else
                {
                    playerDataModel.Ranking.Add(rankingModel);
                    rankingModel = new RankingModel();
                    i = 0;
                }

            }

            return playerDataModel;
        }
    }
}