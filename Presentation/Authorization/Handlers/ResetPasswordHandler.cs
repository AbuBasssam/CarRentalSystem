using Domain.Enums;
using Domain.Security;
using Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Presentation.Authorization.Requirements;
using System.Security.Claims;

namespace Presentation.Authorization.Handlers;
public class ResetPasswordHandler : AuthorizationHandler<ResetPasswordRequirement>
{
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IRequestContext _context;

    public ResetPasswordHandler(IRefreshTokenRepository authService, IRequestContext context)
    {
        _refreshTokenRepo = authService;
        _context = context;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ResetPasswordRequirement requirement)
    {
        // 1. استخراج نص التوكن من الـ RequestContext
        var token = _context.AuthToken;
        var jti = _context.TokenJti;

        if (string.IsNullOrEmpty(token))
        {
            context.Fail();
            return;
        }

        // 2. التحقق من الـ Claims (المرحلة ونوع التوكن)
        // ملاحظة: ASP.NET Core قام بفك التوكن فعلياً ووضعه في context.User
        var isResetToken = context.User.FindFirstValue(SessionTokenClaims.IsResetToken);
        var currentStage = context.User.FindFirstValue(SessionTokenClaims.ResetTokenStage);

        // يجب أن يكون توكن Reset وبالمرحلة المطلوبة في الـ Requirement
        if (isResetToken != "true" || currentStage != ((int)requirement.RequiredStage).ToString())
        {
            context.Fail();
            return;
        }


        var tokenEntity = await _refreshTokenRepo
          .GetTableNoTracking()
          .Where(x => x.JwtId == jti && x.Type == enTokenType.ResetPasswordToken)
          .FirstOrDefaultAsync();


        var isValidInDb = tokenEntity == null || !tokenEntity.IsValid();

        if (isValidInDb)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

    }
}
