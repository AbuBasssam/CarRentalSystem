using Application.Features.Branches;
using Application.Models;
using Domain.AppMetaData;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controller;

//[Authorize(Roles = Roles.Admin)]
public class BranchesController : ApiController
{

    /// <summary>
    /// Returns paginated list of all branches
    /// </summary>
    /// <response code="200">Paginated branch list</response>
    [HttpGet(Router.BranchRouter.GetAll)]
    [ProducesResponseType(typeof(Response<PaginatedResult<BranchSummaryDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetBranchesQuery query)
        => await CommandExecutor.Execute(query, Sender, (Response<PaginatedResult<BranchSummaryDTO>> r) => NewResult(r));

}