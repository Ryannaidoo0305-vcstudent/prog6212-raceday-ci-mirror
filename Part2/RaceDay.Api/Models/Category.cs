namespace RaceDay.Api.Models;

// An Age or Distance category that a Participant picks when entering an event.
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string CategoryType { get; set; } = "";
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? MaxParticipants { get; set; }

    public Event? Event { get; set; }
    public List<Enrolment> Enrolments { get; set; } = new();
}
