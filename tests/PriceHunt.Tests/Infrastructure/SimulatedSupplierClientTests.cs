using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Infrastructure;

public class SimulatedSupplierClientTests
{
    private static readonly IOptions<SimulationOptions> DefaultOptions = Options.Create(new SimulationOptions());
    private static readonly ShippingRoute Route = new("Oslo", "Bergen");
    private static readonly SearchDateRange Dates = new(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public void There_are_seven_suppliers_behind_the_common_interface()
    {
        var clients = SimulatedSuppliers.CreateClients(SimulatedSuppliers.CreateDefaults(), Random.Shared, _time, DefaultOptions);

        Assert.Equal(7, clients.Count);
        Assert.Equal(7, clients.Select(client => client.Supplier.Id).Distinct().Count());
        Assert.Equal(7, clients.Select(client => client.Supplier.Name).Distinct().Count());
        Assert.All(clients, client => Assert.IsAssignableFrom<ISupplierClient>(client));
    }


    [Fact]
    public async Task A_standard_supplier_answers_after_its_delay_with_a_two_decimal_usd_price()
    {
        var random = new ScriptedRandom(750, 12_345);
        var client = Client(SimulatedSuppliers.AtlasFreightId, SupplierBehavior.Normal, random);

        var pending = client.QuoteAsync(Route, Dates, CancellationToken.None);
        _time.Advance(TimeSpan.FromMilliseconds(749));
        Assert.False(pending.IsCompleted);
        _time.Advance(TimeSpan.FromMilliseconds(1));

        var price = Assert.IsType<SupplierQuote.Price>(await pending);
        Assert.Equal(new Money(123.45m, "USD"), price.Amount);
        Assert.Equal(750, price.DurationMilliseconds);
        Assert.Equal((500, 5001), random.Calls[0]);
        Assert.Equal((10_000, 500_001), random.Calls[1]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public async Task FastTrack_fails_when_the_roll_is_below_30(int roll)
    {
        var random = new ScriptedRandom(1_000, roll);
        var client = Client(SimulatedSuppliers.FastTrackId, SupplierBehavior.FailsSometimes, random);

        var pending = client.QuoteAsync(Route, Dates, CancellationToken.None);
        _time.Advance(TimeSpan.FromMilliseconds(1_000));

        var failure = Assert.IsType<SupplierQuote.Failure>(await pending);
        Assert.Equal(SupplierErrorCodes.Failure, failure.ErrorCode);
        Assert.Equal(1_000, failure.DurationMilliseconds);
        Assert.Equal(new[] { (500, 5001), (0, 100) }, random.Calls);
    }

    [Fact]
    public async Task FastTrack_returns_a_price_when_the_roll_is_30_or_more()
    {
        var client = Client(SimulatedSuppliers.FastTrackId, SupplierBehavior.FailsSometimes, new ScriptedRandom(500, 30, 50_000));

        var pending = client.QuoteAsync(Route, Dates, CancellationToken.None);
        _time.Advance(TimeSpan.FromMilliseconds(500));

        var price = Assert.IsType<SupplierQuote.Price>(await pending);
        Assert.Equal(500.00m, price.Amount.Amount);
    }

    [Fact]
    public async Task GlobalPorts_never_answers_until_cancelled()
    {
        var client = Client(SimulatedSuppliers.GlobalPortsId, SupplierBehavior.NeverResponds, new ScriptedRandom());
        using var cancellation = new CancellationTokenSource();

        var pending = client.QuoteAsync(Route, Dates, cancellation.Token);
        _time.Advance(TimeSpan.FromHours(1));
        Assert.False(pending.IsCompleted);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task Delay_failure_rate_and_prices_follow_the_configured_options()
    {
        var options = Options.Create(new SimulationOptions
        {
            MinDelayMs = 10,
            MaxDelayMs = 20,
            FailureRatePercent = 100,
            MinPrice = 5m,
            MaxPrice = 6m
        });
        var random = new ScriptedRandom(15, 99);
        var client = Client(SimulatedSuppliers.FastTrackId, SupplierBehavior.FailsSometimes, random, options);

        var pending = client.QuoteAsync(Route, Dates, CancellationToken.None);
        _time.Advance(TimeSpan.FromMilliseconds(15));

        Assert.IsType<SupplierQuote.Failure>(await pending);
        Assert.Equal((10, 21), random.Calls[0]);
    }

    private SimulatedSupplierClient Client(Guid supplierId, SupplierBehavior behavior, Random random, IOptions<SimulationOptions>? options = null) =>
        new(TestSuppliers.Get(supplierId), behavior, random, _time, options ?? DefaultOptions);

    private sealed class ScriptedRandom : Random
    {
        private readonly Queue<int> _values;

        public ScriptedRandom(params int[] values) => _values = new Queue<int>(values);

        public List<(int Min, int Max)> Calls { get; } = new();

        public override int Next(int minValue, int maxValue)
        {
            Calls.Add((minValue, maxValue));
            var value = _values.Dequeue();
            Assert.InRange(value, minValue, maxValue - 1);
            return value;
        }
    }
}
