using Microsoft.EntityFrameworkCore;
using PriceHunt.Infrastructure.DI;
using PriceHunt.Infrastructure.Persistence.Entities;
using PriceHunt.Infrastructure.Persistence.Repositories;

namespace PriceHunt.Tests.Infrastructure;

public sealed class LocationStoreTests : DatabaseTestBase
{
    private LocationStore NewLocations() => new(NewContext());

    [Fact]
    public async Task The_first_page_has_ten_places_and_the_next_page_continues_after_them()
    {
        var first = await NewLocations().FindAsync(null, 1, 10, CancellationToken.None);
        var second = await NewLocations().FindAsync(null, 2, 10, CancellationToken.None);

        Assert.Equal(10, first.Items.Count);
        Assert.Empty(first.Items.Intersect(second.Items));
        Assert.True(string.Compare(first.Items[^1], second.Items[0], StringComparison.OrdinalIgnoreCase) < 0);
        Assert.True(first.TotalCount > 20);
    }

    [Fact]
    public async Task The_last_page_holds_only_what_is_left()
    {
        var all = await NewLocations().FindAsync(null, 1, 50, CancellationToken.None);

        var last = await NewLocations().FindAsync(null, all.TotalCount / 10 + 1, 10, CancellationToken.None);

        Assert.Equal(all.TotalCount % 10, last.Items.Count);
    }

    [Fact]
    public async Task A_search_ignores_case_and_lists_names_that_start_with_it_first()
    {
        await AddAsync("Port Bergen", "Bergamo");

        var result = await NewLocations().FindAsync("BERG", 1, 10, CancellationToken.None);

        Assert.Equal(new[] { "Bergamo", "Bergen", "Port Bergen" }, result.Items);
    }

    [Fact]
    public async Task Wildcard_characters_in_a_search_are_taken_literally()
    {
        var result = await NewLocations().FindAsync("%", 1, 10, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task Names_are_matched_without_regard_to_case_and_return_the_stored_spelling()
    {
        var names = await NewLocations().GetCanonicalNamesAsync(new[] { "hamburg", "ROTTERDAM", "Atlantis" }, CancellationToken.None);

        Assert.Equal("Hamburg", names["hamburg"]);
        Assert.Equal("Rotterdam", names["ROTTERDAM"]);
        Assert.DoesNotContain("Atlantis", names.Keys);
    }

    [Fact]
    public async Task Starting_the_app_again_does_not_duplicate_the_places()
    {
        var before = await NewContext().Locations.CountAsync();

        await Services.InitializePriceHuntDatabaseAsync();

        Assert.Equal(before, await NewContext().Locations.CountAsync());
    }

    private async Task AddAsync(params string[] names)
    {
        var context = NewContext();
        context.Locations.AddRange(names.Select(name => new LocationRecord { Name = name }));
        await context.SaveChangesAsync();
    }
}
