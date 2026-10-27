using System.Net.Http.Json;
using System.Text.Json;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Api.Support;
using static PriceHunt.Tests.Api.Support.ApiTestHelpers;

namespace PriceHunt.Tests.Api;

public class ApiConcurrencyTests : IClassFixture<FixedPriceApiFactory>
{
    private readonly HttpClient _client;

    public ApiConcurrencyTests(FixedPriceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Two_searches_at_the_same_time_each_get_their_own_complete_stream_and_history()
    {
        var firstRoute = NewRoute();
        var secondRoute = NewRoute();
        var suppliers = new[] { SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId };

        var streams = await Task.WhenAll(
            SearchAsync(_client, SearchBody(firstRoute, suppliers)),
            SearchAsync(_client, SearchBody(secondRoute, suppliers)));

        foreach (var events in streams)
        {
            Assert.Equal(new[] { "started", "supplier", "supplier", "finished" }, events.Select(item => item.Name).ToArray());
            Assert.Equal("Completed", events[^1].Data.GetProperty("status").GetString());
        }

        var searchIds = streams.Select(events => events[0].Data.GetProperty("searchId").GetString()).ToList();
        Assert.NotEqual(searchIds[0], searchIds[1]);

        foreach (var (route, searchId) in new[] { (firstRoute, searchIds[0]), (secondRoute, searchIds[1]) })
        {
            var history = await _client.GetFromJsonAsync<JsonElement>($"/api/history?{route.Query}");

            Assert.Equal(2, history.GetProperty("totalCount").GetInt32());
            Assert.All(history.GetProperty("items").EnumerateArray(), item => Assert.Equal(searchId, item.GetProperty("searchId").GetString()));
        }
    }
}
