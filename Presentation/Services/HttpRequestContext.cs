using Domain.AppMetaData;
using Interfaces;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;



namespace Presentation.Services;

public class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpRequestContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public HttpContext? Context => _accessor.HttpContext;
    private bool IsAuthenticated
    {
        get
        {
            if (Context?.User?.Identity?.IsAuthenticated != true)
                return false;

            return true;
        }
    }

    public string? AuthToken
    {
        get
        {
            string? headertoken = HeaderToken();

            // Extract token from Cookies
            return !string.IsNullOrEmpty(headertoken) ? headertoken : Context?.Request.Cookies[Keys.Access_Token_Key];

        }
    }

    private string? HeaderToken()
    {


        // Extract Bearer token from Authorization header
        return Context?.Request.Headers["Authorization"]
            .FirstOrDefault()?
            .Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    public string? ClientIP
    {
        get
        {


            return Context?.Request.Headers["X-Forwarded-For"].FirstOrDefault() ??
            Context?.Connection.RemoteIpAddress?.ToString();
        }
    }

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
    public int? UserId =>
        int.TryParse(Context?.User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
    public string? TokenJti
    {
        get
        {
            if (!IsAuthenticated)
                return null;

            return Context?.User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        }
    }

    public string? Email
    {
        get
        {
            if (!IsAuthenticated)
                return null;

            return Context?.User.FindFirstValue(ClaimTypes.Email) ?? null;
        }
    }

    public string? UserAgent => Context?.Request.Headers["User-Agent"];
    public string? RefreshToken => Context?.Request.Cookies[Keys.Refresh_Token_Key];
}
