namespace PriceHunt.API.Contracts.Events;

/// <summary>
/// Sent each time a supplier answers.
/// </summary>
/// <param name="SearchId">The search the supplier answered.</param>
/// <param name="SupplierId">The supplier that answered.</param>
/// <param name="SupplierName">The name of the supplier that answered.</param>
/// <param name="Status">Succeeded or Failed.</param>
/// <param name="Price">The price offered. Empty when the supplier failed.</param>
/// <param name="Currency">The currency of the price. Empty when the supplier failed.</param>
/// <param name="ResponseTimeMilliseconds">How long the supplier took to answer.</param>
/// <param name="ErrorCode">Why the supplier failed. Empty when it did not fail.</param>
public sealed record SupplierResultEvent(
    Guid SearchId,
    Guid SupplierId,
    string SupplierName,
    string Status,
    decimal? Price,
    string? Currency,
    int ResponseTimeMilliseconds,
    string? ErrorCode);
