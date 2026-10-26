namespace RaceDay.Api.Models;

// Organiser or Participant (Part 1 table: Roles).
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";

    public List<User> Users { get; set; } = new();
}
