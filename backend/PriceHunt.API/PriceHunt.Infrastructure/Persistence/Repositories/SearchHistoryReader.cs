using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PriceHunt.Application.Common;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;
using PriceHunt.Infrastructure.Persistence.Entities;
using PriceHunt.Infrastructure.Persistence.Mapping;

namespace PriceHunt.Infrastructure.Persistence.Repositories;

/// <summary>
/// Reads past supplier outcomes from the database, filtered, sorted and paged.
/// </summary>
public sealed class SearchHistoryReader : ISearchHistoryReader
{
    private readonly PriceHuntDbContext _context;
    private readonly HistoryMapper _mapper;

    public SearchHistoryReader(PriceHuntDbContext context, HistoryMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns the page of outcomes that match the query.
    /// </summary>
    /// <param name="query">The filters, sort order and paging.</param>
    /// <param name="cancellationToken">Cancelled when the caller gives up.</param>
    /// <returns>The requested page and the total number of matching outcomes.</returns>
    public async Task<PagedResult<HistoryItem>> QueryAsync(HistoryQuery query, CancellationToken cancellationToken)
    {
        var filtered = Filter(_context.SupplierOutcomes.AsNoTracking(), query);

        var page = await Sort(filtered.Include(outcome => outcome.Search), query.SortBy, query.Descending)
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);

        return page.Map(_mapper.ToItem);
    }

    private static IQueryable<SupplierOutcomeRecord> Filter(IQueryable<SupplierOutcomeRecord> outcomes, HistoryQuery query)
    {
        if (query.From.HasValue)
        {
            var start = query.From.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            outcomes = outcomes.Where(outcome => outcome.Search.CreatedAtUtc >= start);
        }

        if (query.To.HasValue)
        {
            var endExclusive = query.To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            outcomes = outcomes.Where(outcome => outcome.Search.CreatedAtUtc < endExclusive);
        }

        if (query.SupplierIds.Count > 0)
        {
            outcomes = outcomes.Where(outcome => query.SupplierIds.Contains(outcome.SupplierId));
        }

        if (query.Origin != null)
        {
            outcomes = outcomes.Where(outcome => outcome.Search.Origin == query.Origin);
        }

        if (query.Destination != null)
        {
            outcomes = outcomes.Where(outcome => outcome.Search.Destination == query.Destination);
        }

        return outcomes;
    }

    private static IOrderedQueryable<SupplierOutcomeRecord> Sort(
        IQueryable<SupplierOutcomeRecord> outcomes,
        HistorySortField sortBy,
        bool descending)
    {
        Expression<Func<SupplierOutcomeRecord, object?>> column = sortBy switch
        {
            HistorySortField.Date => outcome => outcome.Search.CreatedAtUtc,
            HistorySortField.Route => outcome => outcome.Search.Origin + " to " + outcome.Search.Destination,
            HistorySortField.Supplier => outcome => outcome.SupplierName,
            HistorySortField.Status => outcome => outcome.Status,
            HistorySortField.Price => outcome => outcome.PriceCents ?? (descending ? -1 : long.MaxValue),
            HistorySortField.ResponseTime => outcome => outcome.ResponseDurationMilliseconds,
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported sort field.")
        };

        var sorted = descending ? outcomes.OrderByDescending(column) : outcomes.OrderBy(column);
        return sorted.ThenBy(outcome => outcome.SearchId).ThenBy(outcome => outcome.SupplierId);
    }
}
