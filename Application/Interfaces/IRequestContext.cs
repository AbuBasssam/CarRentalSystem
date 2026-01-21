namespace Interfaces;

/// <summary>
/// Provides access to the current HTTP request data, such as headers, 
/// user information, and client details.
/// </summary>
public interface IRequestContext : IScopedService
{
    /// <summary>Gets the raw Authorization token from the header.</summary>
    string? AuthToken { get; }

    /// <summary>Gets the client's IP address.</summary>
    string? ClientIP { get; }

    /// <summary>Gets the User-Agent string of the client.</summary>
    string? UserAgent { get; }

    /// <summary>Gets the preferred language from the request.</summary>
    string? Language { get; }

    /// <summary>Gets the authenticated User ID if available.</summary>
    int? UserId { get; }

    /// <summary>Gets the JTI of the current access token.</summary>
    string? TokenJti { get; }

    /// <summary>Gets the email of the current authenticated user.</summary>
    string? Email { get; }

    /// <summary>Gets the refresh token from the cookies. </summary>
    string? RefreshToken { get; }
}