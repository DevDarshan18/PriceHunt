using PriceHunt.Application.Locations;
using PriceHunt.Application.Locations.Validators;
using PriceHunt.Infrastructure.Locations;

namespace PriceHunt.Tests.Support;

public static class TestLocations
{
    public static LocationService Service() => new(new InMemoryLocationStore(KnownLocations.All), new LocationSearchQueryValidator());
}
