using PriceHunt.Domain.Searches;

namespace PriceHunt.Application.Searches.Models;

/// <summary>
/// A search that has started: the search itself, the updates to stream out, and a task that completes once the result is stored.
/// </summary>
public sealed class ShippingSearchExecution
{
    internal ShippingSearchExecution(
        ShippingSearch search,
        IAsyncEnumerable<ShippingSearchUpdate> events,
        Task completed)
    {
        Search = search;
        Events = events;
        Completed = completed;
    }

    /// <summary>
    /// The search that was started.
    /// </summary>
    public ShippingSearch Search { get; }

    /// <summary>
    /// The updates of the search, in the order they happen.
    /// </summary>
    public IAsyncEnumerable<ShippingSearchUpdate> Events { get; }

    /// <summary>
    /// Completes once the search has ended and its result is stored.
    /// </summary>
    public Task Completed { get; }
}
