using HtmlAgilityPack;
using Lionsoft.Models;
using System.Text.RegularExpressions;

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
            playerDataModel.Events = await GetTournaments(playerDataModel.Bo5Link);

            int i = 0;
            var rankingModel = new RankingModel();

            foreach (HtmlNode rank in htmlDocument.DocumentNode.SelectNodes("//table[@class='ranking']//tr//td"))
            {
                var data = rank.InnerText.Trim();
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

            var resultModel = new ResultModel();
            foreach (HtmlNode result in htmlDocument.DocumentNode.SelectNodes("//*[@id=\"ranking2\"]/div/div[6]/table/tr/td"))
            {
                var data = result.InnerText.Trim();

                if (result.OuterHtml.Contains("date"))
                {
                    resultModel.Date = data;
                }
                else if (result.OuterHtml.Contains("name"))
                {
                    resultModel.Name = data;
                }
                else if (result.OuterHtml.Contains("category"))
                {
                    resultModel.Category = data;
                }
                else if (result.OuterHtml.Contains("rank"))
                {
                    resultModel.Rank = data;
                }
                else if (result.OuterHtml.Contains("result"))
                {
                    resultModel.Result = data;
                }
                else if (result.OuterHtml.Contains("options"))
                {
                    playerDataModel.Results.Add(resultModel);
                    resultModel = new ResultModel();
                }
            }
            
            return playerDataModel;
        }

        private async Task<List<TournamentModel>> GetTournaments(string url)
        {
            var list = new List<TournamentModel>();
            var http = new HttpClient();
            var webData = await http.GetAsync(url).Result.Content.ReadAsStreamAsync();
            var htmlDocument = new HtmlDocument();
            htmlDocument.Load(webData);

            foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//table[contains(@class,'incoming')]/tr/td[2]"))
            {
                var tournamentModel = new TournamentModel
                {
                    Name = row.InnerHtml.RemoveHtmlTags()
                };
                string pattern = @"<a\s+(?:[^>]*?\s+)?href=([""'])(.*?)\1";
                var regex = new Regex(pattern);
                MatchCollection matchedUrls = regex.Matches(row.OuterHtml);
                tournamentModel.Url = matchedUrls[0].Groups[2].Value;
                list.Add(tournamentModel);
            }

            return list;
        }
    }
}