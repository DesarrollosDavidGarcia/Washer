using TallerPro.Security;

namespace TallerPro.Api.Middleware;

/// <summary>
/// Resolves the current tenant from the X-TallerPro-Tenant request header and sets it on
/// <see cref="ITenantContext"/>. The try/finally ensures <see cref="ITenantContext.Clear"/> is
/// always called — even if TrySetTenant or a downstream middleware throws — preventing AsyncLocal
/// contamination across requests (R-2).
/// </summary>
internal sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
    {
        try
        {
            if (context.Request.Headers.TryGetValue("X-TallerPro-Tenant", out var raw)
                && long.TryParse(raw, out var tenantId)
                && tenantId > 0)
            {
                tenantContext.TrySetTenant(tenantId);
            }

            await _next(context);
        }
        finally
        {
            tenantContext.Clear();
        }
    }
}
