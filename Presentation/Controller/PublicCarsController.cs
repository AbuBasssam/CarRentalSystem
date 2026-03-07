using Application.Features.Cars;
using Application.Models;
using Domain.AppMetaData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controller;

public class PublicCarsController : ApiController
{

    /// <summary>
    /// Browse available cars (cursor-paginated + filtered).
    /// Only returns active cars on active branches.
    /// </summary>
    /// <response code="200">Paginated car list</response>
    [HttpGet(Router.PublicCarRouter.BASE)]
    [ProducesResponseType(typeof(Response<CursorPaginatedResult<CustomerCarSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] GetPublicCarsQuery query)
        => await CommandExecutor.Execute(
            query, Sender,
            (Response<CursorPaginatedResult<CustomerCarSummaryDto>> r) => NewResult(r));

    /// <summary>Returns customer-facing car details (no VIN, no PlateNumber).</summary>
    /// <response code="200">Car details</response>
    /// <response code="404">Car not found, inactive, or on inactive branch</response>
    [HttpGet(Router.PublicCarRouter.GetById)]
    [ProducesResponseType(typeof(Response<CustomerCarDetailsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CustomerCarDetailsDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int Id)
        => await CommandExecutor.Execute(
            new GetPublicCarByIdQuery(Id), Sender,
            (Response<CustomerCarDetailsDto> r) => NewResult(r));
}