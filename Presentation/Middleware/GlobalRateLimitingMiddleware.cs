using Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Text.Json;

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
            var retryAfterSeconds = (int)(rateLimitEntry.ExpiresAt - DateTime.UtcNow).TotalSeconds;

            // Create ApiResponse directly without ResponseHandler
            var responseModel = new Response<string>
            {
                StatusCode = HttpStatusCode.TooManyRequests,
                Succeeded = false,
                Message = "Too many requests. Please try again later.",
                Errors = new List<string> { "Too many requests. Please try again later." },
                Meta = new { retryAfterSeconds }
            };

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";

            // Serialize with camelCase
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var json = JsonSerializer.Serialize(responseModel, options);
            await context.Response.WriteAsync(json);

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