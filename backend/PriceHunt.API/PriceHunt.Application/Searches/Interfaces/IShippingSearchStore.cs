using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;

namespace PriceHunt.Application.Searches.Interfaces;

/// <summary>
/// Stores searches and what each supplier answered.
/// </summary>
public interface IShippingSearchStore
{
    /// <summary>
    /// Stores a new search and the suppliers selected for it.
    /// </summary>
    /// <param name="search">The search that started.</param>
    /// <param name="suppliers">The suppliers selected for it.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    Task StartAsync(ShippingSearch search, IReadOnlyList<Supplier> suppliers, CancellationToken cancellationToken);

    /// <summary>
    /// Stores what one supplier answered.
    /// </summary>
    /// <param name="searchId">The search the outcome belongs to.</param>
    /// <param name="supplier">The supplier that answered.</param>
    /// <param name="outcome">What the supplier answered.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    Task SaveOutcomeAsync(Guid searchId, Supplier supplier, SupplierOutcome outcome, CancellationToken cancellationToken);

    /// <summary>
    /// Stores the status the search ended in.
    /// </summary>
    /// <param name="searchId">The search that ended.</param>
    /// <param name="status">How the search ended.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    Task FinishAsync(Guid searchId, SearchStatus status, CancellationToken cancellationToken);
}
