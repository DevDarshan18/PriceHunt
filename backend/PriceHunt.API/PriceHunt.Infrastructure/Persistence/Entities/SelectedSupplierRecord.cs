namespace PriceHunt.Infrastructure.Persistence.Entities;

/// <summary>
/// A supplier that was selected for a stored search.
/// </summary>
public sealed class SelectedSupplierRecord
{
    /// <summary>
    /// The search this supplier was selected for.
    /// </summary>
    public Guid SearchId { get; set; }

    /// <summary>
    /// The supplier that was selected.
    /// </summary>
    public Guid SupplierId { get; set; }

    /// <summary>
    /// The name the supplier had at the time.
    /// </summary>
    public string SupplierName { get; set; } = string.Empty;

    /// <summary>
    /// The position of the supplier in the search.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The search this supplier was selected for.
    /// </summary>
    public SearchRecord Search { get; set; } = null!;
}
