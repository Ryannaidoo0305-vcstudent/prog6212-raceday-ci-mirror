using System.Net;
using System.Net.Http.Json;

namespace RaceDay.Api.Tests;

// Enrolments and results - success and failure.
public class EnrolmentTests
{
    [Fact]
    public async Task Enrol_ValidCategory_Returns201Pending()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);   // born 2001, so Senior (20-39) on race day

        var response = await client.PostAsJsonAsync("/api/events/4/enrolments", new { categoryId = 11 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Pending", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Enrol_Twice_Returns409()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);
        await client.PostAsJsonAsync("/api/events/4/enrolments", new { categoryId = 11 });

        var response = await client.PostAsJsonAsync("/api/events/4/enrolments", new { categoryId = 11 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WrongAgeCategory_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);   // 25 on race day, not a Master (50+)

        var response = await client.PostAsJsonAsync("/api/events/4/enrolments", new { categoryId = 13 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_CategoryFromAnotherEvent_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);

        var response = await client.PostAsJsonAsync("/api/events/4/enrolments", new { categoryId = 8 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_PastEvent_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);

        var response = await client.PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 2 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_OwnEntry_Returns200()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Sipho);   // enrolment 9 is Sipho's Pending 10K entry

        var response = await client.PutAsync("/api/enrolments/9/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEntry_Returns403()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Aisha);

        var response = await client.PutAsync("/api/enrolments/9/cancel", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_ConfirmsEntry_Returns200()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PutAsJsonAsync("/api/enrolments/9/status", new { status = "Confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_InvalidStatus_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PutAsJsonAsync("/api/enrolments/9/status", new { status = "Maybe" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_SeesPublishedResult()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Sipho);

        var body = await client.GetStringAsync("/api/results/mine");

        Assert.Contains("Jozi Sunrise Half Marathon", body);
        Assert.Contains("\"totalFinishers\":5", body);
    }

    [Fact]
    public async Task Result_PositionAlreadyTaken_Returns409()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PutAsJsonAsync("/api/results/2", new { finishTime = "01:29:05", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Result_BeforeRaceDay_Returns400()
    {
        using var api = new TestApi();
        var client = await api.LoginAs(TestApi.Thandi);

        var response = await client.PostAsJsonAsync("/api/events/4/results", new { enrolmentId = 9, finishTime = "00:45:00", finishPosition = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
