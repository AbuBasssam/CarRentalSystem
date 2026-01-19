using ApplicationLayer.Resources;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Presentation.Authorization.Requirements;

namespace Presentation.Authorization.Handlers;

/// <summary>
/// Validates that user has a valid authentication token
/// Can be reused across multiple endpoints (Logout, Profile Update, Password Change, etc.)
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
        // Validate token exists
        var token = _requestContext.AuthToken;
        var jti = _requestContext.TokenJti;

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(jti))
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                _localizer[SharedResourcesKeys.InvalidToken]
            ));
            return Task.CompletedTask;
        }

        // Validate user ID
        var userId = _requestContext.UserId;
        if (!userId.HasValue || userId.Value <= 0)
        {
            context.Fail(new AuthorizationFailureReason(
                this,
                _localizer[SharedResourcesKeys.InvalidUserId]
            ));
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
