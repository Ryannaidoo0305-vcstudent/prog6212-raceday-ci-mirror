using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.Api.Tests;

// Starts the real API in memory for the tests.
// The "Testing" environment makes Program.cs use an in-memory database with the Part 1 seed data,
// and every TestApi gets its own database (see Program.cs), so tests don't affect each other.
public class TestApi : WebApplicationFactory<Program>
{
    public const string Password = "RaceDay@2026";
    public const string Thandi = "thandi.mokoena@example.com";   // Organiser of events 1 and 4
    public const string Pieter = "pieter.vdmerwe@example.com";   // Organiser of events 2 and 3
    public const string Sipho = "sipho.ndlovu@example.com";      // Participant
    public const string Aisha = "aisha.patel@example.com";       // Participant

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    // A client that is already logged in as the given user (the session cookie is kept).
    public async Task<HttpClient> LoginAs(string email)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        return client;
    }
}
