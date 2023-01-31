namespace Lionsoft.Models
{
    public class EventModel
    {
        public string Name { get; set; }
        public List<EventLinkModel> Links { get; set; } = new List<EventLinkModel>();
    }
}