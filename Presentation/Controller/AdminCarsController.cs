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


    /// <summary>Creates a new car.</summary>
    /// <response code="201">Car created — returns new car ID</response>
    /// <response code="400">Branch/Category not found or invalid</response>
    /// <response code="422">Validation or uniqueness error</response>
    /// <response code="500">Internal server error</response>

    [HttpPost(Router.CarRouter.Create)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(Response<int>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Post([FromBody] CreateCarDto dto)
        => await CommandExecutor.Execute(
            new CreateCarCommand(dto), Sender,
            (Response<int> r) => NewResult(r));


    /// <summary>Updates IsActive and/or FleetConditionStatus.</summary>
    /// <response code="200">Status updated</response>
    /// <response code="404">Car not found</response>
    /// <response code="422">Validation error</response>

    [HttpPatch(Router.CarRouter.UpdateStatus)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus([FromRoute] int Id, [FromBody] UpdateCarStatusDto dto)
        => await CommandExecutor.Execute(
            new UpdateCarStatusCommand(Id, dto), Sender,
            (Response<bool> r) => NewResult(r));


    /// <summary>Transfers car to another branch + logs CarBranchHistory.</summary>
    /// <response code="200">Transfer completed</response>
    /// <response code="400">Target branch not found, inactive, or same as current</response>
    /// <response code="404">Car not found</response>

    [HttpPost(Router.CarRouter.Transfer)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Transfer([FromRoute] int Id, [FromBody] TransferCarDto dto)
        => await CommandExecutor.Execute(
            new TransferCarCommand(Id, dto), Sender,
            (Response<bool> r) => NewResult(r));


    // ── Image Management ──────────────────────────────────────────────────────

    /// <summary>
    /// Uploads one or more images for a car.
    /// Validates MIME type, file size (5 MB max), and magic bytes.
    /// Converts all uploads to WebP. First upload becomes primary if none exists.
    /// </summary>
    /// <response code="201">Images uploaded — returns list of new image IDs</response>
    /// <response code="400">Invalid files (wrong type, size, or magic bytes)</response>
    /// <response code="404">Car not found</response>

    [HttpPost(Router.CarRouter.UploadImages)]
    [ProducesResponseType(typeof(Response<List<int>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<List<int>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<List<int>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImages([FromRoute] int Id, [FromForm] IFormFileCollection files)
        => await CommandExecutor.Execute(
            new UploadCarImagesCommand(Id, files.ToList()), Sender,
            (Response<List<int>> r) => NewResult(r));


    /// <summary>
    /// Soft-deletes an image.
    /// If target is primary and other images exist, promotes next image to primary.
    /// Physical file is preserved for the 7-day recovery window (Windows Service cleanup).
    /// </summary>
    /// <response code="200">Image soft-deleted</response>
    /// <response code="400">Cannot delete the only image</response>
    /// <response code="404">Image not found</response>

    [HttpDelete(Router.CarRouter.DeleteImage)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage([FromRoute] int Id, [FromRoute] int ImageId)
        => await CommandExecutor.Execute(
            new DeleteCarImageCommand(Id, ImageId), Sender,
            (Response<bool> r) => NewResult(r));


    /// <summary>Sets a specific image as the primary image for the car.</summary>
    /// <response code="200">Primary image updated</response>
    /// <response code="404">Image not found</response>

    [HttpPatch(Router.CarRouter.SetPrimaryImage)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage([FromRoute] int Id, [FromRoute] int ImageId)
        => await CommandExecutor.Execute(
            new SetPrimaryCarImageCommand(Id, ImageId), Sender,
            (Response<bool> r) => NewResult(r));


}