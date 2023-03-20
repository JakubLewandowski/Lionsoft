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
            var url = $"https://pzsq.tournament.tools/info/org.gracz/{psid}";
            var playerDataModel = new PlayerDataModel()
            {
                 PsLink = url
            };
            var httpClientHelper = new HttpClientHelper();
            var htmlDocument = await httpClientHelper.Get(url);

            if (htmlDocument != null)
            {
                playerDataModel.Name = GetPlayerName(htmlDocument);
                playerDataModel.Ranking = await GetRanking(htmlDocument);
                playerDataModel.Results = GetResults(htmlDocument);
            }
            playerDataModel.Bo5Link = $"https://gracz.squasha.pl/{bo5id}/{playerDataModel.Name?.Replace(" ", "+")}";
            playerDataModel.Events = await GetTournaments(playerDataModel.Bo5Link);

            return playerDataModel;
        }

        private string GetPlayerName(HtmlDocument htmlDocument)
        {
            return htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"ranking2\"]/div/div[1]/div/h2/span[2]").InnerText.Trim();
        }

        private async Task<List<RankingModel>> GetRanking(HtmlDocument htmlDocument)
        {
            var list = new List<RankingModel>();
            int i = 0;
            var rankingModel = new RankingModel();

            foreach (HtmlNode rank in htmlDocument.DocumentNode.SelectNodes("//table[@class='ranking']//tr//td") ?? Enumerable.Empty<HtmlNode>())
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
                        list.Add(rankingModel);
                        rankingModel = new RankingModel();
                        i = -1;
                        break;
                    default:
                        break;
                }
                i++;
            }

            return list;
        }

        private List<ResultModel> GetResults(HtmlDocument htmlDocument)
        {
            var yearAgo = DateTime.Now.AddMonths(-12);
            var list = new List<ResultModel>();
            var resultModel = new ResultModel();
            foreach (HtmlNode result in htmlDocument.DocumentNode.SelectNodes("//table[contains(@class,'results')]/tr/td") ?? Enumerable.Empty<HtmlNode>())
            {
                var data = result.InnerText.Trim();

                if (result.OuterHtml.Contains("date"))
                {
                    var date = Convert.ToDateTime(data);

                    if (date < yearAgo)
                    {
                        break;
                    }

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
                    list.Add(resultModel);
                    resultModel = new ResultModel();
                }
            }
            
            return list;
        }

        private async Task<List<TournamentModel>> GetTournaments(string url)
        {
            var list = new List<TournamentModel>();
            var httpClientHelper = new HttpClientHelper();
            var htmlDocument = await httpClientHelper.Get(url);

            if (htmlDocument != null)
            {
                foreach (HtmlNode row in htmlDocument.DocumentNode.SelectNodes("//table[contains(@class,'incoming')]/tr/td[2]") ?? Enumerable.Empty<HtmlNode>())
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
            }

            return list;
        }
    }
}