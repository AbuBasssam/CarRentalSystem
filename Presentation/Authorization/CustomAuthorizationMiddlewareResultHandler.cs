using Application.Models;
using Domain.AppMetaData;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace Presentation.Authorization;

/// <summary>
/// ENHANCED: Custom Authorization Middleware Result Handler
/// 
/// NEW FEATURES:
/// 1. Returns errorCode and isRecoverable in meta for Frontend
/// 2. Distinguishes between different failure types
/// 3. Helps Frontend decide whether to attempt token refresh
/// 
/// ERROR CODE FLOW:
/// - MissingToken → isRecoverable = false → Frontend won't refresh
/// - InvalidToken → isRecoverable = false → Frontend won't refresh  
/// - TokenExpired → isRecoverable = true → Frontend attempts refresh
/// </summary>
public class CustomAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        // ============================================
        // SUCCESS: Continue to next middleware
        // ============================================
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        // ============================================
        // CHALLENGED: User is not authenticated (401)
        // ============================================
        if (authorizeResult.Challenged)
        {
            // Extract error metadata from authorization context
            var errorMetadata = ExtractErrorMetadata(context);

            await WriteUnifiedResponse(
                context,
                HttpStatusCode.Unauthorized,
                "Authentication required",
                new List<string> { "You must be authenticated to access this resource" },
                errorMetadata
            );
            return;
        }

        // ============================================
        // FORBIDDEN: User lacks permissions (403)
        // ============================================
        if (authorizeResult.Forbidden)
        {
            var errorMessages = ExtractErrorMessages(authorizeResult);

            await WriteUnifiedResponse(
                context,
                HttpStatusCode.Forbidden,
                errorMessages.FirstOrDefault() ?? "Access denied",
                errorMessages,
                new { ErrorCode = enErrorCode.AccessDenied, IsRecoverable = false }
            );
            return;
        }

        // ============================================
        // FALLBACK: Use default handler
        // ============================================
        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>
    /// Extracts error messages from authorization failure reasons
    /// </summary>
    private static List<string> ExtractErrorMessages(PolicyAuthorizationResult authorizeResult)
    {
        var errors = new List<string>();

        if (authorizeResult.AuthorizationFailure != null)
        {
            foreach (var failureReason in authorizeResult.AuthorizationFailure.FailureReasons)
            {
                errors.Add(failureReason.Message);
            }
        }

        // If no specific errors, add a default message
        if (errors.Count == 0)
        {
            errors.Add("You do not have permission to access this resource");
        }

        return errors;
    }

    /// <summary>
    /// Extracts error metadata (errorCode, isRecoverable) from HttpContext
    /// This metadata is set by ValidTokenHandler
    /// </summary>
    private static object ExtractErrorMetadata(HttpContext context)
    {
        // Try to get metadata from authorization context
        var authContext = context.Features.Get<IAuthorizationMiddlewareResultHandler>();

        // Check if there's error metadata in HttpContext.Items
        if (context.Items.TryGetValue(Keys.Auth_Error_Metadata_Key, out var metadata))
        {
            return metadata ?? new { ErrorCode = enErrorCode.InvalidToken.ToString(), IsRecoverable = false };
        }

        // Default metadata for generic authentication failure
        return new { ErrorCode = enErrorCode.InvalidToken.ToString(), IsRecoverable = false };
    }

    /// <summary>
    /// Writes unified Response format directly to HTTP response
    /// ENHANCED: Now includes meta field with errorCode and isRecoverable
    /// </summary>
    private static async Task WriteUnifiedResponse(
        HttpContext context,
        HttpStatusCode statusCode,
        string message,
        List<string> errors,
        object? meta = null)
    {
        // Create unified response
        var response = new Response<string>
        {
            Succeeded = false,
            StatusCode = statusCode,
            Message = message,
            Errors = errors,
            Data = null!,
            Meta = meta, // Contains errorCode and isRecoverable
            ValidationErrors = null
        };

        // Set response status and headers
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        // Serialize with proper options
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        // Write response and complete it
        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);

        // CRITICAL: Complete the response to prevent further middleware processing
        await context.Response.CompleteAsync();
    }
}