namespace RaceDay.Api.Models;

// A Participant's entry into an event, in the category they chose.
// This is the bridge table for the many-to-many between Participants and Events.
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string Status { get; set; } = EnrolmentStatus.Pending;
    public DateTime EnrolledAt { get; set; }

    public User? Participant { get; set; }
    public Event? Event { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}

public static class EnrolmentStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = { Pending, Confirmed, Cancelled };
}
