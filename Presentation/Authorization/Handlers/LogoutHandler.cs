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
    private readonly IRequestContext _requestContext;
    private readonly IStringLocalizer<SharedResources> _localizer;

    #endregion

    #region Constructor
    public LogoutHandler(IRequestContext requestContext, IStringLocalizer<SharedResources> localizer)
    {
        _requestContext = requestContext;
        _localizer = localizer;
    }
    #endregion

    #region Authorization Handler
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, LogoutRequirement requirement)
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
            return Task.CompletedTask;
        }

        // Step 2: Validate user ID
        var userId = _requestContext.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
               _localizer[SharedResourcesKeys.AccessDenied]
            ));
            return Task.CompletedTask;
        }

        // Step 3: All validations passed
        context.Succeed(requirement);

        return Task.CompletedTask;
    }
    #endregion


}
