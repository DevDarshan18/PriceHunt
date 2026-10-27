using FluentValidation;
using PriceHunt.Application.Common;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Locations.Models;

namespace PriceHunt.Application.Locations;

/// <summary>
/// Looks up places for the location pickers and finds the stored spelling of a place name.
/// </summary>
public sealed class LocationService : ILocationService
{
    private readonly ILocationStore _store;
    private readonly IValidator<LocationSearchQuery> _validator;

    public LocationService(ILocationStore store, IValidator<LocationSearchQuery> validator)
    {
        _store = store;
        _validator = validator;
    }

    /// <summary>
    /// Returns one page of place names that match the text typed so far.
    /// </summary>
    /// <param name="query">The text typed so far and the page wanted.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching places.</returns>
    public async Task<PagedResult<string>> SearchAsync(LocationSearchQuery query, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        return await _store.FindAsync(query.Search?.Trim(), query.Page, query.PageSize, cancellationToken);
    }

    /// <summary>
    /// Returns the stored spelling of a place name, or null when the place is not known.
    /// </summary>
    /// <param name="name">The place name as typed. It may be blank.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The stored spelling, or null when the name is blank or not known.</returns>
    public async Task<string?> FindCanonicalAsync(string? name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        var canonical = await _store.GetCanonicalNamesAsync(new[] { trimmed }, cancellationToken);

        return canonical.GetValueOrDefault(trimmed);
    }
}
