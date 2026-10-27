using FluentValidation;
using PriceHunt.Application.Common;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.Locations.Interfaces;

namespace PriceHunt.Application.History;

/// <summary>
/// Validates a history query and returns the matching page of past outcomes.
/// </summary>
public sealed class SearchHistoryService : ISearchHistoryService
{
    private readonly ISearchHistoryReader _reader;
    private readonly IValidator<HistoryQuery> _validator;
    private readonly ILocationService _locations;

    public SearchHistoryService(ISearchHistoryReader reader, IValidator<HistoryQuery> validator, ILocationService locations)
    {
        _reader = reader;
        _validator = validator;
        _locations = locations;
    }

    /// <summary>
    /// Validates the query and returns the matching page of past outcomes.
    /// </summary>
    /// <param name="query">The filters, sort order and paging.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching outcomes.</returns>
    public async Task<PagedResult<HistoryItem>> GetAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        var origin = await _locations.FindCanonicalAsync(query.Origin, cancellationToken);
        var destination = await _locations.FindCanonicalAsync(query.Destination, cancellationToken);

        return await _reader.QueryAsync(query with { Origin = origin, Destination = destination }, cancellationToken);
    }
}
