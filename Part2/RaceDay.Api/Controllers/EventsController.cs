using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[RequireSession]
public class EventsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#7 The event types (Run, Walk, Cycle) for dropdowns and filters.</summary>
    /// <response code="200">[ { eventTypeId, typeName } ]</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("event-types")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetEventTypes()
    {
        var types = await _db.EventTypes.OrderBy(t => t.EventTypeId)
            .Select(t => new { t.EventTypeId, t.TypeName })
            .ToListAsync();
        return Ok(types);
    }

    /// <summary>#8 Browse events, soonest first. Filters: type, search, from, to, includePast.</summary>
    /// <response code="200">The matching events (an empty list if nothing matches).</response>
    /// <response code="400">Bad filter, e.g. from after to, or an unknown type.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("events")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetEvents(string? type, string? search, DateTime? from, DateTime? to, bool includePast = false)
    {
        if (from != null && to != null && from > to)
            return Error(400, "'from' must be before 'to'.");

        var query = _db.Events.AsQueryable();

        if (!string.IsNullOrWhiteSpace(type))
        {
            if (!await _db.EventTypes.AnyAsync(t => t.TypeName == type))
                return Error(400, $"Unknown event type '{type}'. Use Run, Walk or Cycle.");
            query = query.Where(e => e.EventType!.TypeName == type);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(text) || e.Location.ToLower().Contains(text));
        }
        if (!includePast) query = query.Where(e => e.EventDate >= DateTime.Now);
        if (from != null) query = query.Where(e => e.EventDate >= from);
        if (to != null) query = query.Where(e => e.EventDate <= to);

        var events = await query.OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.EventDate,
                e.Location,
                e.DistanceKm,
                EventType = e.EventType!.TypeName,
                e.BannerImageUrl
            })
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>#9 One event in full, with the organiser's name and its categories.</summary>
    /// <response code="200">The event details.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpGet("events/{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.EventType)
            .Include(e => e.Organiser)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return Error(404, "Event not found.");

        return Ok(new
        {
            ev.EventId,
            ev.Name,
            ev.Description,
            ev.EventDate,
            ev.Location,
            ev.DistanceKm,
            EventType = ev.EventType!.TypeName,
            OrganiserName = $"{ev.Organiser!.FirstName} {ev.Organiser.LastName}",
            ev.BannerImageUrl,
            Categories = ev.Categories.OrderBy(c => c.CategoryId).Select(CategoriesController.ToDto)
        });
    }

    /// <summary>#10 Organiser dashboard: your events with total, confirmed and pending entries.</summary>
    /// <response code="200">Your events with their entry counts.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpGet("events/mine")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetMyEvents()
    {
        var events = await _db.Events
            .Where(e => e.OrganiserId == CurrentUserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.Name,
                e.EventDate,
                EventType = e.EventType!.TypeName,
                TotalEnrolments = e.Enrolments.Count,
                Confirmed = e.Enrolments.Count(x => x.Status == EnrolmentStatus.Confirmed),
                Pending = e.Enrolments.Count(x => x.Status == EnrolmentStatus.Pending)
            })
            .ToListAsync();
        return Ok(events);
    }

    /// <summary>#11 Create an event that you own.</summary>
    /// <response code="201">The new event.</response>
    /// <response code="400">Missing field, date in the past, distance of 0 or less, or unknown eventTypeId.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    [HttpPost("events")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> CreateEvent(EventRequest request)
    {
        var problem = await CheckEvent(request);
        if (problem != null) return problem;

        var ev = new Event { OrganiserId = CurrentUserId, CreatedAt = DateTime.UtcNow };
        CopyEvent(request, ev);
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>#12 Update one of your own events.</summary>
    /// <response code="200">The updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No event with that ID.</response>
    [HttpPut("events/{id:int}")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> UpdateEvent(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();

        var problem = await CheckEvent(request);
        if (problem != null) return problem;

        CopyEvent(request, ev);
        await _db.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>#13 Delete one of your events (and its categories). Refused once anyone has entered.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No event with that ID.</response>
    /// <response code="409">The event already has enrolments.</response>
    [HttpDelete("events/{id:int}")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();

        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
            return Error(409, "This event already has enrolments, so it can't be deleted.");

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Rules the data annotations can't check.
    private async Task<IActionResult?> CheckEvent(EventRequest request)
    {
        if (request.EventDate <= DateTime.Now)
            return Error(400, "The event date must be in the future.");
        if (!await _db.EventTypes.AnyAsync(t => t.EventTypeId == request.EventTypeId))
            return Error(400, "Unknown eventTypeId. Use 1 (Run), 2 (Walk) or 3 (Cycle).");
        return null;
    }

    private static void CopyEvent(EventRequest request, Event ev)
    {
        ev.Name = request.Name.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventTypeId = request.EventTypeId;
    }

    private static object ToDto(Event ev) => new
    {
        ev.EventId,
        ev.OrganiserId,
        ev.EventTypeId,
        ev.Name,
        ev.Description,
        ev.EventDate,
        ev.Location,
        ev.DistanceKm,
        ev.BannerImageUrl,
        ev.CreatedAt
    };
}
