using Application.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Presentation.Controller;
/// <summary>
/// Represents the base API controller.
/// </summary>
public abstract class ApiController : ControllerBase
{
    private ISender? _sender;

    /// <summary>
    /// Gets the sender.
    /// </summary>
    protected ISender Sender => _sender ??= HttpContext.RequestServices.GetRequiredService<ISender>();


    public ObjectResult NewResult<T>(Response<T> response)
    {
        return response.StatusCode switch
        {
            System.Net.HttpStatusCode.OK => new OkObjectResult(response),
            System.Net.HttpStatusCode.Created => new CreatedResult(string.Empty, response),
            System.Net.HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            System.Net.HttpStatusCode.BadRequest => new BadRequestObjectResult(response),
            System.Net.HttpStatusCode.NotFound => new NotFoundObjectResult(response),
            System.Net.HttpStatusCode.Accepted => new AcceptedResult(string.Empty, response),
            System.Net.HttpStatusCode.UnprocessableEntity => new UnprocessableEntityObjectResult(response),
            System.Net.HttpStatusCode.Gone => StatusCode(StatusCodes.Status410Gone, response),
            _ => new BadRequestObjectResult(response),
        };
    }
}
