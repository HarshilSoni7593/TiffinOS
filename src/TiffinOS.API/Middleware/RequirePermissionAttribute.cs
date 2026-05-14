using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TiffinOS.API.Services;

namespace TiffinOS.API.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _permission;

    public RequirePermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var userContext = context.HttpContext.RequestServices
            .GetRequiredService<CurrentUserContext>();

        if (!userContext.IsAuthenticated)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Authentication required.",
                code = "UNAUTHENTICATED"
            });
            return;
        }

        if (!userContext.HasPermission(_permission))
        {
            context.Result = new ObjectResult(new
            {
                error = $"Permission denied. Required: {_permission}",
                code = "PERMISSION_DENIED"
            })
            { StatusCode = 403 };
        }
    }
}