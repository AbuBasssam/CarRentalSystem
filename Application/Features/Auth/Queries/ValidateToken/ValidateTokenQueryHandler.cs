using Application.Models;
using Domain.Entities;
using Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Application.Features.AuthFeature;

/// <summary>
/// Handler for token validation query
/// Validates current access token and returns user data if valid
/// 
/// NOTE: Most validation is done by [Authorize(Policy = Policies.ValidToken)]
/// This handler only fetches additional user data not in JWT claims
/// </summary>
public class ValidateTokenQueryHandler : IRequestHandler<ValidateTokenQuery, Response<TokenValidationResponseDTO>>
{
    #region Fields

    private readonly IRequestContext _requestContext;
    private readonly UserManager<User> _userManager;
    private readonly ResponseHandler _responseHandler;

    // Response timing configuration to prevent timing attacks
    // Should match typical database query latency (measure your DB performance)
    private const int MIN_RESPONSE_TIME_MS = 50;
    private const int MAX_RESPONSE_TIME_MS = 150;

    #endregion

    #region Constructor

    public ValidateTokenQueryHandler(IRequestContext requestContext, UserManager<User> userManager, ResponseHandler responseHandler)
    {
        _requestContext = requestContext;
        _userManager = userManager;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Handler

    public async Task<Response<TokenValidationResponseDTO>> Handle(ValidateTokenQuery request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        try
        {

            var userId = _requestContext.UserId!.Value;

            // Get user from database - only need FirstName, LastName, ImagePath
            var user = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new
                {
                    u.FirstName,
                    u.LastName,
                    u.ImagePath
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (user == null)
            {
                // Constant - time response to prevent user enumeration

                await EnsureMinimumResponseTime(startTime, cancellationToken);

                // This should rarely happen (token valid but user deleted?)

                Log.Warning("Token valid but user not found. UserId: {UserId}", userId);

                return _responseHandler.Unauthorized<TokenValidationResponseDTO>();
            }

            // Build minimal response - frontend extracts other data from JWT
            var responseDto = new TokenValidationResponseDTO
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                ImagePath = user.ImagePath
            };

            // Ensure consistent response time (prevents timing attacks)

            await EnsureMinimumResponseTime(startTime, cancellationToken);

            Log.Information("Token validated successfully for user {UserId}", userId);

            return _responseHandler.Success(responseDto);
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled - don't log as error

            Log.Debug("Token validation cancelled for user {UserId}", _requestContext.UserId);

            throw; // Re-throw to let ASP.NET handle it
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during token validation");
            return _responseHandler.InternalServerError<TokenValidationResponseDTO>();
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Ensures consistent response time to prevent timing attacks
    /// Calculates elapsed time and adds random delay to reach target range
    /// </summary>
    /// <param name="startTime">When the request started processing</param>
    /// <param name="cancellationToken">Token to respect request cancellation</param>
    private async Task EnsureMinimumResponseTime(DateTime startTime, CancellationToken cancellationToken)
    {
        try
        {
            var elapsed = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var targetDelay = Random.Shared.Next(MIN_RESPONSE_TIME_MS, MAX_RESPONSE_TIME_MS);
            var remainingDelay = targetDelay - (int)elapsed;

            if (remainingDelay > 0)
            {
                await Task.Delay(remainingDelay, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // If request is cancelled during delay, that's fine
            // Just exit without completing the delay
        }
    }

    #endregion
}
