namespace Lionsoft.Models
{
    public class PlayerDataModel
    {
        public string Name { get; set; }
        public List<RankingModel> Ranking { get; set; } = new List<RankingModel>();
        public string Bo5Link { get; set; }
        public string PsLink { get; set; }
        public List<TournamentModel> Events { get; set; }
    }
}