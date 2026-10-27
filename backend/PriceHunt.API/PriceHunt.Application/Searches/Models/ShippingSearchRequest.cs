namespace PriceHunt.Application.Searches.Models;

/// <summary>
/// What the user asked for: the route, the dates and, optionally, which suppliers to ask.
/// </summary>
/// <param name="FromLocation">The place the shipment leaves from.</param>
/// <param name="ToLocation">The place the shipment goes to.</param>
/// <param name="FromDate">The first day the shipment can leave.</param>
/// <param name="ToDate">The last day the shipment can leave.</param>
/// <param name="SupplierIds">The suppliers to ask. Empty or missing means all of them.</param>
public sealed record ShippingSearchRequest(
    string FromLocation,
    string ToLocation,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<Guid>? SupplierIds = null);
