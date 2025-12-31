using Application.Features.AuthFeature;
using Application.Models;
using Domain.AppMetaData;
using Domain.HelperClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controller;
public class AuthController : ApiController
{
    [HttpPost(Router.AuthenticationRouter.SignIn)]
    [ProducesResponseType(typeof(JwtAuthResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    public async Task<IActionResult> SignIn([FromBody] SignInCommand command)
    {

        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<JwtAuthResult> response) => NewResult(response)
        );
    }


    [HttpPost(Router.AuthenticationRouter.SignUp)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [AllowAnonymous]
    public async Task<IActionResult> SignUp([FromBody] SignUpCommandDTO dto)
    {

        return await CommandExecutor.Execute(
            new SignUpCommand(dto),
            Sender,
            (Response<string> response) => NewResult(response)
        );
    }

    [HttpPost(Router.AuthenticationRouter.EmailConfirmation)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Authorize(Policy = Policies.VerificationOnly)]
    public async Task<IActionResult> VerifyEmailConfirmationOtp([FromBody] ConfirmEmailDTO dto)
    {


        return await CommandExecutor.Execute(
                new ConfirmEmailCommand(dto),
                Sender,
                (Response<bool> response) => NewResult(response)
        );
    }


    [HttpPost(Router.AuthenticationRouter.Token)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ValidateRefreshToken([FromRoute] string token)
    {
        return await CommandExecutor.Execute(new AuthorizeUserQuery { AccessToken = token }, Sender, (Response<string> response) => NewResult(response)
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = Policies.VerificationOnly)]

    public async Task<IActionResult> ResendVerificationCode(ResendVerificationCodeCommand command)
    {

        return await CommandExecutor.Execute(command, Sender, (Response<string> response) => NewResult(response)
        );
    }
}