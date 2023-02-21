﻿using HtmlAgilityPack;
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
            var webData = await http.GetAsync(url).Result.Content.ReadAsStreamAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(webData);
            playerDataModel.Name = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"ranking2\"]/div/div[1]/div/h2/span[2]").InnerText.Trim();
            playerDataModel.Bo5Link = $"https://gracz.squasha.pl/{bo5id}/{playerDataModel.Name.Replace(" ", "+")}";

            var webData2 = await http.GetAsync(playerDataModel.Bo5Link).Result.Content.ReadAsStringAsync();
            var htmlDocument2 = new HtmlDocument();
            htmlDocument2.LoadHtml(webData2);
            var test = htmlDocument.DocumentNode.SelectNodes("//*[@id=\"mainContainer\"]/div/div/div[4]/div/div[2]/table/tbody/tr/td");

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
                        case 3:
                            rankingModel.BestPosition = data.Replace("&nbsp;", " ");
                            playerDataModel.Ranking.Add(rankingModel);
                            rankingModel = new RankingModel();
                            i = -1;
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