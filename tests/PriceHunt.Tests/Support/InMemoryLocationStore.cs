using PriceHunt.Application.Common;
using PriceHunt.Application.Locations.Interfaces;

namespace PriceHunt.Tests.Support;

public sealed class InMemoryLocationStore : ILocationStore
{
    private readonly IReadOnlyList<string> _names;

    public InMemoryLocationStore(IEnumerable<string> names)
    {
        _names = names.OrderBy(name => name).ToList();
    }

    public Task<PagedResult<string>> FindAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var matches = string.IsNullOrEmpty(search)
            ? _names
            : _names.Where(name => name.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        var items = matches.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PagedResult<string>(items, page, pageSize, matches.Count));
    }

    public Task<IReadOnlyDictionary<string, string>> GetCanonicalNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var known = _names.ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);
        var found = names.Where(known.ContainsKey).ToDictionary(name => name, name => known[name], StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IReadOnlyDictionary<string, string>>(found);
    }
}
