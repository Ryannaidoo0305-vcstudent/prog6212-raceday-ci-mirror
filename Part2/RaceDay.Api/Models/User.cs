namespace RaceDay.Api.Models;

// Everyone who can log in. The RoleId says if they are an Organiser or a Participant.
public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    public Role? Role { get; set; }
    public List<Event> OrganisedEvents { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}
