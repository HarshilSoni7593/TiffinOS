using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TiffinOS.API.Services;

namespace TiffinOS.API.Middleware;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireModuleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string _module;

    public RequireModuleAttribute(string module)
    {
        _module = module;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tenantContext = context.HttpContext.RequestServices
            .GetRequiredService<TenantContext>();

        if (!tenantContext.EnabledModules.Contains(_module))
        {
            context.Result = new ObjectResult(new
            {
                error = $"Your plan does not include the {_module} module.",
                code = "MODULE_NOT_ENABLED"
            })
            { StatusCode = 403 };
        }
    }
}