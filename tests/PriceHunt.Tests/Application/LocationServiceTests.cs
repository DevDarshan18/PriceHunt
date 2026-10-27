using FluentValidation;
using PriceHunt.Application.Locations;
using PriceHunt.Application.Locations.Models;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Application;

public class LocationServiceTests
{
    private readonly LocationService _service = TestLocations.Service();

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task Paging_outside_the_allowed_range_is_rejected(int page, int pageSize)
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.SearchAsync(new LocationSearchQuery(null, page, pageSize), CancellationToken.None));
    }

    [Fact]
    public async Task An_overlong_search_is_rejected()
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _service.SearchAsync(new LocationSearchQuery(new string('a', 101), 1, 10), CancellationToken.None));

        Assert.Contains("search", exception.ErrorsByField().Keys);
    }

    [Fact]
    public async Task A_name_is_resolved_to_its_proper_spelling()
    {
        var name = await _service.FindCanonicalAsync("  hamburg ", CancellationToken.None);

        Assert.Equal("Hamburg", name);
    }

    [Theory]
    [InlineData("Atlantis")]
    [InlineData("  ")]
    [InlineData(null)]
    public async Task Unknown_and_blank_names_resolve_to_nothing(string? name)
    {
        var resolved = await _service.FindCanonicalAsync(name, CancellationToken.None);

        Assert.Null(resolved);
    }
}
