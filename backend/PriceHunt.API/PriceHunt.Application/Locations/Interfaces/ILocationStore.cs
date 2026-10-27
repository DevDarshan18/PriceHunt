using PriceHunt.Application.Common;

namespace PriceHunt.Application.Locations.Interfaces;

/// <summary>
/// Reads places from storage.
/// </summary>
public interface ILocationStore
{
    /// <summary>
    /// Returns one page of place names, those starting with the text first.
    /// </summary>
    /// <param name="search">The text typed so far. Empty returns every place.</param>
    /// <param name="page">The page wanted, starting at 1.</param>
    /// <param name="pageSize">How many places a page holds.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching places.</returns>
    Task<PagedResult<string>> FindAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the stored spelling of each given name that exists, keyed by the name as given.
    /// </summary>
    /// <param name="names">The names to look up.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The stored spelling of each name that exists, keyed by the name as given.</returns>
    Task<IReadOnlyDictionary<string, string>> GetCanonicalNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken);
}
