using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;

namespace PriceHunt.Application.Searches.Models;

/// <summary>
/// A step in a running search, sent to the caller as it happens.
/// </summary>
public abstract record ShippingSearchUpdate
{
    private ShippingSearchUpdate(Guid searchId)
    {
        SearchId = searchId;
    }

    /// <summary>
    /// The search this update belongs to.
    /// </summary>
    public Guid SearchId { get; }

    /// <summary>
    /// The search has been stored and the suppliers are being asked.
    /// </summary>
    /// <param name="SearchId">The search that started.</param>
    /// <param name="CreatedAtUtc">When the search started, in UTC.</param>
    /// <param name="Suppliers">The suppliers being asked.</param>
    public sealed record Started(
        Guid SearchId,
        DateTime CreatedAtUtc,
        IReadOnlyList<Supplier> Suppliers) : ShippingSearchUpdate(SearchId);

    /// <summary>
    /// One supplier has answered, with a price or a failure.
    /// </summary>
    /// <param name="SearchId">The search the supplier answered.</param>
    /// <param name="Supplier">The supplier that answered.</param>
    /// <param name="Outcome">What the supplier answered.</param>
    public sealed record SupplierResponded(
        Guid SearchId,
        Supplier Supplier,
        SupplierOutcome Outcome) : ShippingSearchUpdate(SearchId);

    /// <summary>
    /// The search is over and its final status is known.
    /// </summary>
    /// <param name="SearchId">The search that ended.</param>
    /// <param name="Status">How the search ended.</param>
    /// <param name="SelectedCount">How many suppliers were asked.</param>
    /// <param name="RespondedCount">How many of them answered.</param>
    public sealed record Finished(
        Guid SearchId,
        SearchStatus Status,
        int SelectedCount,
        int RespondedCount) : ShippingSearchUpdate(SearchId);
}
