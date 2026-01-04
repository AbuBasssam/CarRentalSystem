using Application.Models;
using ApplicationLayer.Resources;
using Domain.Enums;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

public class OtpCooldownFilter : IAsyncActionFilter
{

    #region Field(s)
    private readonly IOtpRepository _otpRepo;
    private readonly IRequestContext _requestContext;
    private readonly IAuthService _authService;

    private readonly enOtpType _otpType;

    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly ResponseHandler _responseHandler;

    #endregion

    #region Constructor(s)

    public OtpCooldownFilter(IOtpRepository otpRepo, IRequestContext requestContext, IAuthService authService,
        enOtpType otpType, IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {
        _otpRepo = otpRepo;
        _requestContext = requestContext;
        _authService = authService;
        _otpType = otpType;
        _localizer = localizer;
        _responseHandler = responseHandler;
    }

    #endregion

    #region Method(s)

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var token = _requestContext.AuthToken;

        if (string.IsNullOrEmpty(token))
        {
            var response = _responseHandler.Unauthorized<string>();
            var result = new UnauthorizedObjectResult(response);

            context.Result = result;
            return;
        }

        var userIdResult = _authService.GetUserIdFromSessionToken(token);
        if (!userIdResult.IsSuccess)
        {
            var response = _responseHandler.Unauthorized<string>();
            var result = new UnauthorizedObjectResult(response);
            context.Result = result;
            return;
        }

        int userId = userIdResult.Data;

        var lastOtp = await _otpRepo.GetLatestValidOtpAsync(userId, _otpType).FirstOrDefaultAsync();
        var cooldown = lastOtp?.GetRemainingCooldown();

        if (cooldown.HasValue)
        {
            var time = cooldown.Value;

            string formattedTime = string.Format("{0:00}:{1:00}", (int)time.TotalMinutes, time.Seconds);

            var errMessage = string.Format(_localizer[SharedResourcesKeys.ResendCooldown], formattedTime);

            var response = _responseHandler.BadRequest<string>(string.Format(_localizer[SharedResourcesKeys.ResendCooldown], Math.Ceiling(cooldown.Value.TotalSeconds)));

            var result = new BadRequestObjectResult(response);

            context.Result = result;

            return;
        }

        await next();
    }

    #endregion
}