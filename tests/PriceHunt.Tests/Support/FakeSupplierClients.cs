using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Tests.Support;

public static class FakeSupplierClients
{
    public static IReadOnlyList<ISupplierClient> Immediate() => Create(ImmediateQuote);

    public static IReadOnlyList<ISupplierClient> OneCrashing() => Create(CrashingQuote);

    public static IReadOnlyList<ISupplierClient> FixedPrice() => Create(FixedPriceQuote);

    private static IReadOnlyList<ISupplierClient> Create(Func<Supplier, CancellationToken, Task<SupplierQuote>> quote) =>
        TestSuppliers.All
            .Select(supplier => (ISupplierClient)new FakeSupplierClient(supplier, token => quote(supplier, token)))
            .ToList();

    private static async Task<SupplierQuote> ImmediateQuote(Supplier supplier, CancellationToken cancellationToken)
    {
        if (supplier.Id == SimulatedSuppliers.GlobalPortsId)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        if (supplier.Id == SimulatedSuppliers.FastTrackId)
        {
            return new SupplierQuote.Failure(SupplierErrorCodes.Failure, 800);
        }

        var amount = supplier.Id == SimulatedSuppliers.BlueWaveId ? 10.00m
            : supplier.Id == SimulatedSuppliers.DeltaLogisticsId ? 20.00m
            : supplier.Id == SimulatedSuppliers.EuroLineId ? 30.00m
            : supplier.Id == SimulatedSuppliers.CargoNovaId ? 40.00m
            : 50.00m;

        return new SupplierQuote.Price(new Money(amount, "USD"), 500);
    }

    private static Task<SupplierQuote> CrashingQuote(Supplier supplier, CancellationToken cancellationToken)
    {
        if (supplier.Id == SimulatedSuppliers.FastTrackId)
        {
            throw new InvalidOperationException("Simulated supplier crash.");
        }

        return Task.FromResult<SupplierQuote>(new SupplierQuote.Price(new Money(15.00m, "USD"), 500));
    }

    private static async Task<SupplierQuote> FixedPriceQuote(Supplier supplier, CancellationToken cancellationToken)
    {
        if (supplier.Id == SimulatedSuppliers.GlobalPortsId)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        var price = supplier.Id == SimulatedSuppliers.AtlasFreightId ? 100.00m : 200.00m;
        return new SupplierQuote.Price(new Money(price, "USD"), 10);
    }
}

public sealed class GatedSupplierClients
{
    private readonly Dictionary<Guid, TaskCompletionSource<SupplierQuote>> _gates = new();

    public GatedSupplierClients()
    {
        Clients = TestSuppliers.All
            .Select(supplier => (ISupplierClient)new FakeSupplierClient(supplier, token => Wait(supplier, token)))
            .ToList();
    }

    public IReadOnlyList<ISupplierClient> Clients { get; }

    public void Succeed(Guid supplierId, decimal amount) =>
        _gates[supplierId].TrySetResult(new SupplierQuote.Price(new Money(amount, "USD"), 500));

    private Task<SupplierQuote> Wait(Supplier supplier, CancellationToken cancellationToken)
    {
        var gate = new TaskCompletionSource<SupplierQuote>(TaskCreationOptions.RunContinuationsAsynchronously);
        _gates[supplier.Id] = gate;
        cancellationToken.Register(() => gate.TrySetCanceled(cancellationToken));
        return gate.Task;
    }
}
