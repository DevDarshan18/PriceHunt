using PriceHunt.API.Contracts.Responses;

namespace PriceHunt.API.Contracts.Events;

/// <summary>
/// Sent first, when a search has started.
/// </summary>
/// <param name="SearchId">The search that started.</param>
/// <param name="CreatedAtUtc">When the search started, in UTC.</param>
/// <param name="Suppliers">The suppliers being asked.</param>
public sealed record SearchStartedEvent(Guid SearchId, DateTime CreatedAtUtc, IReadOnlyList<SupplierDto> Suppliers);
