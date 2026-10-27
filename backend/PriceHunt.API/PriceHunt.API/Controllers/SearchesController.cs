using Microsoft.AspNetCore.Mvc;
using PriceHunt.API.Contracts.Requests;
using PriceHunt.API.Mapping;
using PriceHunt.API.Streaming;
using PriceHunt.Application.Searches.Interfaces;

namespace PriceHunt.API.Controllers;

/// <summary>
/// Starts a search and streams the supplier answers back as they arrive.
/// </summary>
[ApiController]
[Route("api/searches")]
public class SearchesController : ControllerBase
{
    private readonly IShippingSearchService _service;
    private readonly ApiMapper _mapper;

    public SearchesController(IShippingSearchService service, ApiMapper mapper)
    {
        _service = service;
        _mapper = mapper;
    }

    /// <summary>
    /// Searches all selected suppliers and streams each answer back as it arrives.
    /// </summary>
    /// <param name="request">Origin, destination, ship date and the suppliers to ask.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>A response that ends with a finished event.</returns>
    [HttpPost]
    [Produces("text/event-stream")]
    public async Task<IActionResult> Search(SearchRequestDto request, CancellationToken cancellationToken)
    {
        var execution = await _service.BeginAsync(_mapper.ToSearchRequest(request), cancellationToken);
        return new SearchEventStreamResult(execution, _mapper);
    }
}
