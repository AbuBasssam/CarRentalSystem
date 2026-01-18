using ApplicationLayer.Resources;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Presentation.Authorization.Requirements;

namespace Presentation.Authorization.Handlers;

/// <summary>
/// Handles token validation and extraction before logout processing
/// Works as authorization middleware before reaching the command handler
/// </summary>
public class LogoutHandler : AuthorizationHandler<LogoutRequirement>
{
    #region Fields
    private readonly ITokenValidationService _tokenValidation;

    private readonly IRequestContext _requestContext;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructor
    public LogoutHandler(ITokenValidationService tokenValidation, IRequestContext requestContext, IStringLocalizer<SharedResources> localizer)
    {
        _requestContext = requestContext;
        _tokenValidation = tokenValidation;

        _localizer = localizer;
    }
    #endregion

    #region Authorization Handler
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, LogoutRequirement requirement)
    {
        // Step 1: Validate authentication token exists
        var token = _requestContext.AuthToken;
        var jti = _requestContext.TokenJti;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(jti))
        {
            context.Fail(new AuthorizationFailureReason(
                this,
               _localizer[SharedResourcesKeys.AccessDenied]
            ));
            return;
        }

        // Step 2: Validate user ID
        var userId = _requestContext.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
               _localizer[SharedResourcesKeys.AccessDenied]
            ));
            return;
        }
        // step 3: Validate token
        var isValidToken = await _tokenValidation.ValidateTokenAsync(jti);
        if (!isValidToken)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
               _localizer[SharedResourcesKeys.AccessDenied]
            ));
            return;
        }


        // Step 4: All validations passed
        context.Succeed(requirement);

        return;
    }
    #endregion


}
