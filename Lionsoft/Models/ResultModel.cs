namespace Lionsoft.Models
{
    public class ResultModel
    {
        public int? CardinalNumber { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Rank { get; set; }
        public decimal Result { get; set; }
        public bool IsInTopEight { get; set; } = false;
    }
}