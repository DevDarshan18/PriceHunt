namespace PriceHunt.API.Contracts.Events;

/// <summary>
/// Sent when a search ends.
/// </summary>
/// <param name="SearchId">The search that ended.</param>
/// <param name="Status">How the search ended: Completed, TimedOut or Cancelled.</param>
/// <param name="SelectedCount">How many suppliers were asked.</param>
/// <param name="RespondedCount">How many of them answered.</param>
public sealed record SearchFinishedEvent(Guid SearchId, string Status, int SelectedCount, int RespondedCount);
