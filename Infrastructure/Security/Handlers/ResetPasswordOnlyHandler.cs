using Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Security;

public class ResetPasswordOnlyHandler : AuthorizationHandler<ResetPasswordOnlyRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ResetPasswordOnlyRequirement requirement)
    {
        var isResetToken = context.User.FindFirst(SessionTokenClaims.IsResetToken)?.Value;

        if (isResetToken == "true")
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
