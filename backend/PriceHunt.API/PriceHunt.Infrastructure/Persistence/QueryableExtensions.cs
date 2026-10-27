using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Common;

namespace PriceHunt.Infrastructure.Persistence;

/// <summary>
/// Helpers for database queries.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Counts the matches and returns the requested page of them.
    /// </summary>
    /// <typeparam name="T">The type of item.</typeparam>
    /// <param name="query">The query to page, already filtered and sorted.</param>
    /// <param name="page">The page wanted, starting at 1.</param>
    /// <param name="pageSize">How many items a page holds.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The page and the total number of matches.</returns>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
