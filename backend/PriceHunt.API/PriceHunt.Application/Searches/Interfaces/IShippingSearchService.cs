using PriceHunt.Application.Searches.Models;

namespace PriceHunt.Application.Searches.Interfaces;

/// <summary>
/// Starts and runs searches across the suppliers.
/// </summary>
public interface IShippingSearchService
{
    /// <summary>
    /// Validates the request, stores the search and starts asking the suppliers. The updates arrive on the returned execution.
    /// </summary>
    /// <param name="request">The route, the dates and the suppliers asked for.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up. The suppliers are no longer waited for.</param>
    /// <returns>The started search, its updates, and a task that completes once the result is stored.</returns>
    Task<ShippingSearchExecution> BeginAsync(ShippingSearchRequest request, CancellationToken cancellationToken = default);
}
