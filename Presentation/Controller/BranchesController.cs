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
    [HttpGet(Router.BranchRouter.BASE)]
    [ProducesResponseType(typeof(Response<PaginatedResult<BranchSummaryDTO>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get([FromQuery] GetBranchesQuery query)
        => await CommandExecutor.Execute(query, Sender, (Response<PaginatedResult<BranchSummaryDTO>> r) => NewResult(r));

    /// <summary>
    /// Returns branch details by ID
    /// </summary>
    /// <response code="200">Branch details</response>
    /// <response code="404">Branch not found</response>
    [HttpGet(Router.BranchRouter.GetById)]
    [ProducesResponseType(typeof(Response<BranchDetailsDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<BranchDetailsDTO>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int Id)
        => await CommandExecutor.Execute(
            new GetBranchByIdQuery(Id),
            Sender,
            (Response<BranchDetailsDTO> r) => NewResult(r)
        );

    /// <summary>
    /// Creates a new branch
    /// </summary>
    /// <response code="201">Branch created — returns new branch ID</response>
    /// <response code="422">Validation error</response>
    /// <response code="500">Internal Server error</response>
    [HttpPost(Router.BranchRouter.Create)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] CreateBranchDTO dto)
        => await CommandExecutor.Execute(
            new CreateBranchCommand(dto),
            Sender,
            (Response<int> r) => NewResult(r)
        );

}