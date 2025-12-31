using Domain.Security;
using Microsoft.AspNetCore.Authorization;

namespace Infrastructure.Security;
public class VerificationOnlyHandler : AuthorizationHandler<VerificationOnlyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, VerificationOnlyRequirement requirement)
    {
        var isVerificationToken = context.User.FindFirst(SessionTokenClaims.IsVerificationToken)?.Value;

        if (isVerificationToken == "true")
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
