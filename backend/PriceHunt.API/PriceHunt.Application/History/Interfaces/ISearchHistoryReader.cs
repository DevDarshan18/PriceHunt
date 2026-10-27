using PriceHunt.Application.Common;
using PriceHunt.Application.History.Models;

namespace PriceHunt.Application.History.Interfaces;

/// <summary>
/// Reads past supplier outcomes from storage.
/// </summary>
public interface ISearchHistoryReader
{
    /// <summary>
    /// Returns the page of outcomes that match the query.
    /// </summary>
    /// <param name="query">The filters, sort order and paging.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching outcomes.</returns>
    Task<PagedResult<HistoryItem>> QueryAsync(HistoryQuery query, CancellationToken cancellationToken);
}
