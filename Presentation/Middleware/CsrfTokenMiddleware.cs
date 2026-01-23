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
        // Create and store the antiforgery tokens
        var tokens = _antiforgery.GetAndStoreTokens(context);

        // Set the CSRF token in a cookie
        context.Response.Cookies.Append(
            "XSRF-TOKEN",
            tokens.RequestToken!,
            new CookieOptions
            {
                HttpOnly = false,  // Allow JavaScript to read the cookie
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
            }
        );

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