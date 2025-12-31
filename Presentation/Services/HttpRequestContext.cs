using Interfaces;
using Microsoft.AspNetCore.Http;



namespace Presentation.Services;

public class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    private HttpContext? Context => _accessor.HttpContext;

    public string? AuthToken =>
        Context?.Request.Headers["Authorization"]
            .FirstOrDefault()?
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

    public string? ClientIP =>
        Context?.Connection.RemoteIpAddress?.ToString();

    public string Language
    {
        get
        {
            var lang = Context?.Request.Headers["Accept-Language"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(lang)
                ? "en"
                : lang.Split(',').First().Trim().ToLower();
        }
    }
}
