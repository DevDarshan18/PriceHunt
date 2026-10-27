using PriceHunt.Application.Common;
using PriceHunt.Application.Locations.Models;

namespace PriceHunt.Application.Locations.Interfaces;

/// <summary>
/// Offers the places a user can choose from.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Returns one page of place names that match the text typed so far.
    /// </summary>
    /// <param name="query">The text typed so far and the page wanted.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching places.</returns>
    Task<PagedResult<string>> SearchAsync(LocationSearchQuery query, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the stored spelling of a place name, or null when the place is not known.
    /// </summary>
    /// <param name="name">The place name as typed. It may be blank.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The stored spelling, or null when the name is blank or not known.</returns>
    Task<string?> FindCanonicalAsync(string? name, CancellationToken cancellationToken);
}
