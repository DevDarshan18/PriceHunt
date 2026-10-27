using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Suppliers;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Api.Support;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"pricehunt-api-{Guid.NewGuid():N}.db");

    public IReadOnlyDictionary<string, string> Settings { get; init; } = new Dictionary<string, string>();

    public Action<IServiceCollection>? OverrideServices { get; init; }

    public ILoggerProvider? LoggerProvider { get; init; }

    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Path", _databasePath);

        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            ConfigureTestServices(services);
            OverrideServices?.Invoke(services);
        });

        if (LoggerProvider is not null)
        {
            builder.ConfigureLogging(logging => logging.AddProvider(LoggerProvider));
        }
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        SqliteConnection.ClearAllPools();

        foreach (var path in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}

public sealed class FixedPriceApiFactory : TestApiFactory
{
    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<SupplierCatalog>();
        services.AddSingleton(new SupplierCatalog(FakeSupplierClients.FixedPrice()));
    }
}
