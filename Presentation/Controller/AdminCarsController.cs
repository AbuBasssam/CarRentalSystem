using Application.Features.Cars;
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
    public async Task<IActionResult> Get([FromQuery] AdminCarFilters filters)
    {
        var query = new GetAdminCarsQuery { Filters = filters };
        return await CommandExecutor.Execute(
                query, Sender,
                (Response<CursorPaginatedResult<AdminCarSummaryDto>> r) => NewResult(r));
    }
}