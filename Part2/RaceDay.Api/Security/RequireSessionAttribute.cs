using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Security;

// Put this on a controller or action to protect it.
// [RequireSession]                        -> any logged-in user
// [RequireSession(RoleNames.Organiser)]   -> only Organisers
// No session gives 401, the wrong role gives 403.
// It is an authorization filter, so it runs before the action and before model validation.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireSessionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _role;

    public RequireSessionAttribute(string? role = null)
    {
        _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32(SessionKeys.UserId);
        var role = session.GetString(SessionKeys.Role);

        if (userId == null || role == null)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 401,
                Title = "Unauthorized",
                Detail = "You need to log in first."
            }) { StatusCode = 401 };
            return;
        }

        if (_role != null && role != _role)
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Status = 403,
                Title = "Forbidden",
                Detail = $"Only {_role}s can do this. Your role is {role}."
            }) { StatusCode = 403 };
        }
    }
}
