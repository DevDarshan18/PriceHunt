namespace PriceHunt.Domain.Searches;

/// <summary>
/// Where a search stands: running, or how it ended.
/// </summary>
public enum SearchStatus
{
    Running,
    Completed,
    TimedOut,
    Cancelled
}
