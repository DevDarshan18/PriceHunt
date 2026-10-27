using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Api.Support;
using static PriceHunt.Tests.Api.Support.ApiTestHelpers;

namespace PriceHunt.Tests.Api;

public sealed class ApiTests : IClassFixture<FixedPriceApiFactory>
{
    private readonly HttpClient _client;

    public ApiTests(FixedPriceApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Lists_the_seven_suppliers()
    {
        var suppliers = await _client.GetFromJsonAsync<JsonElement>("/api/suppliers");

        Assert.Equal(7, suppliers.GetArrayLength());
        Assert.Equal("Atlas Freight", suppliers[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Locations_come_ten_at_a_time_in_alphabetical_order()
    {
        var first = await _client.GetFromJsonAsync<JsonElement>("/api/locations");
        var second = await _client.GetFromJsonAsync<JsonElement>("/api/locations?page=2");

        var firstNames = first.GetProperty("items").EnumerateArray().Select(item => item.GetString()!).ToList();
        var secondNames = second.GetProperty("items").EnumerateArray().Select(item => item.GetString()!).ToList();
        Assert.Equal(10, firstNames.Count);
        Assert.Equal(10, secondNames.Count);
        Assert.Equal(firstNames.Order().ToList(), firstNames);
        Assert.True(string.CompareOrdinal(firstNames[^1], secondNames[0]) < 0);
        Assert.True(first.GetProperty("totalCount").GetInt32() > 20);
    }

    [Fact]
    public async Task Locations_can_be_searched_and_an_unmatched_search_is_empty()
    {
        var found = await _client.GetFromJsonAsync<JsonElement>("/api/locations?search=rotter");
        var none = await _client.GetFromJsonAsync<JsonElement>("/api/locations?search=atlantis");

        Assert.Equal("Rotterdam", Assert.Single(found.GetProperty("items").EnumerateArray()).GetString());
        Assert.Equal(0, none.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Locations_reject_an_oversized_page()
    {
        var response = await _client.GetAsync("/api/locations?pageSize=500");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_streams_started_then_each_supplier_then_finished()
    {
        var route = NewRoute();

        var response = await _client.PostAsJsonAsync("/api/searches", SearchBody(route, SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId));
        var events = ParseEvents(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new[] { "started", "supplier", "supplier", "finished" }, events.Select(item => item.Name).ToArray());
        Assert.Equal(2, events[0].Data.GetProperty("suppliers").GetArrayLength());
        Assert.Equal("Succeeded", events[1].Data.GetProperty("status").GetString());
        Assert.Equal("Completed", events[3].Data.GetProperty("status").GetString());
        Assert.Equal(2, events[3].Data.GetProperty("respondedCount").GetInt32());
    }

    [Fact]
    public async Task Search_without_a_supplier_list_queries_all_seven()
    {
        using var disconnect = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var started = await ReadUntilEventAsync(_client, SearchBody(NewRoute(), null), "started", disconnect.Token, disconnect);

        Assert.Equal(7, started.GetProperty("suppliers").GetArrayLength());
    }

    [Fact]
    public async Task Search_with_invalid_input_is_rejected_before_streaming()
    {
        var body = new
        {
            fromLocation = "Oslo",
            toLocation = "Oslo",
            fromDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-10),
            toDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-12),
            supplierIds = (Guid[]?)null
        };

        var response = await _client.PostAsJsonAsync("/api/searches", body);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("fromDate", out _));
        Assert.True(errors.TryGetProperty("toDate", out _));
        Assert.True(errors.TryGetProperty("toLocation", out _));
        Assert.False(string.IsNullOrEmpty(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Search_results_are_stored_and_available_in_the_history()
    {
        var route = NewRoute();
        await _client.PostAsJsonAsync("/api/searches", SearchBody(route, SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId));

        var history = await _client.GetFromJsonAsync<JsonElement>($"/api/history?{route.Query}&sortBy=price&descending=false");

        Assert.Equal(2, history.GetProperty("totalCount").GetInt32());
        var items = history.GetProperty("items");
        Assert.Equal("Atlas Freight", items[0].GetProperty("supplierName").GetString());
        Assert.Equal(100.00m, items[0].GetProperty("price").GetDecimal());
        Assert.Equal(200.00m, items[1].GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task History_filters_by_supplier_and_pages_the_results()
    {
        var route = NewRoute();
        await _client.PostAsJsonAsync("/api/searches", SearchBody(route, SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId));

        var bySupplier = await _client.GetFromJsonAsync<JsonElement>(
            $"/api/history?{route.Query}&supplierIds={SimulatedSuppliers.BlueWaveId}");
        var firstPage = await _client.GetFromJsonAsync<JsonElement>($"/api/history?{route.Query}&pageSize=1");

        Assert.Equal(1, bySupplier.GetProperty("totalCount").GetInt32());
        Assert.Equal("BlueWave Shipping", bySupplier.GetProperty("items")[0].GetProperty("supplierName").GetString());
        Assert.Equal(2, firstPage.GetProperty("totalCount").GetInt32());
        Assert.Equal(1, firstPage.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task History_rejects_an_invalid_page_size()
    {
        var response = await _client.GetAsync("/api/history?pageSize=1000");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(problem.GetProperty("errors").TryGetProperty("pageSize", out _));
    }

    [Fact]
    public async Task Disconnecting_cancels_the_search_and_stores_it_as_cancelled()
    {
        var route = NewRoute();
        using var disconnect = new CancellationTokenSource();
        var body = SearchBody(route, SimulatedSuppliers.GlobalPortsId);

        await ReadUntilEventAsync(_client, body, "started", disconnect.Token, disconnectAfterEvent: disconnect);

        var stored = await WaitForHistoryAsync(route, item => item.GetProperty("status").GetString() == "Cancelled");
        Assert.True(stored, "The disconnected search was not stored as cancelled.");
    }

    private async Task<bool> WaitForHistoryAsync(TestRoute route, Func<JsonElement, bool> predicate)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            var history = await _client.GetFromJsonAsync<JsonElement>($"/api/history?{route.Query}");
            if (history.GetProperty("items").EnumerateArray().Any(predicate))
            {
                return true;
            }

            await Task.Delay(100);
        }

        return false;
    }
}
