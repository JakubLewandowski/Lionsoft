using HtmlAgilityPack;
using Lionsoft.Models;
using Microsoft.Extensions.FileSystemGlobbing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

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
            playerDataModel.Name = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"ranking2\"]/div/div[1]/div/h2/span[2]").InnerText.Trim();
            playerDataModel.Bo5Link = $"https://gracz.squasha.pl/{bo5id}/{playerDataModel.Name.Replace(" ", "+")}";
            playerDataModel.Events = GetTournaments(playerDataModel.Bo5Link);

            int i = 0;
            var rankingModel = new RankingModel();

            foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//table[@class='ranking']//tr//td"))
            {
                var data = row.InnerText.Trim();
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

            return playerDataModel;
        }

        private async IAsyncEnumerable<TournamentModel> GetTournaments(string url)
        {
            var tournamentModel = new TournamentModel();
            var http = new HttpClient();
            var webData = await http.GetAsync(url).Result.Content.ReadAsStringAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(webData);

            foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//*[@id=\"mainContainer\"]/div/div/div[4]/div/div[2]/table/tr/td/div/a"))
            {
                tournamentModel = new TournamentModel();
                tournamentModel.Name = row.InnerText.Trim();
                string pattern = @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1";
                Regex rg = new Regex(pattern);
                MatchCollection matchedAuthors = rg.Matches(row.OuterHtml);
                tournamentModel.Url = matchedAuthors[0].Groups[2].Value;

                yield return tournamentModel;
            }
        }
    }
}