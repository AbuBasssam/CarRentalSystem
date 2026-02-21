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

    /// <summary>
    /// Updates an existing branch
    /// </summary>
    /// <response code="200">Branch updated</response>
    /// <response code="404">Branch not found</response>
    /// <response code="422">Validation error</response>
    /// <response code="500">Internal Server error</response>

    [HttpPut(Router.BranchRouter.Update)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Put([FromRoute] int Id, [FromBody] UpdateBranchDTO dto)
        => await CommandExecutor.Execute(
            new UpdateBranchCommand(Id, dto),
            Sender,
            (Response<bool> r) => NewResult(r)
        );

    /// <summary>
    /// Deletes a branch — fails if branch has cars assigned
    /// </summary>
    /// <response code="200">Branch deleted</response>
    /// <response code="400">Branch has cars — cannot delete</response>
    /// <response code="404">Branch not found</response>
    /// <response code="500">Internal Server error</response>

    [HttpDelete(Router.BranchRouter.Delete)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Delete([FromRoute] int Id)
        => await CommandExecutor.Execute(new DeleteBranchCommand(Id), Sender, (Response<bool> r) => NewResult(r));

    /// <summary>
    /// Change branch active status (Active ↔ Inactive)
    /// </summary>
    /// <response code="200">Returns new status (true = Active)</response>
    /// <response code="404">Branch not found</response>
    /// <response code="500">Internal Server error</response>

    [HttpPatch(Router.BranchRouter.Toggle)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status500InternalServerError)]

    public async Task<IActionResult> Toggle([FromRoute] int Id, [FromBody] ToggleStatusRequest activeStatus)
      => await CommandExecutor.Execute(new ToggleBranchStatusCommand(Id, activeStatus.ActiveStatus), Sender, (Response<bool> r) => NewResult(r));
}