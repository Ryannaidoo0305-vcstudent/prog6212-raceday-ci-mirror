namespace RaceDay.Api.Security;

// The keys used to store the logged-in user in the server-side session.
public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

// The two roles from Part 1 (the Roles table only allows these two).
public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
