namespace Lionsoft.Models
{
    public class PlayerDataModel
    {
        public string Name { get; set; }
        public List<RankingModel> Ranking { get; set; } = new List<RankingModel>();
    }
}