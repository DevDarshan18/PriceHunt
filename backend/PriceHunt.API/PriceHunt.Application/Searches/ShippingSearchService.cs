using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using FluentValidation;
using Microsoft.Extensions.Logging;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Searches.Interfaces;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Searches;

/// <summary>
/// Runs a search: asks every selected supplier at the same time, stores each answer, and stops at the six second limit or when the caller cancels.
/// </summary>
public sealed class ShippingSearchService : IShippingSearchService
{
    private static readonly TimeSpan TimeLimit = TimeSpan.FromSeconds(6);

    private readonly SupplierCatalog _catalog;
    private readonly ILocationService _locations;
    private readonly IValidator<ShippingSearchRequest> _validator;
    private readonly IShippingSearchStore _store;
    private readonly TimeProvider _time;
    private readonly ILogger<ShippingSearchService> _logger;
    private DateTime UtcNow => _time.GetUtcNow().UtcDateTime;

    public ShippingSearchService(
        SupplierCatalog catalog,
        ILocationService locations,
        IValidator<ShippingSearchRequest> validator,
        IShippingSearchStore store,
        TimeProvider time,
        ILogger<ShippingSearchService> logger)
    {
        _catalog = catalog;
        _locations = locations;
        _validator = validator;
        _store = store;
        _time = time;
        _logger = logger;
    }

    /// <summary>
    /// Validates the request, stores the search and starts asking the suppliers. The updates arrive on the returned execution.
    /// </summary>
    /// <param name="request">The route, the dates and the suppliers asked for.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up. The suppliers are no longer waited for.</param>
    /// <returns>The started search, its updates, and a task that completes once the result is stored.</returns>
    public async Task<ShippingSearchExecution> BeginAsync(ShippingSearchRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var origin = await _locations.FindCanonicalAsync(request.FromLocation, cancellationToken);
        var destination = await _locations.FindCanonicalAsync(request.ToLocation, cancellationToken);

        var suppliers = _catalog.Resolve(request.SupplierIds);
        var search = ShippingSearch.Start(
            new ShippingRoute(origin!, destination!),
            new SearchDateRange(request.FromDate, request.ToDate),
            suppliers.Select(supplier => supplier.Id),
            UtcNow);

        var channel = Channel.CreateUnbounded<ShippingSearchUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        var completed = RunAsync(search, suppliers, channel.Writer, cancellationToken);
        return new ShippingSearchExecution(search, channel.Reader.ReadAllAsync(), completed);
    }

