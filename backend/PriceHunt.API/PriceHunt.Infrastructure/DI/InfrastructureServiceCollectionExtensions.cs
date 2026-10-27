using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PriceHunt.Application.History;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.History.Validators;
using PriceHunt.Application.Locations;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Locations.Models;
using PriceHunt.Application.Locations.Validators;
using PriceHunt.Application.Searches;
using PriceHunt.Application.Searches.Interfaces;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Searches.Validators;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.Searches;
using PriceHunt.Infrastructure.Locations;
using PriceHunt.Infrastructure.Persistence;
using PriceHunt.Infrastructure.Persistence.Entities;
using PriceHunt.Infrastructure.Persistence.Mapping;
using PriceHunt.Infrastructure.Persistence.Repositories;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Infrastructure.DI;

/// <summary>
/// Registers the database, the suppliers and the services of the application.
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the API needs below the controllers, with the SQLite database at the given path.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="databasePath">Where the SQLite file is kept.</param>
    /// <returns>The same collection, so calls can be chained.</returns>
    public static IServiceCollection AddPriceHuntInfrastructure(this IServiceCollection services, string databasePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Random.Shared);
        services.AddSingleton(CreateSupplierCatalog);
        services.AddDbContext<PriceHuntDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<HistoryMapper>();
        services.AddScoped<ILocationStore, LocationStore>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IShippingSearchStore, ShippingSearchStore>();
        services.AddScoped<ISearchHistoryReader, SearchHistoryReader>();
        services.AddScoped<ISearchHistoryService, SearchHistoryService>();
        services.AddScoped<IShippingSearchService, ShippingSearchService>();
        services.AddScoped<IValidator<LocationSearchQuery>, LocationSearchQueryValidator>();
        services.AddScoped<IValidator<HistoryQuery>, HistoryQueryValidator>();
        services.AddScoped<IValidator<ShippingSearchRequest>, SearchRequestValidator>();

        return services;
    }

    /// <summary>
    /// Creates the database on the first run or brings an older one up to date with the migrations, adds any suppliers and places that are missing, and marks searches a previous run left running as cancelled.
    /// </summary>
    /// <param name="services">The built service provider.</param>
    /// <param name="cancellationToken">Cancels the startup work.</param>
    public static async Task InitializePriceHuntDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>();

        await context.Database.MigrateAsync(cancellationToken);

        await SeedSuppliersAsync(context, cancellationToken);
        await SeedLocationsAsync(context, cancellationToken);

        await context.Searches
            .Where(search => search.Status == nameof(SearchStatus.Running))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(search => search.Status, nameof(SearchStatus.Cancelled)),
                cancellationToken);
    }

    private static SupplierCatalog CreateSupplierCatalog(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        var suppliers = scope.ServiceProvider.GetRequiredService<PriceHuntDbContext>()
            .Suppliers
            .AsNoTracking()
            .OrderBy(supplier => supplier.Name)
            .ToList();

        return new SupplierCatalog(SimulatedSuppliers.CreateClients(
            suppliers,
            provider.GetRequiredService<Random>(),
            provider.GetRequiredService<TimeProvider>(),
            provider.GetRequiredService<IOptions<SimulationOptions>>()));
    }

    private static async Task SeedSuppliersAsync(PriceHuntDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Suppliers.Select(supplier => supplier.Id).ToListAsync(cancellationToken);

        context.Suppliers.AddRange(SimulatedSuppliers.CreateDefaults().Where(supplier => !existing.Contains(supplier.Id)));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedLocationsAsync(PriceHuntDbContext context, CancellationToken cancellationToken)
    {
        var existing = await context.Locations.Select(location => location.Name).ToListAsync(cancellationToken);
        var missing = KnownLocations.All.Except(existing, StringComparer.OrdinalIgnoreCase);

        context.Locations.AddRange(missing.Select(name => new LocationRecord { Name = name }));
        await context.SaveChangesAsync(cancellationToken);
    }
}
