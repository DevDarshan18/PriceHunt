using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Searches;

/// <summary>
/// One search across the selected suppliers, with the outcome each supplier gave and the status the search ended in.
/// </summary>
public sealed class ShippingSearch
{
    private readonly List<SupplierOutcome> _outcomes = new();

    private ShippingSearch(ShippingRoute route, SearchDateRange dateRange, IReadOnlyList<Guid> supplierIds, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        Route = route;
        DateRange = dateRange;
        SelectedSupplierIds = supplierIds;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>
    /// Identifies the search.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Where the shipment goes from and to.
    /// </summary>
    public ShippingRoute Route { get; }

    /// <summary>
    /// The days the shipment can leave.
    /// </summary>
    public SearchDateRange DateRange { get; }

    /// <summary>
    /// The suppliers this search asks.
    /// </summary>
    public IReadOnlyList<Guid> SelectedSupplierIds { get; }

    /// <summary>
    /// When the search started, in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Running until the search ends, then how it ended.
    /// </summary>
    public SearchStatus Status { get; private set; } = SearchStatus.Running;

    /// <summary>
    /// What each supplier did so far.
    /// </summary>
    public IReadOnlyList<SupplierOutcome> Outcomes => _outcomes;

    /// <summary>
    /// Starts a search in the running state. At least one supplier is needed and none may be repeated.
    /// </summary>
    /// <param name="route">Where the shipment goes from and to.</param>
    /// <param name="dateRange">The days the shipment can leave.</param>
    /// <param name="supplierIds">The suppliers to ask.</param>
    /// <param name="createdAtUtc">When the search starts, in UTC.</param>
    /// <returns>The new search, in the running state.</returns>
    public static ShippingSearch Start(
        ShippingRoute route,
        SearchDateRange dateRange,
        IEnumerable<Guid> supplierIds,
        DateTime createdAtUtc)
    {
        var selected = supplierIds.ToList();

        if (selected.Count == 0)
        {
            throw new ArgumentException("Select at least one supplier.", nameof(supplierIds));
        }

        if (selected.Distinct().Count() != selected.Count)
        {
            throw new ArgumentException("A supplier can only be selected once.", nameof(supplierIds));
        }

        return new ShippingSearch(route, dateRange, selected, createdAtUtc);
    }

    /// <summary>
    /// Records what a supplier did. Only while the search is running, and once per supplier.
    /// </summary>
    /// <param name="outcome">What the supplier did.</param>
    public void RecordOutcome(SupplierOutcome outcome)
    {
        if (Status != SearchStatus.Running)
        {
            throw new InvalidOperationException($"The search is {Status}, so no more outcomes can be recorded.");
        }

        if (_outcomes.Any(recorded => recorded.SupplierId == outcome.SupplierId))
        {
            throw new InvalidOperationException("This supplier already has an outcome.");
        }

        _outcomes.Add(outcome);
    }

    /// <summary>
    /// Ends the search because every supplier answered.
    /// </summary>
    public void Complete() => Finish(SearchStatus.Completed);

    /// <summary>
    /// Ends the search because time ran out before every supplier answered.
    /// </summary>
    public void TimeOut() => Finish(SearchStatus.TimedOut);

    /// <summary>
    /// Ends the search because the caller cancelled.
    /// </summary>
    public void Cancel() => Finish(SearchStatus.Cancelled);

    private void Finish(SearchStatus status)
    {
        if (Status != SearchStatus.Running)
        {
            throw new InvalidOperationException($"The search is already {Status}.");
        }

        Status = status;
    }
}
