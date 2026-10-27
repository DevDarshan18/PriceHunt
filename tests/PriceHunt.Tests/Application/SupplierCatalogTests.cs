using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Application;

public class SupplierCatalogTests
{
    private readonly SupplierCatalog _catalog = TestSuppliers.Catalog();

    [Fact]
    public void Exposes_seven_distinct_named_suppliers()
    {
        Assert.Equal(
            new[] { "Atlas Freight", "BlueWave Shipping", "CargoNova", "Delta Logistics", "EuroLine Express", "FastTrack Couriers", "GlobalPorts Transit" },
            _catalog.All.Select(supplier => supplier.Name).ToArray());
        Assert.Equal(7, _catalog.All.Select(supplier => supplier.Id).Distinct().Count());
    }

    [Fact]
    public void Gives_each_supplier_its_own_client()
    {
        foreach (var supplier in _catalog.All)
        {
            Assert.Equal(supplier, _catalog.GetClient(supplier.Id).Supplier);
        }

        Assert.Throws<ArgumentException>(() => _catalog.GetClient(Guid.NewGuid()));
    }

    [Fact]
    public void Null_selection_includes_every_supplier()
    {
        Assert.Equal(7, _catalog.Resolve(null).Count);
    }

    [Fact]
    public void Selection_keeps_the_requested_order()
    {
        var resolved = _catalog.Resolve(new[] { SimulatedSuppliers.CargoNovaId, SimulatedSuppliers.AtlasFreightId });

        Assert.Equal(new[] { "CargoNova", "Atlas Freight" }, resolved.Select(supplier => supplier.Name).ToArray());
    }

    [Fact]
    public void An_unknown_supplier_is_rejected()
    {
        Assert.Throws<ArgumentException>(() => _catalog.Resolve(new[] { Guid.NewGuid() }));
    }
}
