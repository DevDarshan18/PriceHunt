using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Searches.Interfaces;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Infrastructure.Persistence.Entities;

namespace PriceHunt.Infrastructure.Persistence.Repositories;

/// <summary>
/// Writes searches and supplier outcomes to the database.
/// </summary>
public sealed class ShippingSearchStore : IShippingSearchStore
{
    private readonly PriceHuntDbContext _context;

    public ShippingSearchStore(PriceHuntDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Stores a new search and the suppliers selected for it.
    /// </summary>
    /// <param name="search">The search that started.</param>
    /// <param name="suppliers">The suppliers selected for it.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    public async Task StartAsync(ShippingSearch search, IReadOnlyList<Supplier> suppliers, CancellationToken cancellationToken)
    {
        var names = suppliers.ToDictionary(supplier => supplier.Id, supplier => supplier.Name);
        var record = new SearchRecord
        {
            Id = search.Id,
            Origin = search.Route.Origin,
            Destination = search.Route.Destination,
            FromDate = search.DateRange.From,
            ToDate = search.DateRange.To,
            CreatedAtUtc = search.CreatedAtUtc,
            Status = search.Status.ToString(),
            SelectedSuppliers = search.SelectedSupplierIds
                .Select((supplierId, index) => new SelectedSupplierRecord
                {
                    SupplierId = supplierId,
                    SupplierName = names[supplierId],
                    SortOrder = index
                })
                .ToList()
        };

        _context.Searches.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stores what one supplier answered.
    /// </summary>
    /// <param name="searchId">The search the outcome belongs to.</param>
    /// <param name="supplier">The supplier that answered.</param>
    /// <param name="outcome">What the supplier answered.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    public async Task SaveOutcomeAsync(
        Guid searchId,
        Supplier supplier,
        SupplierOutcome outcome,
        CancellationToken cancellationToken)
    {
        var record = new SupplierOutcomeRecord
        {
            SearchId = searchId,
            SupplierId = supplier.Id,
            SupplierName = supplier.Name,
            Status = outcome.Status.ToString(),
            PriceCents = outcome.Price is null ? null : (long)(outcome.Price.Amount * 100),
            Currency = outcome.Price?.Currency,
            ResponseDurationMilliseconds = outcome.ResponseDurationMilliseconds,
            RespondedAtUtc = outcome.RespondedAtUtc,
            ErrorCode = outcome.ErrorCode
        };

        _context.SupplierOutcomes.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Stores the status the search ended in.
    /// </summary>
    /// <param name="searchId">The search that ended.</param>
    /// <param name="status">How the search ended.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    public async Task FinishAsync(Guid searchId, SearchStatus status, CancellationToken cancellationToken)
    {
        var updated = await _context.Searches
            .Where(search => search.Id == searchId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(search => search.Status, status.ToString()),
                cancellationToken);

        if (updated == 0)
        {
            throw new InvalidOperationException($"Search '{searchId}' was not found.");
        }
    }
}
