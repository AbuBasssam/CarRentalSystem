using Application.Models;
using Domain.Entities;
using Domain.HelperClasses;

namespace Interfaces;

/// <summary>
/// Service interface for authentication and authorization operations.
/// Manages JWT generation, token validation, and session handling.
/// </summary>
public interface IAuthService : IScopedService
{
    /// <summary>
    /// Generates a complete JWT authentication result (Access and Refresh tokens) for a user.
    /// </summary>
    Task<JwtAuthResult> GetJwtAuthForuser(User User);

    /// <summary>
    /// Validates a refresh token against the stored version in the database.
    /// </summary>
    Task<(UserToken?, Exception?)> ValidateRefreshToken(int UserId, string refreshToken, string jwtId);

    /// <summary>
    /// Logout user by revoking their tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="jwtId">JWT ID to revoke</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> Logout(int userId, string jwtId);

    /// <summary>
    /// Logout from all devices by revoking all user tokens
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result<bool>> LogoutFromAllDevices(int userId);



}
