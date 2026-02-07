using Application.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Net;

namespace Presentation.Filters;

/// <summary>
/// Custom CSRF validation attribute that works with header-based tokens
/// </summary>

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ValidateCsrfTokenAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var antiforgery = httpContext.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            // ASP.NET Core Antiforgery automatically validates:
            // 1. Presence of Cookie Token
            // 2. Presence of Request Token (in Header based on HeaderName in Program.cs)
            // 3. Cryptographic relationship between them (not identical but cryptographically linke

            await antiforgery.ValidateRequestAsync(httpContext);

            // If validation succeeds, proceed to the Action
            await next();
        }
        catch (AntiforgeryValidationException ex)
        {
            // Log the error for debugging assistance
            LogCsrfError(httpContext, ex);

            // Create unified error response
            var responseModel = new Response<string>
            {
                Succeeded = false,
                StatusCode = HttpStatusCode.Forbidden,
                Message = "Invalid or missing CSRF token",
                Errors = new List<string>
                {
                    "CSRF token validation failed",
                    ex.Message
                }
            };

            // Set the result with proper status code
            context.Result = new ObjectResult(responseModel)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };

        }
        catch (Exception e)
        {

            Log.Warning("[CSRF Validation Failed] {@ErrorDetails}", e.Message);
            var responseModel = new Response<string>
            {
                Succeeded = false,
                StatusCode = HttpStatusCode.Forbidden,
                Message = "CSRF token validation error",
                Errors = new List<string>
                {
                    "An error occurred while validating CSRF token"
                }
            };

            context.Result = new ObjectResult(responseModel)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };


        }
    }

    private void LogCsrfError(HttpContext context, AntiforgeryValidationException ex)
    {
        var hasHeader = context.Request.Headers.TryGetValue("X-XSRF-TOKEN", out var headerValue);
        var hasCookie = context.Request.Cookies.TryGetValue("XSRF-TOKEN", out var cookieValue);

        var errorDetails = new
        {
            Path = context.Request.Path.ToString(),
            Method = context.Request.Method,
            Exception = ex.Message,
            HasCsrfCookie = hasCookie,
            HasCsrfHeader = hasHeader,
            CookiePreview = hasCookie && cookieValue != null
                ? cookieValue.Substring(0, Math.Min(30, cookieValue.Length)) + "..."
                : "N/A",
            HeaderPreview = hasHeader && !string.IsNullOrEmpty(headerValue)
                ? headerValue.ToString().Substring(0, Math.Min(30, headerValue.ToString().Length)) + "..."
                : "N/A"
        };




        Console.WriteLine($"[CSRF Error] Path: {errorDetails.Path}");
        Console.WriteLine($"[CSRF Error] Method: {errorDetails.Method}");
        Console.WriteLine($"[CSRF Error] Exception: {errorDetails.Exception}");
        Console.WriteLine($"[CSRF Error] Has Cookie: {errorDetails.HasCsrfCookie}");
        Console.WriteLine($"[CSRF Error] Has Header: {errorDetails.HasCsrfHeader}");
        Console.WriteLine($"[CSRF Error] Cookie Preview: {errorDetails.CookiePreview}");
        Console.WriteLine($"[CSRF Error] Header Preview: {errorDetails.HeaderPreview}");

    }
}