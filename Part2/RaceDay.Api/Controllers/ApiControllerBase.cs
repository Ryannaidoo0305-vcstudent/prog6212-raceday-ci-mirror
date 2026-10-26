using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Security;

namespace RaceDay.Api.Controllers;

// Shared helpers for every controller: who is logged in, and short ways to return errors.
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    // Only called on actions with [RequireSession], so the session is always set here.
    protected int CurrentUserId => HttpContext.Session.GetInt32(SessionKeys.UserId) ?? 0;
    protected string CurrentRole => HttpContext.Session.GetString(SessionKeys.Role) ?? "";

    protected ObjectResult Error(int status, string detail)
    {
        return Problem(detail: detail, statusCode: status);
    }

    protected ObjectResult NotYourEvent()
    {
        return Error(403, "You can only manage your own events.");
    }
}
