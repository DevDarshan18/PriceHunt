using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.History.Models;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.DI;
using PriceHunt.Infrastructure.Persistence;
using PriceHunt.Infrastructure.Persistence.Entities;
using PriceHunt.Infrastructure.Persistence.Mapping;
using PriceHunt.Infrastructure.Persistence.Repositories;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Infrastructure;

public abstract class DatabaseTestBase : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"pricehunt-{Guid.NewGuid():N}.db");
    private readonly List<IServiceScope> _scopes = new();

    protected ServiceProvider Services { get; private set; } = null!;

    protected ShippingSearchStore Store { get; private set; } = null!;

    protected SearchHistoryReader History { get; private set; } = null!;

    public virtual async Task InitializeAsync()
    {
        Services = new ServiceCollection()
            .AddDbContext<PriceHuntDbContext>(options => options.UseSqlite($"Data Source={_path}"))
            .BuildServiceProvider();

        await Services.InitializePriceHuntDatabaseAsync();

        Store = NewStore();
        History = new SearchHistoryReader(NewContext(), new HistoryMapper());
    }

    public async Task DisposeAsync()
    {
        foreach (var scope in _scopes)
        {
            scope.Dispose();
        }

        await Services.DisposeAsync();
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
        File.Delete(_path + "-wal");
        File.Delete(_path + "-shm");
    }

    protected ShippingSearchStore NewStore() => new(NewContext());

    protected PriceHuntDbContext NewContext()
    {
        var scope = Services.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>();
    }

    protected static ShippingSearch NewSearch(DateTime createdAtUtc, string origin, string destination, params Guid[] supplierIds) =>
        ShippingSearch.Start(
            new ShippingRoute(origin, destination),
            new SearchDateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 8)),
            supplierIds,
            createdAtUtc);

    protected static IReadOnlyList<Supplier> SuppliersOf(ShippingSearch search) =>
        search.SelectedSupplierIds
            .Select(id => TestSuppliers.Get(id))
            .ToList();

    protected async Task<SearchRecord> LoadAsync(Guid searchId) =>
        await NewContext().Searches
            .Include(search => search.SelectedSuppliers)
            .Include(search => search.Outcomes)
            .SingleAsync(search => search.Id == searchId);

    protected async Task SaveFinishedSearchAsync(
        DateTime createdAtUtc,
        string origin,
        string destination,
        params (Guid SupplierId, decimal? Price, int Milliseconds)[] results)
    {
        var search = NewSearch(createdAtUtc, origin, destination, results.Select(result => result.SupplierId).ToArray());
        var suppliers = SuppliersOf(search);
        await Store.StartAsync(search, suppliers, CancellationToken.None);

        foreach (var (supplierId, price, milliseconds) in results)
        {
            var supplier = suppliers.Single(item => item.Id == supplierId);
            var outcome = price is null
                ? SupplierOutcome.Failed(supplierId, milliseconds, "SUPPLIER_FAILURE", createdAtUtc)
                : SupplierOutcome.Succeeded(supplierId, new Money(price.Value, "USD"), milliseconds, createdAtUtc);
            await Store.SaveOutcomeAsync(search.Id, supplier, outcome, CancellationToken.None);
        }

        await Store.FinishAsync(search.Id, SearchStatus.Completed, CancellationToken.None);
    }

    protected static HistoryQuery Query(
        DateOnly? from = null,
        DateOnly? to = null,
        Guid[]? supplierIds = null,
        string? origin = null,
        string? destination = null,
        HistorySortField sortBy = HistorySortField.Date,
        bool descending = false,
        int page = 1,
        int pageSize = 50) =>
        new(from, to, supplierIds ?? Array.Empty<Guid>(), origin, destination, sortBy, descending, page, pageSize);
}
