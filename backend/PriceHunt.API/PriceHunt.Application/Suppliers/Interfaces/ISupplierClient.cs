using PriceHunt.Application.Searches.Models;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Suppliers.Interfaces;

/// <summary>
/// The way to reach one supplier. Every supplier is used through this interface.
/// </summary>
public interface ISupplierClient
{
    /// <summary>
    /// The supplier this client talks to.
    /// </summary>
    Supplier Supplier { get; }

    /// <summary>
    /// Asks the supplier for a price. The call can be cancelled.
    /// </summary>
    /// <param name="route">Where the shipment goes from and to.</param>
    /// <param name="dates">The days the shipment can leave.</param>
    /// <param name="cancellationToken">Cancelled when the search stops waiting for the supplier.</param>
    /// <returns>The price offered, or the failure the supplier answered with.</returns>
    Task<SupplierQuote> QuoteAsync(ShippingRoute route, SearchDateRange dates, CancellationToken cancellationToken);
}
