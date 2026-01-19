using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using System.Net;

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
    private readonly TimeSpan _window = TimeSpan.FromMinutes(1);

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
            entry.AbsoluteExpirationRelativeToNow = _window;

            entry.Priority = CacheItemPriority.Low;

            return new RateLimitEntry
            {
                Count = 0,
                ExpiresAt = DateTime.UtcNow.Add(_window)
            };
        });

        Interlocked.Increment(ref rateLimitEntry!.Count);

        if (rateLimitEntry.Count > _maxRequestsPerPeriod)
        {
            Log.Warning("Rate limit exceeded for {Identifier}.", identifier);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            await context.Response.WriteAsync("Too Many Requests. Please try again later.");

            var retryAfter = (int)(rateLimitEntry.ExpiresAt - DateTime.UtcNow).TotalSeconds;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;

            await context.Response.WriteAsJsonAsync(new
            {
                statusCode = 429,
                message = "Too many requests. Please try again later.",
                retryAfterSeconds = retryAfter
            });
            return;
        }


        // Allow the request to continue down the pipeline.
        await _next(context);
    }

    /// <summary>
    /// Determines the client identifier used for rate limiting.
    /// </summary>
    private string GetClientIdentifier(HttpContext context)
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
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
internal class RateLimitEntry
{
    public int Count;
    public DateTime ExpiresAt { get; set; }
}