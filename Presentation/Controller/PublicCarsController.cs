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


    /// <summary>
    /// Serves the primary image as a binary stream.
    /// Validation gate: car active, branch active, image not deleted.
    /// </summary>
    /// <response code="200">Image binary stream (image/webp)</response>
    /// <response code="404">Validation gate failed</response>

    [HttpGet(Router.PublicCarRouter.GetPrimaryImage)]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrimaryImage([FromRoute] int Id)
    {
        var result = await Sender.Send(new GetCarPrimaryImageQuery(Id));

        if (!result.Succeeded)
            return NewResult(result);

        return File(result.Data.Content, result.Data.ContentType);
    }


    /// <summary>
    /// Returns image metadata (IDs + serving URLs) for a car.
    /// Gate: car must be active and branch must be active → else 404.
    /// </summary>
    /// <response code="200">List of image metadata</response>
    /// <response code="404">Car not found or inactive</response>

    [HttpGet(Router.PublicCarRouter.GetImages)]
    [ProducesResponseType(typeof(Response<List<CarImageMetadataDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<List<CarImageMetadataDto>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImages([FromRoute] int Id)
        => await CommandExecutor.Execute(
            new GetCarImagesQuery(Id), Sender,
            (Response<List<CarImageMetadataDto>> r) => NewResult(r));


    /// <summary>
    /// Serves a specific image as binary stream.
    /// Validation gate: car active, branch active, image not deleted → else 404.
    /// </summary>
    /// <response code="200">Image binary stream (image/webp)</response>
    /// <response code="404">Validation gate failed</response>

    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage([FromRoute] int Id, [FromRoute] int ImageId)
    {
        var result = await Sender.Send(new GetCarImageQuery(Id, ImageId, IsAdminRequest: false));

        if (!result.Succeeded)
            return NewResult(result);

        return File(result.Data.Content, result.Data.ContentType);
    }
}