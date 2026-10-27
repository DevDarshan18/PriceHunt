namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// The first and last day a shipment can leave.
/// </summary>
public sealed record SearchDateRange
{
    public SearchDateRange(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            throw new ArgumentException("The end date can't be before the start date.", nameof(to));
        }

        From = from;
        To = to;
    }

    /// <summary>
    /// The first day the shipment can leave.
    /// </summary>
    public DateOnly From { get; }

    /// <summary>
    /// The last day the shipment can leave.
    /// </summary>
    public DateOnly To { get; }
}
