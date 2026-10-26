namespace RaceDay.Api.Security;

// BCrypt adds a random salt to every hash and is slow on purpose,
// so stolen hashes are hard to crack. Work factor 11 matches the Part 1 seed hashes.
public static class PasswordHashing
{
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    public static bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // The stored value isn't a BCrypt hash, so it can never match.
            return false;
        }
    }
}
