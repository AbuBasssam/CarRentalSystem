using Domain.Enums;
using Domain.Security;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Presentation.Authorization.Requirements;

namespace Presentation.Authorization.Handlers;
public class VerificationOnlyHandler : AuthorizationHandler<VerificationOnlyRequirement>
{
    private readonly IAuthService _authService;
    private readonly IRequestContext _context;

    public VerificationOnlyHandler(IAuthService authService, IRequestContext context)
    {
        _authService = authService;
        _context = context;
    }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerificationOnlyRequirement requirement)
    {
        var token = _context.AuthToken;

        if (string.IsNullOrEmpty(token))
        {
            context.Fail();
            return;
        }
        var isVerificationToken = context.User.FindFirst(SessionTokenClaims.IsVerificationToken)?.Value;

        if (isVerificationToken != "true")
        {
            context.Fail();
        }

        var isTokenValid = await _authService.ValidateSessionToken(token, enTokenType.VerificationToken);
        if (isTokenValid)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }


    }
}
