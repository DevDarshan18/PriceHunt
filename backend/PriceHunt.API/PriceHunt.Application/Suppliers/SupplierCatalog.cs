using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;

namespace PriceHunt.Application.Suppliers;

/// <summary>
/// The suppliers a search can ask, and the client used to reach each one.
/// </summary>
public sealed class SupplierCatalog
{
    private readonly IReadOnlyDictionary<Guid, ISupplierClient> _clients;

    public SupplierCatalog(IEnumerable<ISupplierClient> clients)
    {
        var available = clients.ToList();
        _clients = available.ToDictionary(client => client.Supplier.Id);
        All = available.Select(client => client.Supplier).ToList();
    }

    /// <summary>
    /// Every supplier that can be asked.
    /// </summary>
    public IReadOnlyList<Supplier> All { get; }

    /// <summary>
    /// Tells whether the id belongs to a supplier that can be asked.
    /// </summary>
    /// <param name="supplierId">The id to look for.</param>
    /// <returns>True when the supplier can be asked.</returns>
    public bool Contains(Guid supplierId) => _clients.ContainsKey(supplierId);

    /// <summary>
    /// Returns the client for a supplier. Throws when the supplier is not in the catalog.
    /// </summary>
    /// <param name="supplierId">The supplier to reach.</param>
    /// <returns>The client for that supplier.</returns>
    public ISupplierClient GetClient(Guid supplierId)
    {
        if (_clients.TryGetValue(supplierId, out var client))
        {
            return client;
        }

        throw new ArgumentException($"Supplier '{supplierId}' is not available.", nameof(supplierId));
    }

    /// <summary>
    /// Returns the suppliers with the given ids, or all of them when no ids are given.
    /// </summary>
    /// <param name="supplierIds">The ids asked for. Null means every supplier.</param>
    /// <returns>The matching suppliers.</returns>
    public IReadOnlyList<Supplier> Resolve(IReadOnlyList<Guid>? supplierIds) =>
        supplierIds is null ? All : supplierIds.Select(id => GetClient(id).Supplier).ToList();
}
