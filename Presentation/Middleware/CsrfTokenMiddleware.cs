using Domain.AppMetaData;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PresentationLayer.Middleware;

/// <summary>
/// CSRF Value Middleware for API endpoints
/// </summary>
public class CsrfTokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAntiforgery _antiforgery;

    public CsrfTokenMiddleware(RequestDelegate next, IAntiforgery antiforgery)
    {
        _next = next;
        _antiforgery = antiforgery;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only when requesting CSRF token
        if (context.Request.Path.StartsWithSegments($"/{Router.AuthenticationRouter.CSRF_Token}"))
        {
            /* // Send in Body
            // GetAndStoreTokens does the following:
            // 1. Creates a Cookie Token and automatically stores it in a Cookie
            // 2. Returns the Request Token which should be sent in the Header
            var tokens = _antiforgery.GetAndStoreTokens(context);

            // Return only the Request Token
            // The Cookie Token is automatically stored by GetAndStoreTokens
            await context.Response.WriteAsJsonAsync(new
            {
                token = tokens.RequestToken,
                message = "Send this token in X-XSRF-TOKEN header for protected requests"
            });

            return;
            */

            //Send in Cookie
            var tokens = _antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append(
           Keys.CSRF_Token_Header_Key,
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,  // Allow JavaScript to read the cookie
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            }
            );

        }

        await _next(context);
    }
}

/// <summary>
/// Extension method for middleware registration
/// </summary>
public static class CsrfTokenMiddlewareExtensions
{
    public static IApplicationBuilder UseCsrfToken(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfTokenMiddleware>();
    }
}