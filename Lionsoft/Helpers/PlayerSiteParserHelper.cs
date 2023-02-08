using HtmlAgilityPack;
using Lionsoft.Models;

namespace Lionsoft.Helpers
{
    public class PlayerSiteParserHelper
    {
        public async IAsyncEnumerable<PlayerDataModel> GetPlayersData()
        {
            yield return await GetPlayerData(31483, 38681);
            yield return await GetPlayerData(31492, 38542);
        }

        public async Task<PlayerDataModel> GetPlayerData(int psid, int bo5id)
        {
            var url = $"https://ranking.polskisquash.pl/info/org.gracz/{psid}";
            var playerDataModel = new PlayerDataModel()
            {
                 PsLink = url
            };
            var http = new HttpClient();
            var webData = await http.GetAsync(url).Result.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(webData);
            playerDataModel.Name = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"ranking2\"]/div/div[1]/div/h2/span[2]").InnerText;
            playerDataModel.Bo5Link = $"https://gracz.squasha.pl/{bo5id}/{playerDataModel.Name.Replace(" ", "+")}";
            int i = 0;
            var rankingModel = new RankingModel();

            foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//table[@class='ranking']//tr//td"))
            {
                var data = row.InnerText.Trim();
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