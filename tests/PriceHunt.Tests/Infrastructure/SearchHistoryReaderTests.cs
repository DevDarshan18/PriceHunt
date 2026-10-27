using PriceHunt.Application.History.Models;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Tests.Infrastructure;

public sealed class SearchHistoryReaderTests : DatabaseTestBase
{
    private static readonly Guid Supplier1 = SimulatedSuppliers.AtlasFreightId;
    private static readonly Guid Supplier2 = SimulatedSuppliers.BlueWaveId;
    private static readonly Guid Supplier6 = SimulatedSuppliers.FastTrackId;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await SaveFinishedSearchAsync(
            new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            "Rotterdam",
            "Hamburg",
            (Supplier1, 120.50m, 900),
            (Supplier2, 80.00m, 1500));
        await SaveFinishedSearchAsync(
            new DateTime(2024, 5, 3, 23, 30, 0, DateTimeKind.Utc),
            "Oslo",
            "Bergen",
            (Supplier1, 300.00m, 700),
            (Supplier6, null, 2000));
        await SaveFinishedSearchAsync(
            new DateTime(2024, 5, 5, 8, 0, 0, DateTimeKind.Utc),
            "Rotterdam",
            "Antwerp",
            (Supplier2, 55.25m, 3000));
    }

    [Fact]
    public async Task Returns_every_supplier_outcome_with_its_search_details()
    {
        var result = await History.QueryAsync(Query(), CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        var first = result.Items[0];
        Assert.Equal("Rotterdam", first.Origin);
        Assert.Equal("Hamburg", first.Destination);
        Assert.Equal(new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc), first.SearchedAtUtc);
        Assert.Equal(DateTimeKind.Utc, first.SearchedAtUtc.Kind);
    }

    [Fact]
    public async Task Price_is_returned_in_decimal_units_and_is_empty_for_failures()
    {
        var result = await History.QueryAsync(Query(supplierIds: new[] { Supplier6, Supplier1 }), CancellationToken.None);

        Assert.Contains(result.Items, item => item.SupplierId == Supplier1 && item.Price == 120.50m && item.Currency == "USD");
        var failure = Assert.Single(result.Items, item => item.SupplierId == Supplier6);
        Assert.Null(failure.Price);
        Assert.Equal("Failed", failure.Status);
        Assert.Equal("SUPPLIER_FAILURE", failure.ErrorCode);
    }

    [Fact]
    public async Task Date_filter_includes_both_end_days()
    {
        var result = await History.QueryAsync(
            Query(from: new DateOnly(2024, 5, 3), to: new DateOnly(2024, 5, 3)),
            CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal("Oslo", item.Origin));
    }

    [Fact]
    public async Task Supplier_filter_accepts_one_or_several_suppliers()
    {
        var one = await History.QueryAsync(Query(supplierIds: new[] { Supplier2 }), CancellationToken.None);
        var several = await History.QueryAsync(Query(supplierIds: new[] { Supplier1, Supplier2 }), CancellationToken.None);

        Assert.Equal(2, one.TotalCount);
        Assert.Equal(4, several.TotalCount);
    }

    [Fact]
    public async Task Location_filters_match_the_exact_place()
    {
        var byOrigin = await History.QueryAsync(Query(origin: "Rotterdam"), CancellationToken.None);
        var byRoute = await History.QueryAsync(Query(origin: "Rotterdam", destination: "Antwerp"), CancellationToken.None);
        var partial = await History.QueryAsync(Query(origin: "Rotter"), CancellationToken.None);

        Assert.Equal(3, byOrigin.TotalCount);
        var single = Assert.Single(byRoute.Items);
        Assert.Equal("Antwerp", single.Destination);
        Assert.Equal(0, partial.TotalCount);
    }

    [Fact]
    public async Task Sorts_by_date_in_both_directions()
    {
        var oldest = await History.QueryAsync(Query(sortBy: HistorySortField.Date), CancellationToken.None);
        var newest = await History.QueryAsync(Query(sortBy: HistorySortField.Date, descending: true), CancellationToken.None);

        Assert.Equal(new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc), oldest.Items[0].SearchedAtUtc);
        Assert.Equal(new DateTime(2024, 5, 5, 8, 0, 0, DateTimeKind.Utc), newest.Items[0].SearchedAtUtc);
    }

    [Fact]
    public async Task Sorts_by_route_on_origin_then_destination()
    {
        var ascending = await History.QueryAsync(Query(sortBy: HistorySortField.Route), CancellationToken.None);
        var descending = await History.QueryAsync(Query(sortBy: HistorySortField.Route, descending: true), CancellationToken.None);

        Assert.Equal("Oslo", ascending.Items[0].Origin);
        Assert.Equal(("Rotterdam", "Hamburg"), (descending.Items[0].Origin, descending.Items[0].Destination));
    }

    [Fact]
    public async Task Sorts_by_status()
    {
        var ascending = await History.QueryAsync(Query(sortBy: HistorySortField.Status), CancellationToken.None);
        var descending = await History.QueryAsync(Query(sortBy: HistorySortField.Status, descending: true), CancellationToken.None);

        Assert.Equal("Failed", ascending.Items[0].Status);
        Assert.Equal("Succeeded", descending.Items[0].Status);
    }

    [Fact]
    public async Task Sorts_by_price_numerically()
    {
        var ascending = await History.QueryAsync(
            Query(sortBy: HistorySortField.Price, supplierIds: new[] { Supplier1, Supplier2 }),
            CancellationToken.None);
        var descending = await History.QueryAsync(
            Query(sortBy: HistorySortField.Price, descending: true, supplierIds: new[] { Supplier1, Supplier2 }),
            CancellationToken.None);

        Assert.Equal(new decimal?[] { 55.25m, 80.00m, 120.50m, 300.00m }, ascending.Items.Select(item => item.Price).ToArray());
        Assert.Equal(new decimal?[] { 300.00m, 120.50m, 80.00m, 55.25m }, descending.Items.Select(item => item.Price).ToArray());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Rows_without_a_price_come_last_in_either_direction(bool descending)
    {
        var result = await History.QueryAsync(Query(sortBy: HistorySortField.Price, descending: descending), CancellationToken.None);

        Assert.Null(result.Items[^1].Price);
        Assert.All(result.Items.Take(4), item => Assert.NotNull(item.Price));
    }

    [Fact]
    public async Task Sorts_by_supplier_and_by_response_time()
    {
        var bySupplier = await History.QueryAsync(Query(sortBy: HistorySortField.Supplier, descending: true), CancellationToken.None);
        var byTime = await History.QueryAsync(Query(sortBy: HistorySortField.ResponseTime), CancellationToken.None);

        Assert.Equal("FastTrack Couriers", bySupplier.Items[0].SupplierName);
        Assert.Equal(new[] { 700, 900, 1500, 2000, 3000 }, byTime.Items.Select(item => item.ResponseTimeMilliseconds).ToArray());
    }

    [Fact]
    public async Task Pages_through_the_results_without_overlap()
    {
        var first = await History.QueryAsync(Query(sortBy: HistorySortField.ResponseTime, pageSize: 2), CancellationToken.None);
        var second = await History.QueryAsync(Query(sortBy: HistorySortField.ResponseTime, page: 2, pageSize: 2), CancellationToken.None);
        var last = await History.QueryAsync(Query(sortBy: HistorySortField.ResponseTime, page: 3, pageSize: 2), CancellationToken.None);

        Assert.Equal(5, first.TotalCount);
        Assert.Equal(new[] { 700, 900 }, first.Items.Select(item => item.ResponseTimeMilliseconds).ToArray());
        Assert.Equal(new[] { 1500, 2000 }, second.Items.Select(item => item.ResponseTimeMilliseconds).ToArray());
        Assert.Equal(new[] { 3000 }, last.Items.Select(item => item.ResponseTimeMilliseconds).ToArray());
    }
}
