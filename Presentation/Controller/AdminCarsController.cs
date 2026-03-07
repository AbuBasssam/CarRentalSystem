using Application.Features.Cars;
using Application.Features.Cars.Queries;
using Application.Models;
using Domain.AppMetaData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controller;

//[Authorize(Roles = Roles.Admin)]
public class AdminCarsController : ApiController
{

    /// <summary>
    /// Returns cursor-paginated list of all cars (admin view — includes inactive).
    /// </summary>
    /// <response code="200">Paginated car list</response>
    /// <response code="500">Internal server error</response>
    [HttpGet(Router.CarRouter.BASE)]
    [ProducesResponseType(typeof(Response<CursorPaginatedResult<AdminCarSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] AdminCarFilters filters, int? cursor, int pageSize = 10)
    {
        var query = new GetAdminCarsQuery { Filters = filters, Cursor = cursor, PageSize = pageSize };
        return await CommandExecutor.Execute(
                query, Sender,
                (Response<CursorPaginatedResult<AdminCarSummaryDto>> r) => NewResult(r));
    }

    /// <summary>Returns admin car details by ID.</summary>
    /// <response code="200">Car details</response>
    /// <response code="404">Car not found</response>
    [HttpGet(Router.CarRouter.GetById)]
    [ProducesResponseType(typeof(Response<AdminCarDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<AdminCarDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int Id)
        => await CommandExecutor.Execute(
            new GetAdminCarByIdQuery(Id), Sender,
            (Response<AdminCarDetailsDto> r) => NewResult(r));

}