using System.Security.Claims;
using NarrationApp.Shared.Constants;

namespace NarrationApp.Server.Middleware;

public sealed class JwtMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Items[AppConstants.HttpContextUserIdKey] = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            context.Items[AppConstants.HttpContextUserRoleKey] = context.User.FindFirstValue(ClaimTypes.Role);
        }

        await next(context);
    }
}
