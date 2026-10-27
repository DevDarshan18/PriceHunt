namespace PriceHunt.API.Contracts.Requests;

/// <summary>
/// Body of the search request.
/// </summary>
/// <param name="FromLocation">The place the shipment leaves from, chosen from the list of places.</param>
/// <param name="ToLocation">The place the shipment goes to, chosen from the list of places.</param>
/// <param name="FromDate">The first day the shipment can leave. Not in the past.</param>
/// <param name="ToDate">The last day the shipment can leave. At most 90 days after the from date.</param>
/// <param name="SupplierIds">The suppliers to ask. Leave out to ask all of them.</param>
public sealed record SearchRequestDto(
    string FromLocation,
    string ToLocation,
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<Guid>? SupplierIds);
