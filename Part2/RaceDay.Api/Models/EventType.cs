namespace RaceDay.Api.Models;

// Run, Walk or Cycle (Part 1 table: EventTypes).
public class EventType
{
    public int EventTypeId { get; set; }
    public string TypeName { get; set; } = "";

    public List<Event> Events { get; set; } = new();
}
