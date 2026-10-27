using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure.Persistence.Entities;

/// <summary>
/// A supplier that a search can ask.
/// </summary>
public sealed class SupplierRecord
{
    /// <summary>
    /// Identifies the supplier. The history refers to it, so it never changes.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name shown to the user. Unique, ignoring case.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// How the simulation makes this supplier behave.
    /// </summary>
    public SupplierBehavior Behavior { get; set; }
}