    /// <summary>
    /// Runs the search in the background and feeds the channel the caller reads from. A failure does not stop
    /// the work half way: the suppliers are always waited for and the search is always closed before the
    /// channel is completed with that failure, so the stored status and what the caller sees agree.
    /// </summary>
    private async Task RunAsync(
        ShippingSearch search,
        IReadOnlyList<Supplier> suppliers,
        ChannelWriter<ShippingSearchUpdate> writer,
        CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(TimeLimit, _time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var run = new SearchRun(search, suppliers, _time.GetTimestamp(), cancellationToken, timeout);
        var pending = new List<Task<QuoteCompletion>>(suppliers.Count);
        var stored = false;
        Exception? failure = null;

        try
        {
            _logger.LogInformation(
                "Search {SearchId} started for {Origin} to {Destination} with {SupplierCount} suppliers.",
                search.Id,
                search.Route.Origin,
                search.Route.Destination,
                suppliers.Count);

            await _store.StartAsync(search, suppliers, CancellationToken.None);
            stored = true;

            pending.AddRange(suppliers.Select(supplier => QuoteOneAsync(search, supplier, linked.Token)));
            await writer.WriteAsync(new ShippingSearchUpdate.Started(search.Id, search.CreatedAtUtc, suppliers));
            await RelayAnswersAsync(search, pending, writer);

            await CloseAsync(run);
            await writer.WriteAsync(CreateFinished(search));

            _logger.LogInformation("Search {SearchId} finished with status {Status}.", search.Id, search.Status);
        }
        catch (Exception exception)
        {
            failure = exception;
            _logger.LogError(exception, "Search {SearchId} failed unexpectedly.", search.Id);
        }

        linked.Cancel();
        await WaitForSuppliersAsync(search.Id, pending);

        if (stored && search.Status == SearchStatus.Running)
        {
            failure ??= await CloseAfterFailureAsync(run);
        }

        writer.TryComplete(failure);

        if (failure is not null)
        {
            ExceptionDispatchInfo.Throw(failure);
        }
    }

    private async Task RelayAnswersAsync(
        ShippingSearch search,
        List<Task<QuoteCompletion>> pending,
        ChannelWriter<ShippingSearchUpdate> writer)
    {
        while (pending.Count > 0)
        {
            var done = await Task.WhenAny(pending);
            pending.Remove(done);
            var completion = await done;
            if (completion.Outcome is null)
            {
                continue;
            }

            await RecordAsync(search, completion.Supplier, completion.Outcome);
            await writer.WriteAsync(new ShippingSearchUpdate.SupplierResponded(search.Id, completion.Supplier, completion.Outcome));
        }
    }

    private async Task<Exception?> CloseAfterFailureAsync(SearchRun run)
    {
        try
        {
            await CloseAsync(run);
            return null;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Search {SearchId} could not be closed cleanly.", run.Search.Id);
            return exception;
        }
    }

    private async Task<QuoteCompletion> QuoteOneAsync(ShippingSearch search, Supplier supplier, CancellationToken cancellationToken)
    {
        var startedAt = _time.GetTimestamp();
        try
        {
            var quote = await _catalog.GetClient(supplier.Id).QuoteAsync(search.Route, search.DateRange, cancellationToken);
            var outcome = ToOutcome(supplier, quote, UtcNow);

            _logger.LogInformation(
                "Search {SearchId} received {OutcomeStatus} from {SupplierName} in {DurationMilliseconds} ms.",
                search.Id,
                outcome.Status,
                supplier.Name,
                outcome.ResponseDurationMilliseconds);

            return new QuoteCompletion(supplier, outcome);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Search {SearchId} stopped waiting for {SupplierName}.", search.Id, supplier.Name);
            return new QuoteCompletion(supplier, null);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Search {SearchId} isolated a failure from {SupplierName}.", search.Id, supplier.Name);

            var failed = SupplierOutcome.Failed(
                supplier.Id,
                ElapsedMilliseconds(startedAt),
                SupplierErrorCodes.Unexpected,
                UtcNow);
            return new QuoteCompletion(supplier, failed);
        }
    }

    private static SupplierOutcome ToOutcome(Supplier supplier, SupplierQuote quote, DateTime respondedAtUtc) => quote switch
    {
        SupplierQuote.Price price => SupplierOutcome.Succeeded(
            supplier.Id,
            price.Amount,
            price.DurationMilliseconds,
            respondedAtUtc),
        SupplierQuote.Failure failure => SupplierOutcome.Failed(
            supplier.Id,
            failure.DurationMilliseconds,
            failure.ErrorCode,
            respondedAtUtc),
        _ => throw new InvalidOperationException("Supplier returned an unknown quote.")
    };

    private async Task RecordAsync(ShippingSearch search, Supplier supplier, SupplierOutcome outcome)
    {
        search.RecordOutcome(outcome);

        try
        {
            await _store.SaveOutcomeAsync(search.Id, supplier, outcome, CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Search {SearchId} could not store the outcome of {SupplierName}.",
                search.Id,
                supplier.Name);
        }
    }

    private async Task CloseAsync(SearchRun run)
    {
        var (search, suppliers, startedAt, cancellationToken, timeout) = run;

        if (search.Status != SearchStatus.Running)
        {
            return;
        }

        var answered = search.Outcomes.Select(outcome => outcome.SupplierId).ToHashSet();
        var unanswered = suppliers.Where(supplier => !answered.Contains(supplier.Id)).ToList();
        var waited = Math.Min(ElapsedMilliseconds(startedAt), (int)TimeLimit.TotalMilliseconds);

        var cancelled = cancellationToken.IsCancellationRequested;
        var timedOut = !cancelled && (timeout.IsCancellationRequested || unanswered.Count > 0);

        foreach (var supplier in unanswered)
        {
            var outcome = cancelled
                ? SupplierOutcome.Cancelled(supplier.Id, waited)
                : SupplierOutcome.TimedOut(supplier.Id, waited);
            await RecordAsync(search, supplier, outcome);
        }

        if (cancelled)
        {
            search.Cancel();
        }
        else if (timedOut)
        {
            search.TimeOut();
        }
        else
        {
            search.Complete();
        }

        await _store.FinishAsync(search.Id, search.Status, CancellationToken.None);
        _logger.LogInformation(
            "Search {SearchId} stored with status {Status} and {OutcomeCount} outcomes.",
            search.Id,
            search.Status,
            search.Outcomes.Count);
    }

    private static ShippingSearchUpdate.Finished CreateFinished(ShippingSearch search) => new(
        search.Id,
        search.Status,
        search.SelectedSupplierIds.Count,
        search.Outcomes.Count(outcome => outcome.Responded));

    private async Task WaitForSuppliersAsync(Guid searchId, List<Task<QuoteCompletion>> pending)
    {
        try
        {
            await Task.WhenAll(pending);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Supplier work ended with an error after search {SearchId}.", searchId);
        }
    }

    private int ElapsedMilliseconds(long startedTimestamp)
    {
        var elapsed = _time.GetElapsedTime(startedTimestamp).TotalMilliseconds;
        return (int)Math.Clamp(elapsed, 0, int.MaxValue);
    }

    private sealed record SearchRun(
        ShippingSearch Search,
        IReadOnlyList<Supplier> Suppliers,
        long StartedAt,
        CancellationToken CallerToken,
        CancellationTokenSource Timeout);

    private readonly record struct QuoteCompletion(Supplier Supplier, SupplierOutcome? Outcome);
}
