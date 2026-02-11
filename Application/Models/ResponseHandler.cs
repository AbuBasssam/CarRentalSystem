using ApplicationLayer.Resources;
using Interfaces;
using Microsoft.Extensions.Localization;
using System.Net;

namespace Application.Models;
public class ResponseHandler : ITransientService
{
    #region Field(s)

    private readonly IStringLocalizer<SharedResources> _stringLocalizer;

    #endregion

    #region Constructor(s)

    public ResponseHandler(IStringLocalizer<SharedResources> stringLocalizer)
    {
        _stringLocalizer = stringLocalizer;
    }

    #endregion

    #region Success Responses
    public Response<T> Success<T>(T entity, object? meta = null, string? message = null)
    => new ResponseBuilder<T>()
        .WithStatusCode(HttpStatusCode.OK)
        .WithSuccess(true)
        .WithMessage(message ?? _stringLocalizer[SharedResourcesKeys.Success])
        .WithData(entity)
        .WithMeta(meta)
        .Build();

    public Response<T> Created<T>(T entity, object? meta = null)
    => new ResponseBuilder<T>()
        .WithStatusCode(HttpStatusCode.Created)
        .WithSuccess(true)
        .WithMessage(_stringLocalizer[SharedResourcesKeys.Created])
        .WithData(entity)
        .WithMeta(meta)
        .Build();

    public Response<T> Deleted<T>(string? message = null)
    => new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.OK)
            .WithSuccess(true)
            .WithMessage(message ?? _stringLocalizer[SharedResourcesKeys.Deleted])
            .Build();

    #endregion

    #region Error Responses
    public Response<T> Unauthorized<T>(string? message = null, object? meta = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.Unauthorized];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.Unauthorized)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .WithMeta(meta)
            .Build();
    }

    public Response<T> BadRequest<T>(string? message = null, Dictionary<string, string>? validationErrors = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.BadRequest];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .WithValidationErrors(validationErrors)
            .Build();
    }

    public Response<T> Forbidden<T>(string? message = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.Forbidden];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.Forbidden)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .Build();
    }

    public Response<T> NotFound<T>(string? message = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.NotFound];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.NotFound)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .Build();
    }

    public Response<T> Gone<T>(string? message = null, object? meta = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.NotFound];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.Gone)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .WithMeta(meta)
            .Build();
    }


    public Response<T> UnprocessableEntity<T>(string? message = null, Dictionary<string, string>? validationErrors = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.UnprocessableEntity];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.UnprocessableEntity)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .WithValidationErrors(validationErrors)
            .Build();
    }

    /*public Response<T> TooManyRequests<T>(int retryAfterSeconds, string? message = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.TooManyRequests];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.TooManyRequests)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .WithMeta(new { retryAfterSeconds })
            .Build();
    }
    */
    public Response<T> InternalServerError<T>(string? message = null)
    {
        var errorMessages = _ParseErrorMessages(message);
        var defaultMessage = _stringLocalizer[SharedResourcesKeys.InternalServerError];

        return new ResponseBuilder<T>()
            .WithStatusCode(HttpStatusCode.InternalServerError)
            .WithSuccess(false)
            .WithMessage(errorMessages.Any() ? string.Join(", ", errorMessages) : defaultMessage)
            .WithErrors(errorMessages.Any() ? errorMessages : new List<string> { defaultMessage })
            .Build();
    }

    #endregion

    #region Helper Method(s)
    /// <summary>
    /// Parse error messages from various formats
    /// Handles: "msg1\nmsg2", "msg1,msg2", "[msg1]\n[msg2]"
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private List<string> _ParseErrorMessages(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return new List<string>();

        return message.Split(new[] { '\t', '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(msg => msg.Trim())
                     .Where(msg => !string.IsNullOrEmpty(msg))
                     .ToList();
    }

    #endregion

}
