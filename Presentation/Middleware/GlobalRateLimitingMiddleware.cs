using Domain.HelperClasses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace PresentationLayer.Middleware;

/// <summary>
/// A Global middleware to rate limiting.
/// </summary>
public class GlobalRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;


    // Customize your limits here.
    private readonly int _maxRequestsPerPeriod = 100;
    private readonly TimeSpan _period = TimeSpan.FromMinutes(1);

    public GlobalRateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;

    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Identify the client based on authentication or combination of IP & User-Agent.
        string identifier = GetClientIdentifier(context);

        // Create a unique key for caching the rate limit info.
        string cacheKey = $"RateLimit_{identifier}";

        // Get or create a new rate limit entry for the client.
        var rateLimitEntry = _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = _period;
            return new RateLimitEntry
            {
                Count = 0,
                ExpiresAt = DateTime.UtcNow.Add(_period)
            };
        });

        // Increment the counter.
        rateLimitEntry!.Count++;

        if (rateLimitEntry.Count > _maxRequestsPerPeriod)
        {
            Log.Warning("Rate limit exceeded for {Identifier}.", identifier);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.Response.WriteAsync("Too Many Requests. Please try again later.");

            //context.Response.Headers["Retry-After"] = _period.TotalSeconds.ToString();

            return;
        }

        //// Optionally, add headers to help the client understand its rate limit status.
        //context.Response.Headers["X-RateLimit-Limit"] = _maxRequestsPerPeriod.ToString();
        //context.Response.Headers["X-RateLimit-Remaining"] = (_maxRequestsPerPeriod - rateLimitEntry.Count).ToString();
        //context.Response.Headers["X-RateLimit-Reset"] =
        //    ((int)(rateLimitEntry.ExpiresAt - DateTime.UtcNow).TotalSeconds).ToString();

        // Allow the request to continue down the pipeline.
        await _next(context);
    }

    /// <summary>
    /// Determines the client identifier used for rate limiting.
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        // If the user is authenticated, use their unique username or ID.
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            // For example, using the Name claim.
            return context.User.Identity.Name!;
        }
        else
        {
            // Otherwise, combine the remote IP address and User-Agent header.
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
            var userAgent = context.Request.Headers["User-Agent"].ToString() ?? "unknown-agent";
            return $"{ip}:{userAgent}";
        }
    }
}
