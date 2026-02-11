using ApplicationLayer.Resources;
using Domain.AppMetaData;
using Domain.Enums;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Presentation.Authorization.Requirements;

namespace Presentation.Authorization.Handlers;

/// <summary>
/// Validates that user has a valid authentication token
/// Can be reused across multiple endpoints (Logout, Profile Update, Password Change, etc.)
/// 
/// ENHANCED VERSION:
/// - Returns specific error messages for different failure scenarios
/// - Works with CustomAuthorizationMiddlewareResultHandler to provide unified Response
/// </summary>
public class ValidTokenHandler : AuthorizationHandler<ValidTokenRequirement>
{
    private readonly IRequestContext _requestContext;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public ValidTokenHandler(
        IRequestContext requestContext,
        IStringLocalizer<SharedResources> localizer)
    {
        _requestContext = requestContext;
        _localizer = localizer;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ValidTokenRequirement requirement)
    {
        var httpContext = context.Resource as DefaultHttpContext;
        if (httpContext == null) return Task.CompletedTask;
        var isTokenValidationEndpoint = httpContext.Request.Path.Value?
        .Equals("/" + Router.AuthenticationRouter.TokenValidation, StringComparison.OrdinalIgnoreCase) ?? false;

        // ============================================
        // STEP 1: Validate Token Exists
        // ============================================
        var token = _requestContext.AuthToken;
        var jti = _requestContext.TokenJti;

        if (string.IsNullOrEmpty(token))
        {
            // NO TOKEN AT ALL - This is a new user or completely logged out
            // Frontend should NOT attempt refresh for this case
            context.Fail(new AuthorizationFailureReason(
                this,
                _localizer[SharedResourcesKeys.MissingToken]
            ));
            if (httpContext != null)
            {
                httpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                {
                    ErrorCode = enErrorCode.MissingToken.ToString(),
                    IsRecoverable = isTokenValidationEndpoint
                };
            }

            return Task.CompletedTask;
        }
        // ============================================
        // STEP 2: Validate JTI (Token ID)
        // ============================================
        if (string.IsNullOrEmpty(jti))
        {
            // Token exists but has no JTI - corrupted or invalid
            context.Fail(new AuthorizationFailureReason(
                this,
                _localizer[SharedResourcesKeys.InvalidToken]
            ));
            if (httpContext != null)
            {
                httpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                {
                    ErrorCode = enErrorCode.InvalidToken.ToString(),
                    IsRecoverable = isTokenValidationEndpoint
                };
            }


            return Task.CompletedTask;
        }

        // ============================================
        // STEP 3: Validate User ID
        // ============================================
        var userId = _requestContext.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                _localizer[SharedResourcesKeys.InvalidToken]
            ));
            if (httpContext != null)
            {
                httpContext.Items[Keys.Auth_Error_Metadata_Key] = new
                {
                    ErrorCode = enErrorCode.InvalidToken.ToString(),
                    IsRecoverable = isTokenValidationEndpoint
                };
            }


            return Task.CompletedTask;
        }

        // ============================================
        // STEP 4: Success - All Validations Passed
        // ============================================
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}