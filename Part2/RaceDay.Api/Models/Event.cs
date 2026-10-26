namespace RaceDay.Api.Models;

// A run, walk or cycle event owned by one Organiser.
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public int EventTypeId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public string? BannerImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? Organiser { get; set; }
    public EventType? EventType { get; set; }
    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}
