using Application.Models;
using ApplicationLayer.Resources;
using Domain.Entities;
using Domain.HelperClasses;
using Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Serilog;

namespace Application.Features.AuthFeature;

public class SignInCommandHandler : IRequestHandler<SignInCommand, Response<JwtAuthResult>>
{
    #region Field
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IStringLocalizer<SharedResources> _Localizer;
    private readonly ResponseHandler _responseHandler;
    #endregion

    #region Constructor
    public SignInCommandHandler(IUserService userService, IAuthService authService,
        IStringLocalizer<SharedResources> localizer, ResponseHandler responseHandler)
    {
        _userService = userService;
        _authService = authService;
        _Localizer = localizer;
        _responseHandler = responseHandler;

    }
    #endregion

    #region Handler

    public async Task<Response<JwtAuthResult>> Handle(SignInCommand request, CancellationToken cancellationToken)
    {
        // Always fetch user (even if email doesn't exist)
        var user = await _userService.GetUserByEmailAsync(request.Email).FirstOrDefaultAsync();

        var userNotExists = _UserNotExists(user);

        // Use a dummy Password hash for non-existent users to force constant-time comparison
        var passwordToCheck = userNotExists ? "dummy_password" : request.Password;

        // Always call CheckPasswordAsync (critical for timing attack protection)
        var confirmedUser = await _IsConfirmedPassword(user!, passwordToCheck);
        var isValid = !userNotExists && confirmedUser.Succeeded;

        return isValid ?
            await _HandleSuccessfulLoginAsync(user!) :
            _HandleFailedLoginAsync(request.Email);

    }

    #endregion

    #region Helpers Methods
    private bool _UserNotExists(User? user) => user == null;

    private async Task<SignInResult> _IsConfirmedPassword(User user, string password) => await _userService.CheckPasswordAsync(user, password);

    private Response<JwtAuthResult> _HandleFailedLoginAsync(string? email = null)
    {

        _LogFailedLoginAttempt(attemptedEmail: email);

        return _CreateErrorResponse();
    }



    private void _LogFailedLoginAttempt(string? attemptedEmail)
    {
        const string logTitle = "Failed login attempt";

        if (string.IsNullOrEmpty(attemptedEmail))
        {
            Log.Warning(logTitle);
            return;
        }

        // Obfuscate the email (e.g., "us***@ex***.com")
        var obfuscatedEmail = ObfuscateEmail(attemptedEmail);

        Log.Warning($"{logTitle} for {obfuscatedEmail} (Original: {attemptedEmail})",
            logTitle,
            obfuscatedEmail,
            "[email redacted]");


    }

    private string ObfuscateEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "[redacted]";

        try
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return "****@****";

            var name = parts[0].Length > 2
                ? parts[0][..2] + new string('*', parts[0].Length - 2)
                : "***";

            var domain = parts[1].Length > 2
                ? parts[1][..2] + new string('*', parts[1].Length - 2)
                : "***";

            return $"{name}@{domain}";
        }
        catch
        {
            return "****@****";
        }
    }

    private Response<JwtAuthResult> _CreateErrorResponse()
    {
        return _responseHandler.BadRequest<JwtAuthResult>(
            _Localizer[SharedResourcesKeys.InValidCredentials]
        );
    }

    private async Task<Response<JwtAuthResult>> _HandleSuccessfulLoginAsync(User user)
    {
        var jwtAuthResult = await _authService.GetJwtAuthForuser(user);
        return _responseHandler.Success(jwtAuthResult);
    }

    #endregion

}
