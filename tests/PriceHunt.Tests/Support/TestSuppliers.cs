using Microsoft.Extensions.Options;
using PriceHunt.Application.Suppliers;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Tests.Support;

public static class TestSuppliers
{
    public static IReadOnlyList<Supplier> All { get; } = RealClients().Select(client => client.Supplier).ToList();

    public static IReadOnlyList<ISupplierClient> RealClients() =>
        SimulatedSuppliers.CreateClients(SimulatedSuppliers.CreateDefaults(), Random.Shared, TimeProvider.System, Options.Create(new SimulationOptions()));

    public static SupplierCatalog Catalog() => new(RealClients());

    public static Supplier Get(Guid supplierId) => All.Single(supplier => supplier.Id == supplierId);
}
