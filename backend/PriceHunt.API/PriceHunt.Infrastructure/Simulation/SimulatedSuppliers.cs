using Microsoft.Extensions.Options;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Infrastructure.Persistence.Entities;

namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// The seven simulated suppliers and how they are stored. The ids are fixed because the stored history refers to them.
/// </summary>
public static class SimulatedSuppliers
{
    public static readonly Guid AtlasFreightId = Guid.Parse("bb31cb32-e965-4f79-aa43-99142ff0180b");
    public static readonly Guid BlueWaveId = Guid.Parse("bee17723-da70-4af9-9889-6baaa6661af6");
    public static readonly Guid CargoNovaId = Guid.Parse("1131cb16-419d-45af-bbde-1128d66106ef");
    public static readonly Guid DeltaLogisticsId = Guid.Parse("ae27b613-0e16-4626-bcb9-a5cc4541e120");
    public static readonly Guid EuroLineId = Guid.Parse("b5bd05a3-5d43-4c9b-ba9a-3fb1530853a7");
    public static readonly Guid FastTrackId = Guid.Parse("367c4f12-5ace-429c-aed1-37f16172a99d");
    public static readonly Guid GlobalPortsId = Guid.Parse("0ba72a1f-61a4-4b7e-8011-6d1c4dc40aa8");

    /// <summary>
    /// The suppliers stored on the first run: five that behave normally, one that fails some of the time and one that never answers.
    /// </summary>
    /// <returns>New records each time, so no instance is shared between database contexts.</returns>
    public static IReadOnlyList<SupplierRecord> CreateDefaults() => new[]
    {
        Record(AtlasFreightId, "Atlas Freight", SupplierBehavior.Normal),
        Record(BlueWaveId, "BlueWave Shipping", SupplierBehavior.Normal),
        Record(CargoNovaId, "CargoNova", SupplierBehavior.Normal),
        Record(DeltaLogisticsId, "Delta Logistics", SupplierBehavior.Normal),
        Record(EuroLineId, "EuroLine Express", SupplierBehavior.Normal),
        Record(FastTrackId, "FastTrack Couriers", SupplierBehavior.FailsSometimes),
        Record(GlobalPortsId, "GlobalPorts Transit", SupplierBehavior.NeverResponds)
    };

    /// <summary>
    /// Creates one simulated client for each stored supplier.
    /// </summary>
    /// <param name="suppliers">The stored suppliers.</param>
    /// <param name="random">The source of the delays and prices.</param>
    /// <param name="time">The clock the delays run on.</param>
    /// <param name="options">The simulation settings.</param>
    /// <returns>One client per supplier.</returns>
    public static IReadOnlyList<ISupplierClient> CreateClients(
        IEnumerable<SupplierRecord> suppliers,
        Random random,
        TimeProvider time,
        IOptions<SimulationOptions> options)
    {
        return suppliers
            .Select(record => new SimulatedSupplierClient(new Supplier(record.Id, record.Name), record.Behavior, random, time, options))
            .ToList();
    }

    private static SupplierRecord Record(Guid id, string name, SupplierBehavior behavior) =>
        new() { Id = id, Name = name, Behavior = behavior };
}
