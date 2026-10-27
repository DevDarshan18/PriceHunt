namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// How a simulated supplier behaves.
/// </summary>
public enum SupplierBehavior
{
    /// <summary>
    /// Always answers with a price.
    /// </summary>
    Normal,

    /// <summary>
    /// Answers, but returns an error part of the time.
    /// </summary>
    FailsSometimes,

    /// <summary>
    /// Never answers, so the search has to give up on it.
    /// </summary>
    NeverResponds
}
