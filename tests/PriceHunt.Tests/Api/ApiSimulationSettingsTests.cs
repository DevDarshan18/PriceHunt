using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Api.Support;
using static PriceHunt.Tests.Api.Support.ApiTestHelpers;

namespace PriceHunt.Tests.Api;

public class ApiSimulationSettingsTests
{
    [Fact]
    public async Task Simulation_settings_in_the_configuration_change_how_suppliers_behave()
    {
        await using var factory = new TestApiFactory
        {
            Settings = new Dictionary<string, string>
            {
                ["Simulation:MinDelayMs"] = "5",
                ["Simulation:MaxDelayMs"] = "10",
                ["Simulation:FailureRatePercent"] = "100",
                ["Simulation:MinPrice"] = "7",
                ["Simulation:MaxPrice"] = "7",
                ["Simulation:Currency"] = "EUR"
            }
        };
        using var client = factory.CreateClient();

        var events = await SearchAsync(client, SearchBody(NewRoute(), SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.FastTrackId));

        var results = events.Where(item => item.Name == "supplier").Select(item => item.Data).ToList();
        var atlas = results.Single(item => item.GetProperty("supplierName").GetString() == "Atlas Freight");
        var fastTrack = results.Single(item => item.GetProperty("supplierName").GetString() == "FastTrack Couriers");

        Assert.Equal(7.00m, atlas.GetProperty("price").GetDecimal());
        Assert.Equal("EUR", atlas.GetProperty("currency").GetString());
        Assert.Equal("Failed", fastTrack.GetProperty("status").GetString());
        Assert.Equal("SUPPLIER_FAILURE", fastTrack.GetProperty("errorCode").GetString());
        Assert.InRange(atlas.GetProperty("responseTimeMilliseconds").GetInt32(), 5, 200);
        Assert.Equal("Completed", events[^1].Data.GetProperty("status").GetString());
    }
}
