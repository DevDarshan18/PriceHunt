using Microsoft.AspNetCore.Mvc;
using PriceHunt.API.Contracts.Requests;
using PriceHunt.API.Mapping;
using PriceHunt.Application.Common;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;

namespace PriceHunt.API.Controllers;

/// <summary>
/// Serves the history of past searches.
/// </summary>
[ApiController]
[Route("api/history")]
public class HistoryController : ControllerBase
{
    private readonly ISearchHistoryService _history;
    private readonly ApiMapper _mapper;

    public HistoryController(ISearchHistoryService history, ApiMapper mapper)
    {
        _history = history;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns one page of supplier outcomes from past searches, filtered and sorted as requested.
    /// </summary>
    /// <param name="request">Filters, sort order and paging.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <returns>The requested page and the total number of matching outcomes.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<PagedResult<HistoryItem>> Get([FromQuery] HistoryRequestDto request, CancellationToken cancellationToken)
    {
        return await _history.GetAsync(_mapper.ToHistoryQuery(request), cancellationToken);
    }
}
