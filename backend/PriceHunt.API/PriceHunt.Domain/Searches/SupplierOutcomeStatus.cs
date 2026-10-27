namespace PriceHunt.Domain.Searches;

/// <summary>
/// How a supplier's attempt ended.
/// </summary>
public enum SupplierOutcomeStatus
{
    Succeeded,
    Failed,
    TimedOut,
    Cancelled
}
