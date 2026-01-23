using Domain.AppMetaData;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using Timer = System.Threading.Timer;

public class SensitiveRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Timer _cleanupTimer;

    // Thread-safe in-memory store for request counts
    private readonly ConcurrentDictionary<string, (int Count, DateTime ResetTime, TimeSpan CurrentWindow)> _requestCounts = new();

    // Use route constants from Router class and normalize keys
    private readonly Dictionary<string, (int Limit, TimeSpan Window)> _endpointRateLimits = new()
    {

        { Normalize(Router.AuthenticationRouter.SignIn), (5, TimeSpan.FromMinutes(15)) },

        { Normalize(Router.AuthenticationRouter.SignUp), (2, TimeSpan.FromHours(1)) },

        { Normalize(Router.AuthenticationRouter.EmailConfirmation), (5, TimeSpan.FromMinutes(3)) },
        { Normalize(Router.AuthenticationRouter.PasswordReset), (5, TimeSpan.FromMinutes(15)) },
        { Normalize(Router.AuthenticationRouter.PasswordResetVerification), (3, TimeSpan.FromMinutes(5)) },
        { Normalize(Router.AuthenticationRouter.Password), (5, TimeSpan.FromMinutes(5)) },

        { Normalize(Router.AuthenticationRouter.ResendVerification), (5, TimeSpan.FromHours(24)) },
        { Normalize(Router.AuthenticationRouter.ResendPasswordReset), (5, TimeSpan.FromHours(24)) },

        { Normalize(Router.AuthenticationRouter.RefreshToken), (5, TimeSpan.FromMinutes(30)) },
        { Normalize(Router.AuthenticationRouter.CSRF_Token), (20, TimeSpan.FromMinutes(5)) }

    };

    public SensitiveRateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;

        // Starts cleanup every 5 minutes to avoid memory leaks
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = Normalize(context.Request.Path.Value);
        var ipAddress = GetClientIpAddress(context);

        if (_endpointRateLimits.TryGetValue(path, out var rateLimit))
        {
            string key = $"{ipAddress}:{path}";
            var now = DateTime.UtcNow;

            var entry = _requestCounts.AddOrUpdate(
                key,
                _ => (1, now.Add(rateLimit.Window), rateLimit.Window),
                (_, current) =>
                {
                    // 1.if window has expired, reset count
                    if (now > current.ResetTime)
                        return (1, now.Add(rateLimit.Window), rateLimit.Window);

                    // 2.If within limit, increment count
                    if (current.Count < rateLimit.Limit)
                        return (current.Count + 1, current.ResetTime, current.CurrentWindow);

                    // 3. Exceeds limit, apply penalty by increasing window
                    // Doubling the window time up to a maximum of 24 hours
                    var newWindow = current.CurrentWindow.TotalHours < 24
                                    ? current.CurrentWindow.Multiply(2)
                                    : current.CurrentWindow;

                    return (current.Count + 1, now.Add(newWindow), newWindow);
                });

            // Check if limit exceeded
            if (entry.Count > rateLimit.Limit)
            {
                var retryAfterSeconds = (int)Math.Max(0, (entry.ResetTime - now).TotalSeconds);

                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();

                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 429,
                    succeeded = false,
                    message = "Too many attempts. A penalty has been applied to your wait time.",
                    retryAfter = retryAfterSeconds
                });
                return;
            }
        }

        await _next(context);
    }

    /// <summary>
    ///Normalizes a route to lowercase and trims trailing slash
    /// </summary>
    private static string Normalize(string? path)
    {
        if (string.IsNullOrEmpty(path)) return "/";

        path = path.ToLowerInvariant().TrimEnd('/');

        return path.StartsWith('/') ? path : "/" + path;
    }


    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedHeader))
            return forwardedHeader.Split(',')[0].Trim();

        var realIpHeader = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIpHeader))
            return realIpHeader;

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }


    /// <summary>
    ///Removes expired rate limit entries to prevent memory leak
    /// </summary>
    private void CleanupExpiredEntries(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _requestCounts
            .Where(kvp => now > kvp.Value.ResetTime.AddMinutes(10))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _requestCounts.TryRemove(key, out _);
    }
}
