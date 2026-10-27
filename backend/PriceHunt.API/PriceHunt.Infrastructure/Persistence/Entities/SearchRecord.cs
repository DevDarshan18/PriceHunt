namespace PriceHunt.Infrastructure.Persistence.Entities;

/// <summary>
/// A stored search.
/// </summary>
public sealed class SearchRecord
{
    /// <summary>
    /// Identifies the search.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Where the shipment was to leave from.
    /// </summary>
    public string Origin { get; set; } = string.Empty;

    /// <summary>
    /// Where the shipment was to go.
    /// </summary>
    public string Destination { get; set; } = string.Empty;

    /// <summary>
    /// The first day the shipment could leave.
    /// </summary>
    public DateOnly FromDate { get; set; }

    /// <summary>
    /// The last day the shipment could leave.
    /// </summary>
    public DateOnly ToDate { get; set; }

    /// <summary>
    /// When the search started, in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// How the search ended, or Running while it is going.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The suppliers that were asked.
    /// </summary>
    public List<SelectedSupplierRecord> SelectedSuppliers { get; set; } = new();

    /// <summary>
    /// What each supplier did.
    /// </summary>
    public List<SupplierOutcomeRecord> Outcomes { get; set; } = new();
}
