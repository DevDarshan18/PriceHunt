namespace PriceHunt.Application.History.Models;

/// <summary>
/// One supplier's outcome in one past search, as listed on the history screen.
/// </summary>
/// <param name="SearchId">The search this outcome belongs to.</param>
/// <param name="SearchedAtUtc">When the search started, in UTC.</param>
/// <param name="Origin">Where the shipment was to leave from.</param>
/// <param name="Destination">Where the shipment was to go.</param>
/// <param name="SupplierId">The supplier that was asked.</param>
/// <param name="SupplierName">The name of the supplier that was asked.</param>
/// <param name="Price">The price offered. Empty when the supplier gave none.</param>
/// <param name="Currency">The currency of the price. Empty when there is no price.</param>
/// <param name="ResponseTimeMilliseconds">How long the supplier took, or how long the search waited for it.</param>
/// <param name="Status">How the supplier's attempt ended.</param>
/// <param name="ErrorCode">Why the supplier failed. Empty when it did not fail.</param>
public sealed record HistoryItem(
    Guid SearchId,
    DateTime SearchedAtUtc,
    string Origin,
    string Destination,
    Guid SupplierId,
    string SupplierName,
    decimal? Price,
    string? Currency,
    int ResponseTimeMilliseconds,
    string Status,
    string? ErrorCode);
