namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// The origin and destination of a shipment.
/// </summary>
public sealed record ShippingRoute
{
    public ShippingRoute(string origin, string destination)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            throw new ArgumentException("Origin is required.", nameof(origin));
        }

        if (string.IsNullOrWhiteSpace(destination))
        {
            throw new ArgumentException("Destination is required.", nameof(destination));
        }

        if (string.Equals(origin.Trim(), destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Destination can't be the same as the origin.", nameof(destination));
        }

        Origin = origin.Trim();
        Destination = destination.Trim();
    }

    /// <summary>
    /// The place the shipment leaves from.
    /// </summary>
    public string Origin { get; }

    /// <summary>
    /// The place the shipment goes to.
    /// </summary>
    public string Destination { get; }

    public override string ToString() => $"{Origin} -> {Destination}";
}
