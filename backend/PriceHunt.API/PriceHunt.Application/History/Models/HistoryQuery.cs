namespace PriceHunt.Application.History.Models;

/// <summary>
/// Filters, sort order and paging for the history list.
/// </summary>
/// <param name="From">Only searches started on or after this day.</param>
/// <param name="To">Only searches started on or before this day.</param>
/// <param name="SupplierIds">Only these suppliers. Empty means all of them.</param>
/// <param name="Origin">Only searches from this place.</param>
/// <param name="Destination">Only searches to this place.</param>
/// <param name="SortBy">The column to sort by.</param>
/// <param name="Descending">True to sort from highest to lowest.</param>
/// <param name="Page">The page wanted, starting at 1.</param>
/// <param name="PageSize">How many rows a page holds.</param>
public sealed record HistoryQuery(
    DateOnly? From,
    DateOnly? To,
    IReadOnlyList<Guid> SupplierIds,
    string? Origin,
    string? Destination,
    HistorySortField SortBy,
    bool Descending,
    int Page,
    int PageSize);
