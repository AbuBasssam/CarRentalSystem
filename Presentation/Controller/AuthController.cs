using Application.Features.AuthFeature;
using Application.Models;
using Domain.AppMetaData;
using Domain.Enums;
using Domain.HelperClasses;
using Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;
using Presentation.Helpers;

namespace Presentation.Controller;
public class AuthController : ApiController
{
    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    /// <param name="command">Sign in credentials</param>
    /// <returns>JWT authentication result with access and refresh tokens</returns>
    /// <response code="200">Successfully authenticated</response>
    /// <response code="400">Invalid credentials or account locked</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many login attempts</response>
    [HttpPost(Router.AuthenticationRouter.SignIn)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status429TooManyRequests)]
    //[ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status401Unauthorized)]//Locked account
    public async Task<IActionResult> SignIn([FromBody] SignInCommand command)
    {

        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<JwtAuthResult> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Registers a new user account
    /// </summary>
    /// <param name="dto">User registration data</param>
    /// <returns>Success message with verification instructions</returns>
    /// <response code="201">User successfully created</response>
    /// <response code="400">Email already exists or invalid data</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many registration attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost(Router.AuthenticationRouter.SignUp)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignUp([FromBody] SignUpCommandDTO dto)
    {

        return await CommandExecutor.Execute(
            new SignUpCommand(dto),
            Sender,
            (Response<string> response) => NewResult(response)
        );
    }

    /// <summary>
    /// Confirms user email with verification code
    /// </summary>
    /// <param name="dto">Email confirmation data containing OTP code</param>
    /// <returns>Confirmation result</returns>
    /// <response code="200">Email successfully confirmed</response>
    /// <response code="400">Invalid or expired code</response>
    /// <response code="401">Invalid or expired verification token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many verification attempts</response>
    /// <response code="500">Internal server error</response>
    [HttpPost(Router.AuthenticationRouter.EmailConfirmation)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = Policies.VerificationOnly)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDTO dto)
    {


        return await CommandExecutor.Execute(
                new ConfirmEmailCommand(dto),
                Sender,
                (Response<bool> response) => NewResult(response)
        );
    }

    /// <summary>
    /// Validates and refreshes access token
    /// </summary>
    /// <param name="token">Refresh token</param>
    /// <returns>New access token</returns>
    /// <response code="200">Token successfully refreshed</response>
    /// <response code="400">Invalid refresh token</response>
    /// <response code="401">Token expired or revoked</response>
    /// <response code="429">Too many token refresh requests</response>
    [HttpPost(Router.AuthenticationRouter.Token)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ValidateRefreshToken([FromRoute] string token)
    {
        return await CommandExecutor.Execute(
            new AuthorizeUserQuery { AccessToken = token },
            Sender,
            (Response<string> response) => NewResult(response)
        );

    }


    /// <summary>
    /// Resends email verification code to the user
    /// </summary>
    /// <remarks>
    /// This endpoint allows users to request a new verification code if:
    /// - The previous code has expired
    /// - They didn't receive the email
    /// - The cooldown period (2 minutes) has passed
    /// 
    /// Rate Limit: 10 requests per hour
    /// </remarks>
    /// <response code="200">Verification code sent successfully</response>
    /// <response code="400">Bad request - user already verified or cooldown active</response>
    /// <response code="401">Unauthorized - invalid or expired verification token</response>
    /// <response code="404">User not found</response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <response code="500">Internal server error</response>
    [HttpPost(Router.AuthenticationRouter.ResendVerification)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status500InternalServerError)]

    [Authorize(Policy = Policies.VerificationOnly)]
    [OtpCooldown(enOtpType.ConfirmEmail)]
    public async Task<IActionResult> ResendVerificationCode()
    {
        ResendVerificationCodeCommand command = new ResendVerificationCodeCommand();
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<string> response) => NewResult(response)
        );
    }
    /// <summary>
    /// Send password reset code to email
    /// </summary>
    [HttpPost(Router.AuthenticationRouter.PasswordReset)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [AllowAnonymous]

    public async Task<IActionResult> SendResetCode([FromBody] SendResetCodeDTO dto)
    {
        SendResetCodeCommand command = new SendResetCodeCommand(dto);
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<VerificationFlowResponse> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Step 2: Verify the reset code
    /// </summary>
    [HttpPost(Router.AuthenticationRouter.PasswordResetVerification)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Authorize(Policy = Policies.AwaitVerification)]

    public async Task<IActionResult> VerifyResetCode([FromBody] VerifyResetCodeDTO dto)
    {
        VerifyResetCodeCommand command = new VerifyResetCodeCommand(dto);
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<VerificationFlowResponse> response) => NewResult(response)
        );
    }
    /// <summary>
    /// Step 3: Reset password with new password
    /// </summary>
    /// <remarks>
    /// Requires valid reset token from Step 2 (Verified stage).
    /// Updates user's password and invalidates all existing sessions.
    /// </remarks>
    [HttpPut(Router.AuthenticationRouter.PasswordReset)]
    [Authorize(Policy = Policies.ResetPasswordVerified)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        return await CommandExecutor.Execute(
          command,
          Sender,
          (Response<bool> response) => NewResult(response)
        );
    }

    /// <summary>
    /// Resend password reset code (Token-based for security)
    /// </summary>
    /// <remarks>
    /// Requires valid reset token from previous send/resend.
    /// 
    /// Security benefits:
    /// - Token rotation (old token invalidated, new token issued)
    /// - Cannot resend to different email (email from token only)
    /// - Rate limiting on user level (not just IP)
    /// - Cooldown period enforced (60 seconds)
    /// 
    /// Similar to RefreshToken pattern:
    /// - Must use valid token from previous step
    /// - Old token becomes invalid after resend
    /// - New token must be used for verification
    /// </remarks>
    [HttpPost(Router.AuthenticationRouter.ResendPasswordReset)]
    [Authorize(Policy = Policies.AwaitVerification)]
    [OtpCooldown(enOtpType.ResetPassword)]
    public async Task<IActionResult> ResendResetCode()
    {
        var command = new ResendResetCodeCommand();
        return await CommandExecutor.Execute(
         command,
         Sender,
         (Response<VerificationFlowResponse> response) => NewResult(response)
       );
    }
}