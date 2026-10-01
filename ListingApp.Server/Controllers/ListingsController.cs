using ListingApp.Server.Models;
using ListingApp.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ListingApp.Server.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController(
    ListingRepository repository,
    ListingSearchService searchService) : ControllerBase
{
    [HttpGet("search")]
    [ProducesResponseType<ListingSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public ActionResult<ListingSearchResponse> Search([FromQuery] ListingSearchQuery query)
    {
        try
        {
            return Ok(searchService.Search(repository.GetAll(), query));
        }
        catch (ListingSearchValidationException exception)
        {
            var details = new ValidationProblemDetails(exception.Errors.ToDictionary(
                error => error.Key,
                error => error.Value))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more search parameters are invalid."
            };

            return BadRequest(details);
        }
    }
}
