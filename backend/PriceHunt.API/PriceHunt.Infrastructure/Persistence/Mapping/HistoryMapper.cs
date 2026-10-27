using PriceHunt.Application.History.Models;
using PriceHunt.Infrastructure.Persistence.Entities;
using Riok.Mapperly.Abstractions;

namespace PriceHunt.Infrastructure.Persistence.Mapping;

/// <summary>
/// Copies a stored outcome, together with its search, into a history item.
/// </summary>
[Mapper]
public sealed partial class HistoryMapper
{
    /// <summary>
    /// Converts one stored outcome. The price goes from cents to a decimal.
    /// </summary>
    [MapperIgnoreSource(nameof(SupplierOutcomeRecord.RespondedAtUtc))]
    [MapProperty("Search.CreatedAtUtc", nameof(HistoryItem.SearchedAtUtc))]
    [MapProperty("Search.Origin", nameof(HistoryItem.Origin))]
    [MapProperty("Search.Destination", nameof(HistoryItem.Destination))]
    [MapProperty(nameof(SupplierOutcomeRecord.PriceCents), nameof(HistoryItem.Price), Use = nameof(ToPrice))]
    [MapProperty(nameof(SupplierOutcomeRecord.ResponseDurationMilliseconds), nameof(HistoryItem.ResponseTimeMilliseconds))]
    public partial HistoryItem ToItem(SupplierOutcomeRecord record);

    private static decimal? ToPrice(long? cents) => cents / 100m;
}
