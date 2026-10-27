using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Domain;

public class ValueObjectTests
{
    [Fact]
    public void A_route_trims_both_locations()
    {
        var route = new ShippingRoute("  Rotterdam ", " Hamburg");

        Assert.Equal("Rotterdam", route.Origin);
        Assert.Equal("Hamburg", route.Destination);
    }

    [Theory]
    [InlineData("", "Hamburg")]
    [InlineData("Rotterdam", "  ")]
    [InlineData("Oslo", " oslo ")]
    public void A_route_needs_two_different_locations(string origin, string destination)
    {
        Assert.Throws<ArgumentException>(() => new ShippingRoute(origin, destination));
    }

    [Fact]
    public void A_date_range_can_be_a_single_day()
    {
        var day = new DateOnly(2024, 6, 1);

        var range = new SearchDateRange(day, day);

        Assert.Equal(day, range.From);
        Assert.Equal(day, range.To);
    }

    [Fact]
    public void A_date_range_cannot_end_before_it_starts()
    {
        Assert.Throws<ArgumentException>(() => new SearchDateRange(new DateOnly(2024, 6, 2), new DateOnly(2024, 6, 1)));
    }
}
