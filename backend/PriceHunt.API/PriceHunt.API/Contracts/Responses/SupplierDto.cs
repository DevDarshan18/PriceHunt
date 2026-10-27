namespace PriceHunt.API.Contracts.Responses;

/// <summary>
/// A supplier as returned by the API.
/// </summary>
/// <param name="Id">Identifies the supplier.</param>
/// <param name="Name">The name shown to the user.</param>
public sealed record SupplierDto(Guid Id, string Name);
