using Microsoft.AspNetCore.Mvc;
using PriceHunt.API.Contracts.Requests;
using PriceHunt.API.Mapping;
using PriceHunt.Application.Common;
using PriceHunt.Application.Locations.Interfaces;

namespace PriceHunt.API.Controllers;

/// <summary>
/// Serves the places that can be chosen as an origin or destination.
/// </summary>
[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly ILocationService _locations;
    private readonly ApiMapper _mapper;

    public LocationsController(ILocationService locations, ApiMapper mapper)
    {
        _locations = locations;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns one page of place names, optionally narrowed by the text typed so far.
    /// </summary>
    /// <param name="request">Search text and paging.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The requested page and the total number of matching places.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<PagedResult<string>> Get([FromQuery] LocationsRequestDto request, CancellationToken cancellationToken)
    {
        return await _locations.SearchAsync(_mapper.ToLocationQuery(request), cancellationToken);
    }
}
