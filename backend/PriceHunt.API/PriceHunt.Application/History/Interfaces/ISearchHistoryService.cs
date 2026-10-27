using PriceHunt.Application.Common;
using PriceHunt.Application.History.Models;

namespace PriceHunt.Application.History.Interfaces;

/// <summary>
/// Answers questions about past searches.
/// </summary>
public interface ISearchHistoryService
{
    /// <summary>
    /// Validates the query and returns the matching page of past outcomes.
    /// </summary>
    /// <param name="query">The filters, sort order and paging.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching outcomes.</returns>
    Task<PagedResult<HistoryItem>> GetAsync(HistoryQuery query, CancellationToken cancellationToken);
}
