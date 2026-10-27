using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.ValueObjects;
using PriceHunt.Infrastructure.DI;
using PriceHunt.Infrastructure.Simulation;

namespace PriceHunt.Tests.Infrastructure;

public sealed class ShippingSearchStoreTests : DatabaseTestBase
{
    private static readonly DateTime Created = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    [Fact]
    public async Task Start_stores_a_running_search_with_its_parameters_and_suppliers()
    {
        var search = NewSearch(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.FastTrackId);

        await Store.StartAsync(search, SuppliersOf(search), CancellationToken.None);

        var stored = await LoadAsync(search.Id);
        Assert.Equal("Rotterdam", stored.Origin);
        Assert.Equal("Hamburg", stored.Destination);
        Assert.Equal(new DateOnly(2024, 6, 1), stored.FromDate);
        Assert.Equal(new DateOnly(2024, 6, 8), stored.ToDate);
        Assert.Equal(Created, stored.CreatedAtUtc);
        Assert.Equal(nameof(SearchStatus.Running), stored.Status);
        Assert.Equal(
            new[] { "Atlas Freight", "FastTrack Couriers" },
            stored.SelectedSuppliers.OrderBy(item => item.SortOrder).Select(item => item.SupplierName).ToArray());
    }

    [Fact]
    public async Task Outcomes_are_stored_for_a_price_a_failure_and_a_timeout()
    {
        var search = NewSearch(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.FastTrackId, SimulatedSuppliers.GlobalPortsId);
        var suppliers = SuppliersOf(search);
        var responded = Created.AddSeconds(1);
        await Store.StartAsync(search, suppliers, CancellationToken.None);

        await Store.SaveOutcomeAsync(
            search.Id,
            suppliers[0],
            SupplierOutcome.Succeeded(suppliers[0].Id, new Money(19.95m, "USD"), 800, responded),
            CancellationToken.None);
        await Store.SaveOutcomeAsync(
            search.Id,
            suppliers[1],
            SupplierOutcome.Failed(suppliers[1].Id, 1200, SupplierErrorCodes.Failure, responded),
            CancellationToken.None);
        await Store.SaveOutcomeAsync(
            search.Id,
            suppliers[2],
            SupplierOutcome.TimedOut(suppliers[2].Id, 6000),
            CancellationToken.None);

        var stored = await LoadAsync(search.Id);

        var price = Assert.Single(stored.Outcomes, item => item.SupplierId == SimulatedSuppliers.AtlasFreightId);
        Assert.Equal(nameof(SupplierOutcomeStatus.Succeeded), price.Status);
        Assert.Equal(1995, price.PriceCents);
        Assert.Equal("USD", price.Currency);
        Assert.Equal(800, price.ResponseDurationMilliseconds);
        Assert.Equal(responded, price.RespondedAtUtc);

        var failed = Assert.Single(stored.Outcomes, item => item.SupplierId == SimulatedSuppliers.FastTrackId);
        Assert.Equal(nameof(SupplierOutcomeStatus.Failed), failed.Status);
        Assert.Equal(SupplierErrorCodes.Failure, failed.ErrorCode);
        Assert.Null(failed.PriceCents);

        var timedOut = Assert.Single(stored.Outcomes, item => item.SupplierId == SimulatedSuppliers.GlobalPortsId);
        Assert.Equal(nameof(SupplierOutcomeStatus.TimedOut), timedOut.Status);
        Assert.Null(timedOut.ErrorCode);
        Assert.Null(timedOut.RespondedAtUtc);
        Assert.Equal(6000, timedOut.ResponseDurationMilliseconds);
    }

    [Theory]
    [InlineData(SearchStatus.Completed)]
    [InlineData(SearchStatus.TimedOut)]
    [InlineData(SearchStatus.Cancelled)]
    public async Task Finish_stores_the_final_status(SearchStatus status)
    {
        var search = NewSearch(SimulatedSuppliers.AtlasFreightId);
        await Store.StartAsync(search, SuppliersOf(search), CancellationToken.None);

        await Store.FinishAsync(search.Id, status, CancellationToken.None);

        Assert.Equal(status.ToString(), (await LoadAsync(search.Id)).Status);
    }

    [Fact]
    public async Task Searches_left_running_by_a_crash_are_cancelled_on_startup()
    {
        var search = NewSearch(SimulatedSuppliers.AtlasFreightId);
        await Store.StartAsync(search, SuppliersOf(search), CancellationToken.None);

        await Services.InitializePriceHuntDatabaseAsync();

        Assert.Equal(nameof(SearchStatus.Cancelled), (await LoadAsync(search.Id)).Status);
    }

    [Fact]
    public async Task Finish_rejects_an_unknown_search()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Store.FinishAsync(Guid.NewGuid(), SearchStatus.Completed, CancellationToken.None));
    }

    [Fact]
    public async Task Several_searches_can_write_at_the_same_time()
    {
        var searches = Enumerable.Range(0, 5).Select(_ => NewSearch(SimulatedSuppliers.AtlasFreightId)).ToList();

        await Task.WhenAll(searches.Select(async search =>
        {
            var store = NewStore();
            var suppliers = SuppliersOf(search);
            await store.StartAsync(search, suppliers, CancellationToken.None);
            await store.SaveOutcomeAsync(
                search.Id,
                suppliers[0],
                SupplierOutcome.Succeeded(suppliers[0].Id, new Money(10m, "USD"), 500, Created),
                CancellationToken.None);
            await store.FinishAsync(search.Id, SearchStatus.Completed, CancellationToken.None);
        }));

        Assert.Equal(5, await NewContext().SupplierOutcomes.CountAsync());
    }

    private static ShippingSearch NewSearch(params Guid[] supplierIds) =>
        NewSearch(Created, "Rotterdam", "Hamburg", supplierIds);
}
