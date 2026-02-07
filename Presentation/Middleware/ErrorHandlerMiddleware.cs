using Application.Models;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;

namespace PresentationLayer.Middleware;

/// <summary>
/// Global Error Handler Middleware
/// Catches all exceptions and returns unified ApiResponse structure
/// </summary>
public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;


    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle 403 Forbidden responses by converting to 401 Unauthorized
            if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                await HandleForbiddenResponse(context);
            }
        }
        catch (Exception error)
        {
            await HandleExceptionAsync(context, error);
        }

    }


    /// <summary>
    /// Main exception handler - creates unified error responses
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception error)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        // Default error response
        var responseModel = new ResponseBuilder<string>()
            .WithSuccess(false)
            .Build();

        // Log the error
        Log.Error(error, "Error occurred: {Message}", error.Message);

        // Map exception types to appropriate responses
        switch (error)
        {
            case UnauthorizedAccessException:
                responseModel.StatusCode = HttpStatusCode.Unauthorized;
                responseModel.Message = error.Message;
                responseModel.Errors = new List<string> { error.Message };
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case ValidationException validationException:
                HandleValidationException(validationException, responseModel, response);
                break;

            case KeyNotFoundException:
                responseModel.StatusCode = HttpStatusCode.NotFound;
                responseModel.Message = error.Message;
                responseModel.Errors = new List<string> { error.Message };
                response.StatusCode = (int)HttpStatusCode.NotFound;
                break;

            case DbUpdateException dbException:
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = "Database operation failed";
                responseModel.Errors = new List<string> { dbException.Message };
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case BadRequestException badRequestException:
                responseModel.StatusCode = HttpStatusCode.BadRequest;
                responseModel.Message = badRequestException.Message;
                responseModel.Errors = new List<string> { badRequestException.Message };
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            default:
                HandleGenericException(error, responseModel, response);
                break;
        }

        await WriteJsonResponse(context, responseModel);
    }


    /// <summary>
    /// Handle 403 responses (convert to 401 for client consistency)
    /// </summary>
    private async Task HandleForbiddenResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        var responseModel = new ResponseBuilder<string>()
            .WithStatusCode(HttpStatusCode.Unauthorized)
            .WithSuccess(false)
            .WithMessage("Access denied. You are not authorized.")
            .WithErrors(new List<string> { "Access denied." })
            .Build();

        await WriteJsonResponse(context, responseModel);
    }

    /// <summary>
    /// Handle FluentValidation exceptions with field-specific errors
    /// </summary>
    private void HandleValidationException(
        ValidationException validationException,
        Response<string> responseModel,
        HttpResponse response)
    {
        responseModel.StatusCode = HttpStatusCode.UnprocessableEntity;
        responseModel.Message = "Validation failed";
        response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

        // Extract field-specific validation errors if available
        if (validationException.Data.Contains("ValidationErrors") &&
            validationException.Data["ValidationErrors"] is Dictionary<string, string> validationErrors)
        {
            responseModel.ValidationErrors = validationErrors;

            // Also add to errors array for backward compatibility
            responseModel.Errors = validationErrors.Values.ToList();
        }
        else
        {
            // Fallback: parse from message
            responseModel.Errors = validationException.Message
                .Split(new[] { '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().Trim('[', ']'))
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToList();
        }
    }


    /// <summary>
    /// Handle generic exceptions
    /// </summary>
    private void HandleGenericException(
        Exception error,
        Response<string> responseModel,
        HttpResponse response)
    {
        responseModel.StatusCode = HttpStatusCode.InternalServerError;
        response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // Build error message with inner exception if present
        var errorMessage = error.Message;
        if (error.InnerException != null)
        {
            errorMessage += $" | Inner: {error.InnerException.Message}";
        }

        responseModel.Message = errorMessage;
        responseModel.Errors = new List<string> { errorMessage };
    }

    /// <summary>
    /// Write JSON response with camelCase naming
    /// </summary>
    private async Task WriteJsonResponse<T>(HttpContext context, Response<T> responseModel)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var result = JsonSerializer.Serialize(responseModel, options);
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
}


