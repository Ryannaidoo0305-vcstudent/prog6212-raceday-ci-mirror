using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Event and category management - success and failure.
public class EventTests
{
    private static object NewEvent(int daysFromNow = 60, decimal distanceKm = 5, int eventTypeId = 1) => new
    {
        name = "Test Park Run",
        description = "A short test run.",
        eventDate = DateTime.Now.AddDays(daysFromNow),
        location = "Emmarentia Dam, Johannesburg",
        distanceKm,
        eventTypeId
    };

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events", NewEvent());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_InThePast_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events", NewEvent(daysFromNow: -1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_ZeroDistance_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events", NewEvent(distanceKm: 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_UnknownType_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events", NewEvent(eventTypeId: 99));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateOwnEvent_Returns200()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PutAsJsonAsync("/api/events/4", NewEvent());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_WithEnrolments_Returns409()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.DeleteAsync("/api/events/4");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteNewEvent_Returns204()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);
        var created = await client.PostAsJsonAsync("/api/events", NewEvent());
        var id = (await created.Content.ReadFromJsonAsync<IdOnly>())!.EventId;

        var response = await client.DeleteAsync($"/api/events/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task GetMissingEvent_Returns404()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Sipho);

        var response = await client.GetAsync("/api/events/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ShowsOnlyMyEvents()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var events = await client.GetFromJsonAsync<List<IdOnly>>("/api/events/mine");

        Assert.Equal(new[] { 1, 4 }, events!.Select(e => e.EventId).OrderBy(x => x));
    }

    [Fact]
    public async Task AddCategory_DuplicateName_Returns409()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events/4/categories",
            new { name = "Senior (20-39)", categoryType = "Age", minAge = 20, maxAge = 39 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_MinAgeAboveMaxAge_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events/4/categories",
            new { name = "Odd", categoryType = "Age", minAge = 50, maxAge = 30 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddCategory_Valid_Returns201()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events/4/categories",
            new { name = "5km Fun Run", categoryType = "Distance", distanceKm = 5 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private class IdOnly
    {
        public int EventId { get; set; }
    }
}
