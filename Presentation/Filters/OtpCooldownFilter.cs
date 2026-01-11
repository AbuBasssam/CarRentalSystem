using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Infrastructure.Filters;

/// <summary>
/// Action filter to enforce OTP cooldown period
/// Prevents rapid resend requests for OTP codes
/// </summary>
public class OtpCooldownFilter : IAsyncActionFilter
{
    private readonly IOtpRepository _otpRepo;
    private readonly IRequestContext _requestContext;
    private readonly enOtpType _otpType;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;


    public OtpCooldownFilter(
        IOtpRepository otpRepo,
        IRequestContext requestContext,
        IStringLocalizer<SharedResources> localizer,
        ResponseHandler responseHandler,
        enOtpType otpType)
    {
        _otpRepo = otpRepo;
        _requestContext = requestContext;
        _localizer = localizer;
        _responseHandler = responseHandler;
        _otpType = otpType;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // ============================================================
        // Step 1: Get UserId (user must already be authenticated)
        // ============================================================
        var userId = _requestContext.UserId;

        if (!userId.HasValue || userId.Value <= 0)
        {
            var response = _responseHandler.Unauthorized<string>();
            context.Result = new UnauthorizedObjectResult(response);
            return;
        }

        // ============================================================
        // Step 2: Get last OTP for user (ANY status)
        // ============================================================
        var lastOtp = await _otpRepo
            .GetLatestValidOtpAsync(userId.Value, _otpType)
            .FirstOrDefaultAsync();

        if (lastOtp is null)
        {
            // User never requested OTP before → allow
            await next();
            return;
        }

        // ============================================================
        // Step 3: Check cooldown
        // ============================================================

        var canResendResult = lastOtp.CanResend();
        if (!canResendResult.canResend)
        {
            var secondsRemaining = (int)Math.Ceiling(canResendResult.remaining!.Value.TotalSeconds);

            var errorMessage = string.Format(
                _localizer[SharedResourcesKeys.ResendCooldown],
                secondsRemaining
            );

            context.Result = new BadRequestObjectResult(
                _responseHandler.BadRequest<string>(errorMessage)
            );
            return;
        }


        await next();
    }
}

