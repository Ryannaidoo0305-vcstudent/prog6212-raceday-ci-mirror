using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api")]
[RequireSession]
public class CategoriesController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#15 An event's categories, with spaces left where there is an entry limit.</summary>
    /// <response code="200">The categories.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No such event.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return Error(404, "Event not found.");

        var categories = await _db.Categories.Include(c => c.Enrolments)
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();
        return Ok(categories.Select(ToDto));
    }

    /// <summary>#16 Add an Age or Distance category to one of your events.</summary>
    /// <response code="201">The new category.</response>
    /// <response code="400">Type not Age/Distance, minAge above maxAge, Age without an age limit, or Distance without distanceKm.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such event.</response>
    /// <response code="409">That name is already used in this event.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> CreateCategory(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return Error(404, "Event not found.");
        if (ev.OrganiserId != CurrentUserId) return NotYourEvent();

        var problem = CheckCategory(request);
        if (problem != null) return problem;

        if (await _db.Categories.AnyAsync(c => c.EventId == eventId && c.Name == request.Name.Trim()))
            return Error(409, "This event already has a category with that name.");

        var category = new Category { EventId = eventId };
        CopyCategory(request, category);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return Created($"/api/events/{eventId}/categories", ToDto(category));
    }

    /// <summary>#17 Update a category on one of your events.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Same rules as #16, or a limit below the entries already made.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such category.</response>
    /// <response code="409">That name is already used in this event.</response>
    [HttpPut("categories/{id:int}")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return Error(404, "Category not found.");
        if (category.Event!.OrganiserId != CurrentUserId) return NotYourEvent();

        var problem = CheckCategory(request);
        if (problem != null) return problem;

        var activeEntries = category.Enrolments.Count(e => e.Status != EnrolmentStatus.Cancelled);
        if (request.MaxParticipants != null && request.MaxParticipants < activeEntries)
            return Error(400, $"The limit can't be lower than the {activeEntries} entries already made.");

        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.CategoryId != id && c.Name == request.Name.Trim()))
            return Error(409, "This event already has a category with that name.");

        CopyCategory(request, category);
        await _db.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>#18 Remove a category that nobody has entered yet.</summary>
    /// <response code="204">Deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not your event.</response>
    /// <response code="404">No such category.</response>
    /// <response code="409">Participants have already entered this category.</response>
    [HttpDelete("categories/{id:int}")]
    [RequireSession(RoleNames.Organiser)]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ProblemDetails), 403)]
    [ProducesResponseType(typeof(ProblemDetails), 404)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return Error(404, "Category not found.");
        if (category.Event!.OrganiserId != CurrentUserId) return NotYourEvent();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
            return Error(409, "Participants have already entered this category, so it can't be deleted.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // The same rules as the CHECK constraints in Part 1, so the user gets a clear 400 instead of a database error.
    private ObjectResult? CheckCategory(CategoryRequest request)
    {
        if (request.CategoryType != "Age" && request.CategoryType != "Distance")
            return Error(400, "categoryType must be Age or Distance.");
        if (request.CategoryType == "Age" && request.MinAge == null && request.MaxAge == null)
            return Error(400, "An Age category needs a minAge or a maxAge.");
        if (request.MinAge != null && request.MaxAge != null && request.MinAge > request.MaxAge)
            return Error(400, "minAge can't be above maxAge.");
        if (request.CategoryType == "Distance" && request.DistanceKm == null)
            return Error(400, "A Distance category needs distanceKm.");
        return null;
    }

    private static void CopyCategory(CategoryRequest request, Category category)
    {
        category.Name = request.Name.Trim();
        category.CategoryType = request.CategoryType;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;
        category.MaxParticipants = request.MaxParticipants;
    }

    public static object ToDto(Category c)
    {
        var entries = c.Enrolments.Count(e => e.Status != EnrolmentStatus.Cancelled);
        return new
        {
            c.CategoryId,
            c.EventId,
            c.Name,
            c.CategoryType,
            c.MinAge,
            c.MaxAge,
            c.DistanceKm,
            c.MaxParticipants,
            SpacesLeft = c.MaxParticipants == null ? (int?)null : c.MaxParticipants - entries
        };
    }
}
