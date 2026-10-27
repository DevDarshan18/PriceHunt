using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Common;
using PriceHunt.Application.Locations.Interfaces;

namespace PriceHunt.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads places from the Locations table.
/// </summary>
public sealed class LocationStore : ILocationStore
{
    private const string LikeEscape = "\\";

    private readonly PriceHuntDbContext _context;

    public LocationStore(PriceHuntDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns one page of place names, those starting with the text first.
    /// </summary>
    /// <param name="search">The text typed so far. Empty returns every place.</param>
    /// <param name="page">The page wanted, starting at 1.</param>
    /// <param name="pageSize">How many places a page holds.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching places.</returns>
    public async Task<PagedResult<string>> FindAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var locations = _context.Locations.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var text = EscapeLike(search);
            locations = locations
                .Where(location => EF.Functions.Like(location.Name, $"%{text}%", LikeEscape))
                .OrderBy(location => EF.Functions.Like(location.Name, $"{text}%", LikeEscape) ? 0 : 1)
                .ThenBy(location => location.Name);
        }
        else
        {
            locations = locations.OrderBy(location => location.Name);
        }

        return await locations
            .Select(location => location.Name)
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    /// <summary>
    /// Returns the stored spelling of each given name that exists, keyed by the name as given.
    /// </summary>
    /// <param name="names">The names to look up.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The stored spelling of each name that exists, keyed by the name as given.</returns>
    public async Task<IReadOnlyDictionary<string, string>> GetCanonicalNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken)
    {
        var found = await _context.Locations
            .AsNoTracking()
            .Where(location => names.Contains(location.Name))
            .Select(location => location.Name)
            .ToListAsync(cancellationToken);

        return found.ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);
    }

    private static string EscapeLike(string value) =>
        value.Replace(LikeEscape, LikeEscape + LikeEscape).Replace("%", LikeEscape + "%").Replace("_", LikeEscape + "_");
}
