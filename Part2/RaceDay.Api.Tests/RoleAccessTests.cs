using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Role rejection: no session gives 401, the wrong role gives 403.
public class RoleAccessTests
{
    [Theory]
    [InlineData("GET", "/api/users/me")]
    [InlineData("GET", "/api/event-types")]
    [InlineData("GET", "/api/events")]
    [InlineData("GET", "/api/events/mine")]
    [InlineData("POST", "/api/events")]
    [InlineData("DELETE", "/api/events/1")]
    [InlineData("POST", "/api/events/4/enrolments")]
    [InlineData("GET", "/api/enrolments/mine")]
    [InlineData("PUT", "/api/enrolments/9/status")]
    [InlineData("PUT", "/api/events/1/results/publish")]
    [InlineData("GET", "/api/results/mine")]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401(string method, string url)
    {
        using var api = new TestApi();
        var client = api.CreateClient();

        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { }) };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/events/mine")]
    [InlineData("POST", "/api/events")]
    [InlineData("GET", "/api/events/4/enrolments")]
    [InlineData("PUT", "/api/events/1/results/publish")]
    public async Task OrganiserEndpoint_AsParticipant_Returns403(string method, string url)
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Sipho);

        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { }) };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/enrolments/mine")]
    [InlineData("POST", "/api/events/4/enrolments")]
    [InlineData("GET", "/api/results/mine")]
    public async Task ParticipantEndpoint_AsOrganiser_Returns403(string method, string url)
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var request = new HttpRequestMessage(new HttpMethod(method), url) { Content = JsonContent.Create(new { categoryId = 11 }) };
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ChangingAnotherOrganisersEvent_Returns403()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);   // event 2 belongs to Pieter

        var response = await client.DeleteAsync("/api/events/2");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
