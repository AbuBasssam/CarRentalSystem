using Application.Features.AuthFeature;
using Application.Models;
using Domain.AppMetaData;
using Domain.HelperClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Filters;
using Presentation.Helpers;

namespace Presentation.Controller;
public class AuthController : ApiController
{
    /// <summary>
    /// Authenticates user with email and password
    /// </summary>
    /// <remarks>
    /// Rate Limit: 5 requests per 15 Minutes/user 
    /// </remarks>
    /// <param name="command">Sign in credentials</param>
    /// <returns>JWT authentication result with access and refresh tokens</returns>
    /// <response code="200">Successfully authenticated</response>
    /// <response code="400">Invalid credentials or account locked</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many login attempts</response>

    [HttpPost(Router.AuthenticationRouter.SignIn)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]
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
    /// <remarks>
    /// Rate Limit: 2 requests per hour/user 
    /// </remarks>
    /// <param name="dto">User registration data</param>
    /// <response code="201">User successfully created</response>
    /// <response code="400">Email already exists or invalid data</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many registration attempts</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.SignUp)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection


    public async Task<IActionResult> SignUp([FromBody] SignUpDTO dto)
    {

        return await CommandExecutor.Execute(
            new SignUpCommand(dto),
            Sender,
            (Response<bool> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Confirms user email with verification code
    /// </summary>
    /// <remarks>
    /// Rate Limit: 5 requests per 3 minutes/user 
    /// </remarks>
    /// <param name="dto">Email confirmation data containing OTP code</param>
    /// <returns>Confirmation result</returns>
    /// <response code="200">Email successfully confirmed</response>
    /// <response code="400">Invalid or expired code</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many verification attempts</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.EmailConfirmation)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status410Gone)]

    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection

    public async Task<IActionResult> ConfirmEmail([FromBody] VerificationDTO dto)
    {


        return await CommandExecutor.Execute(
                new ConfirmEmailCommand(dto),
                Sender,
                (Response<bool> response) => NewResult(response)
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
    /// Rate Limit: 5 requests per 24 hours/user 
    /// </remarks>
    /// <response code="200">Verification code sent successfully</response>
    /// <response code="400">Bad request - user already verified or cooldown active</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="410">Too many verification attempts </response>
    /// <response code="429">Too many requests - rate limit exceeded</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.ResendVerification)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<string>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection
    public async Task<IActionResult> ResendVerificationCode([FromBody] ResendCodeDTO dto)
    {
        ResendVerificationCodeCommand command = new ResendVerificationCodeCommand(dto);
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<bool> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Initiates password reset process by sending verification code
    /// </summary>
    /// <param name="dto">Email address for password reset</param>
    /// <remarks>
    /// Starts the password reset flow:
    /// 1. Validates user exists and email is verified
    /// 2. Generates secure verification code
    /// 3. Sends code to user's email
    /// Security features:
    /// - Rate limiting 5 requests per 15 minutes/user
    /// - Short-lived verification codes
    /// </remarks>
    /// <returns>Session token for verification step</returns>
    /// <response code="200">Reset code sent successfully</response>
    /// <response code="400">User not found or email not verified</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many reset attempts</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.PasswordReset)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection

    public async Task<IActionResult> SendResetCode([FromBody] SendResetCodeDTO dto)
    {
        SendResetCodeCommand command = new SendResetCodeCommand(dto);
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<bool> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Verifies password reset code and returns token for password change
    /// </summary>
    /// <param name="dto">Session token and verification code</param>
    /// <remarks>
    /// Validates the verification code sent to user's email.
    /// On success, returns a token that can be used to change password.
    /// 
    /// Flow:
    /// 1. Verify OTP code matches
    /// 2. Check code hasn't expired
    /// 3. Return authorization token for ResetPassword step
    /// Security features:
    /// - Rate limiting 3 requests per 5 minutes/user
    /// </remarks>
    /// <returns>Authorization token for password reset</returns>
    /// <response code="200">Code verified successfully</response>
    /// <response code="400">Invalid or expired code</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="410">Too many verification attempts </response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many Verification attempts</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.PasswordResetVerification)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status410Gone)]
    [ProducesResponseType(typeof(Response<VerificationFlowResponse>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection

    public async Task<IActionResult> VerifyResetCode([FromBody] VerificationDTO dto)
    {
        VerifyResetCodeCommand command = new VerifyResetCodeCommand(dto);
        return await CommandExecutor.Execute(
            command,
            Sender,
            (Response<VerificationFlowResponse> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Completes password reset with new password
    /// </summary>
    /// <param name="command">Reset token and new password</param>
    /// <remarks>
    /// Final step in password reset flow.
    /// 
    /// Requirements:
    /// - Valid reset token (from VerifyResetCode step)
    /// - New password must meet strength requirements
    /// - Password must differ from previous password
    /// 
    /// On success:
    /// - Password is updated
    /// - All existing tokens are revoked
    /// - User must sign in with new password
    /// Security features:
    /// - Rate limiting 5 requests per 5 minutes/user
    /// </remarks>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid token or weak password</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="500">Internal server error</response>

    [HttpPut(Router.AuthenticationRouter.PasswordReset)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [Authorize(Policy = Policies.ResetPassword)]

    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        return await CommandExecutor.Execute(
          command,
          Sender,
          (Response<bool> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Resends password reset verification code to user's email
    /// </summary>
    /// <param name="dto">Email address or user identifier</param>
    /// <remarks>
    /// Generates and sends a new password reset verification code.
    /// Use cases:
    /// - User didn't receive original reset code
    /// - Reset code expired
    /// - User accidentally deleted reset email
    /// Security features:
    /// - Rate limiting 5 requests per 24 hours/user
    /// - Previous codes are invalidated
    /// - Code expires after configured duration 
    /// </remarks>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Reset code sent successfully</response>
    /// <response code="400">Invalid email or user not found</response>
    /// <response code="403">Invalid CSRF token</response>
    /// <response code="422">Validation error</response>
    /// <response code="429">Too many reset attempts - rate limit exceeded</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.AuthenticationRouter.ResendPasswordReset)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ValidateCsrfToken]  // CSRF Protection

    public async Task<IActionResult> ResendResetCode([FromBody] ResendCodeDTO dto)
    {
        var command = new ResendResetCodeCommand(dto);
        return await CommandExecutor.Execute(
         command,
         Sender,
         (Response<bool> response) => NewResult(response)
       );
    }


    /// <summary>
    /// Logs out the current user by revoking their authentication tokens
    /// </summary>
    /// <remarks>
    /// Invalidates the current access and refresh tokens.
    /// User must re-authenticate to obtain new tokens.
    /// 
    /// Security measures:
    /// - Requires valid authentication token
    /// - Validates token ownership
    /// - Revokes tokens immediately
    /// - Logs logout activity with IP and user agent
    /// </remarks>
    /// <returns>Success confirmation</returns>
    /// <response code="200">Successfully logged out</response>
    /// <response code="401">Invalid or missing authentication token</response>
    /// <response code="400">Logout operation failed</response>

    [HttpPost(Router.AuthenticationRouter.Logout)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [Authorize(Policy = Policies.Logout)]
    public async Task<IActionResult> Logout()
    {
        var command = new LogoutCommand();
        return await CommandExecutor.Execute(
          command,
          Sender,
          (Response<bool> response) => NewResult(response)
        );
    }


    /// <summary>
    /// Refreshes authentication tokens using a valid refresh token
    /// </summary>
    /// <remarks>
    /// Exchanges an expired access token and valid refresh token for new tokens.
    /// 
    /// Security features:
    /// - One-time use: Each refresh token can only be used once
    /// - Token pairing: Access and refresh tokens must match (JTI validation)
    /// - Reuse detection: Attempting to reuse a token revokes all user sessions
    /// - Account validation: Checks email verification and account lock status
    /// - Rate limiting 5 requests per 30 minutes/user
    /// Token reuse security:
    /// If a refresh token is reused (indicating possible token theft),
    /// ALL active tokens for the user are immediately revoked for security.
    /// </remarks>
    /// <returns>New JWT authentication result with fresh access and refresh tokens</returns>
    /// <response code="200">Successfully refreshed tokens</response>
    /// <response code="401">Invalid, expired, revoked, or reused token detected</response>
    /// <response code="400">User account locked or email not verified</response>
    /// <response code="422">Validation error - invalid token format</response>
    /// <response code="403">Invalid CSRF token</response>

    [HttpPost(Router.AuthenticationRouter.RefreshToken)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<JwtAuthResult>), StatusCodes.Status422UnprocessableEntity)]
    [ValidateCsrfToken]
    public async Task<IActionResult> RefreshToken()
    {
        return await CommandExecutor.Execute(
            new RefreshTokenCommand(),
            Sender,
            (Response<JwtAuthResult> response) => NewResult(response)
        );
    }

    /// <summary>
    /// Generates and returns CSRF token for form submissions
    /// </summary>
    /// <returns>CSRF token for API requests</returns>
    /// <response code="200">CSRF token generated successfully</response>
    /// <response code="429">Too many reset attempts - rate limit exceeded</response>

    [HttpGet(Router.AuthenticationRouter.CSRF_Token)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]

    public IActionResult GetCsrfToken()
    {
        return Ok(new { message = "CSRF token set in cookie" });
    }


    /// <summary>
    /// Validates current authentication token and returns user data
    /// </summary>
    /// <remarks>
    /// This endpoint is used for:
    /// - Initial app load verification
    /// - Checking if user session is still valid
    /// - Getting current user data (FirstName, LastName, ImagePath)
    /// 
    /// Note: Email, UserId, and Roles are already in JWT claims - 
    /// frontend can extract them without calling this endpoint.
    /// 
    /// Authentication required via access token in httpOnly cookie.
    /// 
    /// Rate Limit: 20 requests per 5 minutes/user
    /// (Higher limit since called on every app load)
    /// 
    /// Returns minimal user data if token is valid, otherwise returns 401.
    /// </remarks>
    /// <returns>User information not available in JWT claims</returns>
    /// <response code="200">Token valid - returns user data</response>
    /// <response code="401">Token invalid, expired, or missing</response>
    /// <response code="429">Too many validation requests</response>
    /// <response code="500">Internal server error</response>

    [HttpGet(Router.AuthenticationRouter.TokenValidation)]
    [ProducesResponseType(typeof(Response<TokenValidationResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<TokenValidationResponseDTO>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(Response<TokenValidationResponseDTO>), StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = Policies.ValidToken)]
    public async Task<IActionResult> ValidateToken()
    {
        var query = new ValidateTokenQuery();
        return await CommandExecutor.Execute(
            query,
            Sender,
            (Response<TokenValidationResponseDTO> response) => NewResult(response)
        );
    }


}