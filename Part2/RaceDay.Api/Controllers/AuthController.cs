using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly RaceDayDbContext _db;

    public AuthController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>#1 Register a new Organiser or Participant. The password is stored as a BCrypt hash.</summary>
    /// <response code="201">The new profile (never the password hash).</response>
    /// <response code="400">Missing or invalid field, weak password, unknown role, or a Participant without a date of birth.</response>
    /// <response code="409">The email is already registered.</response>
    [HttpPost("register")]
    [ProducesResponseType(201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request.Role != RoleNames.Organiser && request.Role != RoleNames.Participant)
            return Error(400, "Role must be Organiser or Participant.");

        if (request.Role == RoleNames.Participant && request.DateOfBirth == null)
            return Error(400, "Participants need a date of birth for the age categories.");

        if (request.DateOfBirth != null && request.DateOfBirth >= DateOnly.FromDateTime(DateTime.Today))
            return Error(400, "Date of birth must be in the past.");

        var email = request.Email.Trim().ToLower();
        if (await _db.Users.AnyAsync(u => u.Email.ToLower() == email))
            return Error(409, "That email is already registered.");

        var role = await _db.Roles.SingleAsync(r => r.RoleName == request.Role);
        var user = new User
        {
            RoleId = role.RoleId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PasswordHash = PasswordHashing.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Created("/api/users/me", UsersController.ToProfile(user, role.RoleName));
    }

    /// <summary>#2 Log in. Starts a server-side session holding the user's ID and role.</summary>
    /// <response code="200">{ userId, fullName, email, role } plus the session cookie.</response>
    /// <response code="400">Email or password missing.</response>
    /// <response code="401">Wrong email or password (the same message for both).</response>
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLower();
        var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null || !PasswordHashing.Verify(request.Password, user.PasswordHash))
            return Error(401, "Incorrect email or password.");

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role!.RoleName);

        return Ok(new
        {
            user.UserId,
            FullName = $"{user.FirstName} {user.LastName}",
            user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>#3 Log out. Ends the session so the cookie stops working.</summary>
    /// <response code="204">Logged out.</response>
    /// <response code="401">No active session.</response>
    [HttpPost("logout")]
    [RequireSession]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
