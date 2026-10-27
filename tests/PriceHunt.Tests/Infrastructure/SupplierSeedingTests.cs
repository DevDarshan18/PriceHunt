using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.DI;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Tests.Infrastructure;

public sealed class SupplierSeedingTests : DatabaseTestBase
{
    [Fact]
    public async Task The_seven_suppliers_are_stored_on_the_first_run()
    {
        var stored = await NewContext().Suppliers.AsNoTracking().ToListAsync();

        Assert.Equal(7, stored.Count);
        Assert.Equal(5, stored.Count(supplier => supplier.Behavior == SupplierBehavior.Normal));
        Assert.Equal(SupplierBehavior.FailsSometimes, stored.Single(supplier => supplier.Id == SimulatedSuppliers.FastTrackId).Behavior);
        Assert.Equal(SupplierBehavior.NeverResponds, stored.Single(supplier => supplier.Id == SimulatedSuppliers.GlobalPortsId).Behavior);
    }

    [Fact]
    public async Task Starting_again_does_not_add_them_twice()
    {
        await Services.InitializePriceHuntDatabaseAsync();

        Assert.Equal(7, await NewContext().Suppliers.CountAsync());
    }

    [Fact]
    public async Task A_supplier_changed_in_the_table_is_not_overwritten_on_the_next_start()
    {
        var context = NewContext();
        var atlas = await context.Suppliers.SingleAsync(supplier => supplier.Id == SimulatedSuppliers.AtlasFreightId);
        atlas.Name = "Atlas Cargo";
        await context.SaveChangesAsync();

        await Services.InitializePriceHuntDatabaseAsync();

        var stored = await NewContext().Suppliers.SingleAsync(supplier => supplier.Id == SimulatedSuppliers.AtlasFreightId);
        Assert.Equal("Atlas Cargo", stored.Name);
    }

    [Fact]
    public async Task A_catalog_can_be_built_from_the_stored_suppliers()
    {
        var stored = await NewContext().Suppliers.AsNoTracking().ToListAsync();

        var catalog = new SupplierCatalog(
            SimulatedSuppliers.CreateClients(stored, Random.Shared, TimeProvider.System, Options.Create(new SimulationOptions())));

        Assert.Equal(7, catalog.All.Count);
        Assert.Contains(catalog.All, supplier => supplier.Name == "FastTrack Couriers");
    }
}
