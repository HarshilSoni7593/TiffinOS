using Microsoft.EntityFrameworkCore;
using TiffinOS.API.Data;
using TiffinOS.API.Services;

namespace TiffinOS.API.Middleware
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            TenantContext tenantcontext,
            AppDbContext db
            )
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (path.StartsWith("/swagger") ||
                path.StartsWith("/health") ||
                path.StartsWith("/api/platform") ||
                path.StartsWith("/api/auth/register") ||
                path.StartsWith("/api/auth/refresh") ||
                path.StartsWith("/api/auth/logout") ||
                path.StartsWith("/api/auth/forgot-password") ||
                path.StartsWith("/api/auth/reset-password"))
            {
                await _next(context);
                return;
            }

            var tenantSlug = ResolveTenantSlug(context);

            if (string.IsNullOrEmpty(tenantSlug))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Tenant could not be identified.",
                    code = "TENANT_MISSING"
                });
                return;
            }

            var tenant = await db.Tenants
                .AsNoTracking()
                .Where(t => t.Slug == tenantSlug && t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.Slug,
                    t.Name
                })
                .FirstOrDefaultAsync();

            if (tenant == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Tenant not found or inactive.",
                    code = "TENANT_NOT_FOUND"
                });
                return;
            }

            var enabledModules = await db.TenantModules
                .AsNoTracking()
                .Where(tm => tm.TenantId == tenant.Id && tm.IsActive)
                .Select(tm => tm.ModuleSlug)
                .ToArrayAsync();

            tenantcontext.TenantId = tenant.Id;
            tenantcontext.TenantSlug = tenant.Slug;
            tenantcontext.TenantName = tenant.Name;
            tenantcontext.EnabledModules = enabledModules;
            tenantcontext.IsResolved = true;

            await _next(context);
        }

        public static string? ResolveTenantSlug(HttpContext context)
        {
            var headerSlug = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
            if(!string.IsNullOrEmpty(headerSlug)) 
            {
                return headerSlug.ToLower().Trim();
            }

            var host = context.Request.Host.Host;
            var parts = host.Split('.');

            if(parts.Length >= 3 && parts[0] != "www" && parts[0] != "api")
            {
                return parts[0].ToLower().Trim();
            }

            return null;
        }
    }
}
