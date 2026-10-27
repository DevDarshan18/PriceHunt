using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Application.Searches;
using PriceHunt.Application.Searches.Interfaces;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Searches.Validators;
using PriceHunt.Application.Suppliers;
using PriceHunt.Application.Suppliers.Interfaces;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Application;

public class ShippingSearchServiceTests
{
    private static readonly DateTimeOffset Now = new(2024, 5, 6, 7, 8, 9, TimeSpan.Zero);

    private readonly FakeTimeProvider _time = new(Now);

    [Fact]
    public async Task Search_is_still_running_just_before_the_six_second_limit()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.GlobalPortsId));

        _time.Advance(TimeSpan.FromMilliseconds(5999));

        Assert.False(execution.Completed.IsCompleted);
        _time.Advance(TimeSpan.FromMilliseconds(1));
        await execution.Completed;
        Assert.Equal(SearchStatus.TimedOut, execution.Search.Status);
    }

    [Fact]
    public async Task Rejects_identical_locations_and_an_empty_supplier_list()
    {
        var service = CreateService(FakeSupplierClients.Immediate());

        await Assert.ThrowsAsync<ValidationException>(() => service.BeginAsync(
            new ShippingSearchRequest("Rotterdam", "rotterdam", new DateOnly(2024, 5, 1), new DateOnly(2024, 5, 2))));
        await Assert.ThrowsAsync<ValidationException>(() => service.BeginAsync(
            new ShippingSearchRequest("Rotterdam", "Hamburg", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 8), Array.Empty<Guid>())));
    }

    [Fact]
    public async Task Rejects_a_place_that_is_not_in_the_list()
    {
        var service = CreateService(FakeSupplierClients.Immediate());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.BeginAsync(
            new ShippingSearchRequest("Atlantis", "Hamburg", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 8))));

        Assert.Equal("Choose a place from the list.", Assert.Single(exception.ErrorsByField()["fromLocation"]));
    }

    [Fact]
    public async Task Stores_the_places_under_their_proper_names_whatever_the_user_typed()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(new ShippingSearchRequest("  hamburg ", "ROTTERDAM", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 8), new[] { SimulatedSuppliers.AtlasFreightId }));

        await execution.Completed;

        Assert.Equal(("Hamburg", "Rotterdam"), (execution.Search.Route.Origin, execution.Search.Route.Destination));
    }

    [Fact]
    public async Task Search_is_stamped_with_the_current_utc_time()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId));

        await execution.Completed;

        Assert.Equal(SearchStatus.Completed, execution.Search.Status);
        Assert.Equal(Now.UtcDateTime, execution.Search.CreatedAtUtc);
    }

    [Fact]
    public async Task Streams_each_response_as_it_arrives_then_finishes()
    {
        var gates = new GatedSupplierClients();
        var execution = await CreateService(gates.Clients)
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId));
        await using var enumerator = execution.Events.GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.IsType<ShippingSearchUpdate.Started>(enumerator.Current);

        gates.Succeed(SimulatedSuppliers.AtlasFreightId, 50.00m);
        Assert.True(await enumerator.MoveNextAsync());
        var first = Assert.IsType<ShippingSearchUpdate.SupplierResponded>(enumerator.Current);
        Assert.Equal(SimulatedSuppliers.AtlasFreightId, first.Supplier.Id);

        gates.Succeed(SimulatedSuppliers.BlueWaveId, 10.00m);
        Assert.True(await enumerator.MoveNextAsync());
        var second = Assert.IsType<ShippingSearchUpdate.SupplierResponded>(enumerator.Current);
        Assert.Equal(SimulatedSuppliers.BlueWaveId, second.Supplier.Id);

        Assert.True(await enumerator.MoveNextAsync());
        var finished = Assert.IsType<ShippingSearchUpdate.Finished>(enumerator.Current);
        Assert.Equal(SearchStatus.Completed, finished.Status);
        Assert.Equal(2, finished.RespondedCount);
        Assert.False(await enumerator.MoveNextAsync());
        await execution.Completed;
    }

    [Fact]
    public async Task One_supplier_failing_does_not_affect_the_others()
    {
        var execution = await CreateService(FakeSupplierClients.OneCrashing())
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.FastTrackId));

        var events = await ReadAllAsync(execution);
        var finished = Assert.IsType<ShippingSearchUpdate.Finished>(events[^1]);

        Assert.Equal(SearchStatus.Completed, finished.Status);
        Assert.Equal(2, finished.RespondedCount);
        Assert.Contains(execution.Search.Outcomes, outcome => outcome.SupplierId == SimulatedSuppliers.AtlasFreightId && outcome.Price is not null);
        var failed = Assert.Single(execution.Search.Outcomes, outcome => outcome.SupplierId == SimulatedSuppliers.FastTrackId);
        Assert.Equal(SupplierOutcomeStatus.Failed, failed.Status);
        Assert.Equal(SupplierErrorCodes.Unexpected, failed.ErrorCode);
    }

    [Fact]
    public async Task Supplier_that_never_responds_times_out_when_the_deadline_passes()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.GlobalPortsId));

        _time.Advance(TimeSpan.FromSeconds(6));
        await execution.Completed;

        Assert.Equal(SearchStatus.TimedOut, execution.Search.Status);
        Assert.Contains(execution.Search.Outcomes, outcome => outcome.SupplierId == SimulatedSuppliers.AtlasFreightId && outcome.Status == SupplierOutcomeStatus.Succeeded);
        var unanswered = Assert.Single(execution.Search.Outcomes, outcome => outcome.SupplierId == SimulatedSuppliers.GlobalPortsId);
        Assert.Equal(SupplierOutcomeStatus.TimedOut, unanswered.Status);
        Assert.Equal(6000, unanswered.ResponseDurationMilliseconds);
    }

    [Fact]
    public async Task Waiting_time_of_a_supplier_that_never_responds_never_exceeds_the_time_limit()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.GlobalPortsId));

        _time.Advance(TimeSpan.FromMilliseconds(6400));
        await execution.Completed;

        var unanswered = Assert.Single(execution.Search.Outcomes, outcome => outcome.SupplierId == SimulatedSuppliers.GlobalPortsId);
        Assert.Equal(6000, unanswered.ResponseDurationMilliseconds);
    }

    [Fact]
    public async Task Default_search_times_out_and_keeps_the_responses_collected()
    {
        var execution = await CreateService(FakeSupplierClients.Immediate()).BeginAsync(Request());

        _time.Advance(TimeSpan.FromSeconds(6));
        var events = await ReadAllAsync(execution);
        var finished = Assert.IsType<ShippingSearchUpdate.Finished>(events[^1]);

        Assert.Equal(SearchStatus.TimedOut, finished.Status);
        Assert.Equal(7, finished.SelectedCount);
        Assert.Equal(6, finished.RespondedCount);
    }

    [Fact]
    public async Task Cancelling_the_caller_stops_the_supplier_that_never_responds_and_cancels_the_search()
    {
        using var caller = new CancellationTokenSource();
        var execution = await CreateService(FakeSupplierClients.Immediate())
            .BeginAsync(Request(SimulatedSuppliers.GlobalPortsId), caller.Token);

        caller.Cancel();
        await execution.Completed;

        Assert.Equal(SearchStatus.Cancelled, execution.Search.Status);
        Assert.Equal(SupplierOutcomeStatus.Cancelled, Assert.Single(execution.Search.Outcomes).Status);
    }

    [Fact]
    public async Task Search_is_stored_when_it_starts_and_each_outcome_as_it_is_recorded()
    {
        var store = new RecordingSearchStore();
        var gates = new GatedSupplierClients();
        var execution = await CreateService(gates.Clients, store)
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.BlueWaveId));
        await using var enumerator = execution.Events.GetAsyncEnumerator();

        await enumerator.MoveNextAsync();
        Assert.Equal(execution.Search.Id, store.Started?.Id);
        Assert.Empty(store.Outcomes);

        gates.Succeed(SimulatedSuppliers.AtlasFreightId, 50.00m);
        await enumerator.MoveNextAsync();
        Assert.Equal((SimulatedSuppliers.AtlasFreightId, SupplierOutcomeStatus.Succeeded), Assert.Single(store.Outcomes));
        Assert.Null(store.FinishedStatus);

        gates.Succeed(SimulatedSuppliers.BlueWaveId, 10.00m);
        while (await enumerator.MoveNextAsync())
        {
        }

        await execution.Completed;
        Assert.Equal(2, store.Outcomes.Count);
        Assert.Equal(SearchStatus.Completed, store.FinishedStatus);
    }

    [Fact]
    public async Task Timed_out_search_stores_the_missing_supplier_and_the_final_status()
    {
        var store = new RecordingSearchStore();
        var execution = await CreateService(FakeSupplierClients.Immediate(), store)
            .BeginAsync(Request(SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.GlobalPortsId));

        _time.Advance(TimeSpan.FromSeconds(6));
        await execution.Completed;

        Assert.Contains((SimulatedSuppliers.GlobalPortsId, SupplierOutcomeStatus.TimedOut), store.Outcomes);
        Assert.Equal(SearchStatus.TimedOut, store.FinishedStatus);
    }

    [Fact]
    public async Task Cancelled_search_is_stored_as_cancelled()
    {
        var store = new RecordingSearchStore();
        using var caller = new CancellationTokenSource();
        var execution = await CreateService(FakeSupplierClients.Immediate(), store)
            .BeginAsync(Request(SimulatedSuppliers.GlobalPortsId), caller.Token);

        caller.Cancel();
        await execution.Completed;

        Assert.Equal((SimulatedSuppliers.GlobalPortsId, SupplierOutcomeStatus.Cancelled), Assert.Single(store.Outcomes));
        Assert.Equal(SearchStatus.Cancelled, store.FinishedStatus);
    }

    private ShippingSearchService CreateService(IEnumerable<ISupplierClient> clients, IShippingSearchStore? store = null)
    {
        var catalog = new SupplierCatalog(clients);
        var locations = TestLocations.Service();
        var validator = new SearchRequestValidator(locations, catalog, _time);

        return new ShippingSearchService(catalog, locations, validator, store ?? new RecordingSearchStore(), _time, NullLogger<ShippingSearchService>.Instance);
    }

    private static ShippingSearchRequest Request(params Guid[] supplierIds) => new(
        "Rotterdam",
        "Hamburg",
        new DateOnly(2024, 6, 1),
        new DateOnly(2024, 6, 8),
        supplierIds.Length == 0 ? null : supplierIds);

    private static async Task<IReadOnlyList<ShippingSearchUpdate>> ReadAllAsync(ShippingSearchExecution execution)
    {
        var events = new List<ShippingSearchUpdate>();
        await foreach (var update in execution.Events)
        {
            events.Add(update);
        }

        await execution.Completed;
        return events;
    }

    private sealed class RecordingSearchStore : IShippingSearchStore
    {
        public ShippingSearch? Started { get; private set; }

        public List<(Guid SupplierId, SupplierOutcomeStatus Status)> Outcomes { get; } = new();

        public SearchStatus? FinishedStatus { get; private set; }

        public Task StartAsync(ShippingSearch search, IReadOnlyList<Supplier> suppliers, CancellationToken cancellationToken)
        {
            Started = search;
            return Task.CompletedTask;
        }

        public Task SaveOutcomeAsync(Guid searchId, Supplier supplier, SupplierOutcome outcome, CancellationToken cancellationToken)
        {
            Outcomes.Add((supplier.Id, outcome.Status));
            return Task.CompletedTask;
        }

        public Task FinishAsync(Guid searchId, SearchStatus status, CancellationToken cancellationToken)
        {
            FinishedStatus = status;
            return Task.CompletedTask;
        }
    }
}
