using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[RequireSession]
public class ResultsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#24 Capture a finish time and position after the event. Saved unpublished.</summary>
    /// <response code="201">The new result.</response>
    /// <response code="400">Event hasn't happened yet, entry not in this event or not Confirmed, or invalid time.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such event or enrolment.</response>
    /// <response code="409">This entry already has a result, or the position is taken.</response>
    [HttpPost("events/{eventId:int}/results")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CaptureResult(int eventId, ResultRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();
        if (ev.EventDate > DateTime.Now) return Error(400, "You can only capture results after the event.");
        if (request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1))
            return Error(400, "finishTime must be between 00:00:01 and 23:59:59.");

        var enrolment = await _db.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);
        if (enrolment == null) return Error(404, "Enrolment not found.");
        if (enrolment.EventId != eventId) return Error(400, "That entry isn't in this event.");
        if (enrolment.Status != EnrolmentStatus.Confirmed) return Error(400, "Only Confirmed entries can have a result.");
        if (enrolment.Result != null) return Error(409, "This entry already has a result.");
        if (await PositionTaken(eventId, request.FinishPosition, null))
            return Error(409, $"Position {request.FinishPosition} is already taken in this event.");

        var result = new Result
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = request.FinishTime,
            FinishPosition = request.FinishPosition,
            IsPublished = false,
            RecordedAt = DateTime.UtcNow
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/results", ToDto(result));
    }

    /// <summary>#25 Correct a captured finish time or position.</summary>
    /// <response code="200">The updated result.</response>
    /// <response code="400">Invalid time or position.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such result.</response>
    /// <response code="409">The position is already taken.</response>
    [HttpPut("results/{id:int}")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> UpdateResult(int id, UpdateResultRequest request)
    {
        var result = await _db.Results.Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);
        if (result == null) return Error(404, "Result not found.");
        var ev = result.Enrolment!.Event!;
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();
        if (request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromDays(1))
            return Error(400, "finishTime must be between 00:00:01 and 23:59:59.");
        if (await PositionTaken(ev.EventId, request.FinishPosition, id))
            return Error(409, $"Position {request.FinishPosition} is already taken in this event.");

        result.FinishTime = request.FinishTime;
        result.FinishPosition = request.FinishPosition;
        await _db.SaveChangesAsync();
        return Ok(ToDto(result));
    }

    /// <summary>#26 Publish all captured results for one of your events.</summary>
    /// <response code="200">{ publishedCount }</response>
    /// <response code="400">No results captured yet.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such event.</response>
    [HttpPut("events/{eventId:int}/results/publish")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Publish(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();

        var results = await _db.Results.Where(r => r.Enrolment!.EventId == eventId).ToListAsync();
        if (results.Count == 0) return Error(400, "No results have been captured for this event yet.");

        foreach (var r in results) r.IsPublished = true;
        await _db.SaveChangesAsync();
        return Ok(new { PublishedCount = results.Count });
    }

    /// <summary>#27 All results for one of your events (published or not), by position.</summary>
    /// <response code="200">The results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("events/{eventId:int}/results")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();

        var list = await _db.Results
            .Where(r => r.Enrolment!.EventId == eventId)
            .OrderBy(r => r.FinishPosition)
            .Select(r => new
            {
                r.ResultId,
                r.EnrolmentId,
                r.FinishPosition,
                ParticipantName = r.Enrolment!.Participant!.FirstName + " " + r.Enrolment.Participant.LastName,
                CategoryName = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.IsPublished
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>#28 Your race history: published results only, e.g. 1st of 5 finishers.</summary>
    /// <response code="200">Your results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("results/mine")]
    [RequireSession(RoleNames.Participant)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetMyResults()
    {
        var list = await _db.Results
            .Where(r => r.IsPublished && r.Enrolment!.ParticipantId == CurrentUserId)
            .Select(r => new
            {
                EventName = r.Enrolment!.Event!.Name,
                r.Enrolment.Event.EventDate,
                CategoryName = r.Enrolment.Category!.Name,
                r.FinishTime,
                r.FinishPosition,
                TotalFinishers = _db.Results.Count(x => x.IsPublished && x.Enrolment!.EventId == r.Enrolment.EventId)
            })
            .ToListAsync();
        return Ok(list);
    }

    private Task<bool> PositionTaken(int eventId, int position, int? ignoreResultId)
    {
        return _db.Results.AnyAsync(r => r.Enrolment!.EventId == eventId
                                         && r.FinishPosition == position
                                         && r.ResultId != ignoreResultId);
    }

    private static object ToDto(Result r) => new
    {
        r.ResultId,
        r.EnrolmentId,
        r.FinishTime,
        r.FinishPosition,
        r.IsPublished,
        r.RecordedAt
    };
}
