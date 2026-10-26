using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[RequireSession]
public class EnrolmentsController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#19 Enter an event in the category you choose. Starts as Pending.</summary>
    /// <response code="201">{ enrolmentId, eventId, categoryId, status: "Pending", enrolledAt }</response>
    /// <response code="400">Category not part of this event, event already happened, or your age on race day is outside the category.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    /// <response code="404">No such event or category.</response>
    /// <response code="409">Already entered this event, or the category is full.</response>
    [HttpPost("events/{eventId:int}/enrolments")]
    [RequireSession(RoleNames.Participant)]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Enrol(int eventId, EnrolRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");

        var category = await _db.Categories.Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId);
        if (category == null) return Error(404, "Category not found.");
        if (category.EventId != eventId) return Error(400, "That category isn't part of this event.");
        if (ev.EventDate <= DateTime.Now) return Error(400, "This event has already happened.");

        // Age categories: check the participant's age on race day.
        if (category.CategoryType == "Age")
        {
            var me = await _db.Users.FindAsync(CurrentUserId);
            if (me!.DateOfBirth == null) return Error(400, "Add your date of birth to your profile first.");
            var age = AgeOn(me.DateOfBirth.Value, ev.EventDate);
            if ((category.MinAge != null && age < category.MinAge) || (category.MaxAge != null && age > category.MaxAge))
                return Error(400, $"You will be {age} on race day, which is outside the {category.Name} category.");
        }

        if (await _db.Enrolments.AnyAsync(e => e.ParticipantId == CurrentUserId && e.EventId == eventId))
            return Error(409, "You have already entered this event.");

        var entries = category.Enrolments.Count(e => e.Status != EnrolmentStatus.Cancelled);
        if (category.MaxParticipants != null && entries >= category.MaxParticipants)
            return Error(409, "This category is full.");

        var enrolment = new Enrolment
        {
            ParticipantId = CurrentUserId,
            EventId = eventId,
            CategoryId = category.CategoryId,
            Status = EnrolmentStatus.Pending,
            EnrolledAt = DateTime.UtcNow
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return Created("/api/enrolments/mine", ToDto(enrolment));
    }

    /// <summary>#20 Every event you have entered, with the category and status.</summary>
    /// <response code="200">Your enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant.</response>
    [HttpGet("enrolments/mine")]
    [RequireSession(RoleNames.Participant)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var list = await _db.Enrolments
            .Where(e => e.ParticipantId == CurrentUserId)
            .OrderBy(e => e.Event!.EventDate)
            .Select(e => new
            {
                e.EnrolmentId,
                e.EventId,
                EventName = e.Event!.Name,
                e.Event.EventDate,
                e.Event.Location,
                CategoryName = e.Category!.Name,
                e.Status,
                e.EnrolledAt
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>#21 Withdraw your own entry before race day. The row is kept as Cancelled.</summary>
    /// <response code="200">The enrolment, now Cancelled.</response>
    /// <response code="400">The event already happened, or the entry is already cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or not your entry.</response>
    /// <response code="404">No such enrolment.</response>
    [HttpPut("enrolments/{id:int}/cancel")]
    [RequireSession(RoleNames.Participant)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> Cancel(int id)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return Error(404, "Enrolment not found.");
        if (enrolment.ParticipantId != CurrentUserId) return Error(403, "That isn't your entry.");
        if (enrolment.Event!.EventDate <= DateTime.Now) return Error(400, "The event has already happened.");
        if (enrolment.Status == EnrolmentStatus.Cancelled) return Error(400, "This entry is already cancelled.");

        enrolment.Status = EnrolmentStatus.Cancelled;
        await _db.SaveChangesAsync();
        return Ok(ToDto(enrolment));
    }

    /// <summary>#22 Everyone entered in one of your events. Optional filter: ?status=Pending</summary>
    /// <response code="200">The entries.</response>
    /// <response code="400">Unknown status filter.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetEventEnrolments(int eventId, string? status)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();
        if (status != null && !EnrolmentStatus.All.Contains(status))
            return Error(400, "status must be Pending, Confirmed or Cancelled.");

        var query = _db.Enrolments.Where(e => e.EventId == eventId);
        if (status != null) query = query.Where(e => e.Status == status);

        var list = await query.OrderBy(e => e.EnrolledAt)
            .Select(e => new
            {
                e.EnrolmentId,
                ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                e.Participant.Email,
                CategoryName = e.Category!.Name,
                e.Status,
                e.EnrolledAt,
                HasResult = e.Result != null
            })
            .ToListAsync();
        return Ok(list);
    }

    /// <summary>#23 Confirm (or change) the status of an entry in one of your events.</summary>
    /// <response code="200">The updated enrolment.</response>
    /// <response code="400">Invalid status, or the entry has a result and the new status isn't Confirmed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such enrolment.</response>
    [HttpPut("enrolments/{id:int}/status")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> ChangeStatus(int id, StatusRequest request)
    {
        if (!EnrolmentStatus.All.Contains(request.Status))
            return Error(400, "status must be Pending, Confirmed or Cancelled.");

        var enrolment = await _db.Enrolments.Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment == null) return Error(404, "Enrolment not found.");
        if (enrolment.Event!.OrganiserId != CurrentUserId) return NotYourEvent();
        if (enrolment.Result != null && request.Status != EnrolmentStatus.Confirmed)
            return Error(400, "This entry already has a result, so it must stay Confirmed.");

        enrolment.Status = request.Status;
        await _db.SaveChangesAsync();
        return Ok(ToDto(enrolment));
    }

    // Age in whole years on the day of the event.
    public static int AgeOn(DateOnly dateOfBirth, DateTime eventDate)
    {
        var age = eventDate.Year - dateOfBirth.Year;
        if (DateOnly.FromDateTime(eventDate) < dateOfBirth.AddYears(age)) age--;
        return age;
    }

    private static object ToDto(Enrolment e) => new
    {
        e.EnrolmentId,
        e.ParticipantId,
        e.EventId,
        e.CategoryId,
        e.Status,
        e.EnrolledAt
    };
}
