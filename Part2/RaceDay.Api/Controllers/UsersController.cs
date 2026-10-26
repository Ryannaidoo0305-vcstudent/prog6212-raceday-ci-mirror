using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/users")]
[RequireSession]
public class UsersController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#4 The logged-in user's own profile.</summary>
    /// <response code="200">The profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> GetMe()
    {
        var user = await _db.Users.Include(u => u.Role).SingleAsync(u => u.UserId == CurrentUserId);
        return Ok(ToProfile(user, user.Role!.RoleName));
    }

    /// <summary>#5 Update your own details. Email and role can't be changed.</summary>
    /// <response code="200">The updated profile.</response>
    /// <response code="400">Invalid field, e.g. a date of birth in the future.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request)
    {
        if (request.DateOfBirth != null && request.DateOfBirth >= DateOnly.FromDateTime(DateTime.Today))
            return Error(400, "Date of birth must be in the past.");

        var user = await _db.Users.Include(u => u.Role).SingleAsync(u => u.UserId == CurrentUserId);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.DateOfBirth = request.DateOfBirth;
        await _db.SaveChangesAsync();

        return Ok(ToProfile(user, user.Role!.RoleName));
    }

    // The profile shape returned to the client - never includes the password hash.
    public static object ToProfile(User user, string role) => new
    {
        user.UserId,
        user.FirstName,
        user.LastName,
        user.Email,
        Role = role,
        user.PhoneNumber,
        user.DateOfBirth,
        user.ProfilePictureUrl,
        user.CreatedAt
    };
}
