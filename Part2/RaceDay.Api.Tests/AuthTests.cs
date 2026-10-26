using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Registration and login - success and failure.
public class AuthTests
{
    private static object NewParticipant(string email, string password = "Strong@123") => new
    {
        firstName = "Test",
        lastName = "Runner",
        email,
        password,
        role = "Participant",
        dateOfBirth = "1999-05-01"
    };

    [Fact]
    public async Task Register_NewParticipant_Returns201()
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewParticipant("new.runner@example.com"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewParticipant("THANDI.MOKOENA@example.com"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("NoNumbers!!")]
    [InlineData("NoSymbol123")]
    public async Task Register_WeakPassword_Returns400(string password)
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewParticipant("weak@example.com", password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_UnknownRole_Returns400()
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Test", lastName = "Admin", email = "admin@example.com", password = "Strong@123", role = "Admin"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_CorrectPassword_Returns200WithRole()
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestApi.Thandi, password = TestApi.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Organiser", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = TestApi.Thandi, password = "Wrong@1234" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_ThenLogin_Works()
    {
        using var api = new TestApi();
        var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", NewParticipant("brand.new@example.com"));

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "brand.new@example.com", password = "Strong@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsTheSession()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Sipho);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var after = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }
}
