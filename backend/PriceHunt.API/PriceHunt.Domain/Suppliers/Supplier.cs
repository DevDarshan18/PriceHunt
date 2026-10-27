namespace PriceHunt.Domain.Suppliers;

/// <summary>
/// A shipping supplier that can be asked for a price.
/// </summary>
/// <param name="Id">Identifies the supplier. The history relies on it, so it never changes.</param>
/// <param name="Name">The name shown to the user.</param>
public sealed record Supplier(Guid Id, string Name);
