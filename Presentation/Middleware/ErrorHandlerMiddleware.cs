using Application.Models;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;

namespace PresentationLayer.Middleware;

// Global middleware to catch errors

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
            // Intercept the 403 status code and change it to 401 if needed
            if (context.Response.StatusCode == (int)HttpStatusCode.Forbidden)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized; // Change 403 to 401

                var responseModel = new ResponseBuilder<string>()
                    .WithStatusCode(HttpStatusCode.Unauthorized)
                    .WithSuccess(false)
                    .WithMessage("Access denied. You are not authorized.")
                    .WithErrors(new List<string> { "Access denied." })
                    .Build();
                // Use camelCase naming
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                var result = JsonSerializer.Serialize(responseModel, options);

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsync(result);
            }
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";
            var responseModel =
                new ResponseBuilder<string>()
                .WithSuccess(false)
                .WithErrors(new List<string> { error?.Message! })
                .Build();

            Log.Error(error, error!.Message, context.Request, "");
            //TODO:: cover all validation errors
            switch (error)
            {
                case UnauthorizedAccessException:
                    // custom application error
                    responseModel.Message = error.Message;
                    responseModel.StatusCode = HttpStatusCode.Unauthorized;
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case ValidationException validationException:
                    responseModel.StatusCode = HttpStatusCode.UnprocessableEntity;
                    responseModel.Message = "Validation failed";

                    // Split the validation message by tab or newline
                    responseModel.Errors = validationException.Message
                        .Split(new[] { '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(e => e.Trim())
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .ToList();
                    break;

                case KeyNotFoundException:
                    // not found error
                    responseModel.Message = error.Message;
                    responseModel.StatusCode = HttpStatusCode.NotFound;
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case DbUpdateException e:
                    // can't update error
                    responseModel.Message = e.Message;
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case BadRequestException e:
                    responseModel.Message = e.Message;
                    responseModel.StatusCode = HttpStatusCode.BadRequest;
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case Exception e:
                    if (e.GetType().ToString() == "ApiException")
                    {
                        responseModel.Message += e.Message;
                        responseModel.Message += e.InnerException == null ? "" : "\n" + e.InnerException.Message;
                        responseModel.StatusCode = HttpStatusCode.BadRequest;
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    responseModel.Message = e.Message;
                    responseModel.Message += e.InnerException == null ? "" : "\n" + e.InnerException.Message;

                    responseModel.StatusCode = HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;

                default:
                    // unhandled error
                    responseModel.Message = error?.Message;
                    responseModel.StatusCode = HttpStatusCode.InternalServerError;
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;

            }
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var result = JsonSerializer.Serialize(responseModel, options);

            await response.WriteAsync(result);
        }

    }

}


