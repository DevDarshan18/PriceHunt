using PriceHunt.Application.History.Models;

namespace PriceHunt.API.Contracts.Requests;

/// <summary>
/// Query string of the history request.
/// </summary>
public sealed class HistoryRequestDto
{
    /// <summary>
    /// Only searches started on or after this day.
    /// </summary>
    public DateOnly? From { get; init; }

    /// <summary>
    /// Only searches started on or before this day.
    /// </summary>
    public DateOnly? To { get; init; }

    /// <summary>
    /// Only these suppliers. Empty means all of them.
    /// </summary>
    public List<Guid> SupplierIds { get; init; } = new();

    /// <summary>
    /// Only searches from this place, spelled as in the list of places.
    /// </summary>
    public string? Origin { get; init; }

    /// <summary>
    /// Only searches to this place, spelled as in the list of places.
    /// </summary>
    public string? Destination { get; init; }

    /// <summary>
    /// The column to sort by: date, route, supplier, status, price or responseTime.
    /// </summary>
    public HistorySortField SortBy { get; init; } = HistorySortField.Date;

    /// <summary>
    /// True to sort from highest to lowest. The default is true.
    /// </summary>
    public bool Descending { get; init; } = true;

    /// <summary>
    /// The page wanted, starting at 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// How many rows a page holds. The default is 20 and the most is 100.
    /// </summary>
    public int PageSize { get; init; } = 20;
}
