using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Support;

public sealed class FakeSupplierClient : ISupplierClient
{
    private readonly Func<CancellationToken, Task<SupplierQuote>> _quote;

    public FakeSupplierClient(Supplier supplier, Func<CancellationToken, Task<SupplierQuote>> quote)
    {
        Supplier = supplier;
        _quote = quote;
    }

    public Supplier Supplier { get; }

    public Task<SupplierQuote> QuoteAsync(ShippingRoute route, SearchDateRange dates, CancellationToken cancellationToken) =>
        _quote(cancellationToken);
}
