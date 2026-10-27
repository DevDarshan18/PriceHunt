namespace PriceHunt.Infrastructure.Persistence.Entities;

/// <summary>
/// A place that can be chosen as an origin or destination.
/// </summary>
public sealed class LocationRecord
{
    /// <summary>
    /// Identifies the place.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the place. Unique, ignoring case.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
