using System.Diagnostics;
using Microsoft.Extensions.Options;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// A supplier that answers after a random delay with a random price, and can be set to fail some of the time or never answer.
/// </summary>
public sealed class SimulatedSupplierClient : ISupplierClient
{
    private readonly SupplierBehavior _behavior;
    private readonly Random _random;
    private readonly TimeProvider _time;
    private readonly SimulationOptions _options;

    public SimulatedSupplierClient(
        Supplier supplier,
        SupplierBehavior behavior,
        Random random,
        TimeProvider time,
        IOptions<SimulationOptions> options)
    {
        Supplier = supplier;
        _behavior = behavior;
        _random = random;
        _time = time;
        _options = options.Value;
    }

    /// <summary>
    /// The supplier this client talks to.
    /// </summary>
    public Supplier Supplier { get; }

    /// <summary>
    /// Asks the supplier for a price. The call can be cancelled.
    /// </summary>
    /// <param name="route">Where the shipment goes from and to.</param>
    /// <param name="dates">The days the shipment can leave.</param>
    /// <param name="cancellationToken">Cancelled when the search stops waiting for the supplier.</param>
    /// <returns>The price offered, or the failure the supplier answered with.</returns>
    public async Task<SupplierQuote> QuoteAsync(ShippingRoute route, SearchDateRange dates, CancellationToken cancellationToken)
    {
        if (_behavior == SupplierBehavior.NeverResponds)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, _time, cancellationToken);
            throw new UnreachableException();
        }

        var delayMs = _random.Next(_options.MinDelayMs, _options.MaxDelayMs + 1);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), _time, cancellationToken);

        if (_behavior == SupplierBehavior.FailsSometimes && _random.Next(0, 100) < _options.FailureRatePercent)
        {
            return new SupplierQuote.Failure(SupplierErrorCodes.Failure, delayMs);
        }

        return new SupplierQuote.Price(NextPrice(), delayMs);
    }

    private Money NextPrice()
    {
        var cents = _random.Next((int)(_options.MinPrice * 100), (int)(_options.MaxPrice * 100) + 1);
        return new Money(cents / 100m, _options.Currency);
    }
}
