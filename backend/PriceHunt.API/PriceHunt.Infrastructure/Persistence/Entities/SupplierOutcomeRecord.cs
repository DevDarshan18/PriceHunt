namespace PriceHunt.Infrastructure.Persistence.Entities;

/// <summary>
/// What one supplier did in a stored search.
/// </summary>
public sealed class SupplierOutcomeRecord
{
    /// <summary>
    /// The search this outcome belongs to.
    /// </summary>
    public Guid SearchId { get; set; }

    /// <summary>
    /// The supplier this outcome belongs to.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// The name the supplier had at the time.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// How the supplier's attempt ended.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The price in cents. Empty when the supplier gave none.
    /// </summary>
    public long? PriceCents { get; set; }

    /// <summary>
    /// The currency of the price. Empty when there is no price.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// How long the supplier took, or how long the search waited for it.
    /// </summary>
    public int ResponseDurationMilliseconds { get; set; }

    /// <summary>
    /// When the supplier answered, in UTC. Empty when it never answered.
    /// </summary>
    public DateTime? RespondedAtUtc { get; set; }

    /// <summary>
    /// Why the supplier failed. Empty when it did not fail.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// The search this outcome belongs to.
    /// </summary>
    public SearchRecord Search { get; set; } = null!;
}
