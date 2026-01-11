using Domain.Enums;
using Domain.Security;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Presentation.Authorization.Requirements;
using Serilog;
using System.Security.Claims;

namespace Presentation.Authorization.Handlers;

/// <summary>
/// Authorization handler for password reset flow
/// Validates:
/// 1. Token is a reset token
/// 2. Token is at the correct stage
/// 3. Token exists in database and is valid
/// </summary>
public class ResetPasswordHandler : AuthorizationHandler<ResetPasswordRequirement>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IRequestContext _context;
    private const string Error_Message = "Missing or invalid authentication token";
    public ResetPasswordHandler(IRefreshTokenRepository authService, IRequestContext context)
    {
        _refreshTokenRepo = authService;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ResetPasswordRequirement requirement)
    {
        // Step 1: Extract token from request context

        var token = _context.AuthToken;
        var jti = _context.TokenJti;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(jti))
        {
            context.Fail(new AuthorizationFailureReason(this, Error_Message));
            return;
        }

        // Step 2: Validate JWT claims

        var isResetToken = context.User.FindFirstValue(SessionTokenClaims.IsResetToken);
        var currentStageStr = context.User.FindFirstValue(SessionTokenClaims.ResetTokenStage);

        if (!int.TryParse(currentStageStr, out int currentStage) ||
           currentStage != (int)requirement.RequiredStage)
        {
            var expectedStage = requirement.RequiredStage.ToString();
            var actualStage = Enum.IsDefined(typeof(enResetPasswordStage), currentStage)
                ? ((enResetPasswordStage)currentStage).ToString()
                : "Unknown";

            context.Fail(new AuthorizationFailureReason(this, Error_Message));
            return;
        }


        try
        {
            var tokenEntity = await _refreshTokenRepo
                .GetTokenByJti(jti)
                .FirstOrDefaultAsync();

            // ✅ FIXED: Correct logic - token must exist AND be valid
            if (tokenEntity != null && tokenEntity.Type == enTokenType.ResetPasswordToken && tokenEntity.IsValid())
            {
                context.Succeed(requirement);
            }
            else
            {


                context.Fail(new AuthorizationFailureReason(this, Error_Message));
            }
        }
        catch (Exception ex)
        {
            // Log error but don't expose details to user
            Log.Error(ex, "Error validating reset token {Jti}", jti);

            context.Fail(new AuthorizationFailureReason(
                this,
                Error_Message
            ));
        }

    }
}
