namespace PriceHunt.Infrastructure.Locations;

/// <summary>
/// The places the pickers offer. They are copied into the database at startup.
/// </summary>
public static class KnownLocations
{
    /// <summary>
    /// Every known place.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        "Amsterdam",
        "Antwerp",
        "Barcelona",
        "Bergen",
        "Berlin",
        "Bremerhaven",
        "Busan",
        "Cape Town",
        "Copenhagen",
        "Dubai",
        "Dublin",
        "Felixstowe",
        "Gdansk",
        "Genoa",
        "Gothenburg",
        "Hamburg",
        "Helsinki",
        "Hong Kong",
        "Houston",
        "Istanbul",
        "Le Havre",
        "Lisbon",
        "London",
        "Los Angeles",
        "Madrid",
        "Marseille",
        "Milan",
        "Mumbai",
        "Munich",
        "New York",
        "Oslo",
        "Paris",
        "Piraeus",
        "Prague",
        "Rome",
        "Rotterdam",
        "Santos",
        "Savannah",
        "Shanghai",
        "Singapore",
        "Southampton",
        "Stockholm",
        "Sydney",
        "Tokyo",
        "Valencia",
        "Vancouver",
        "Vienna",
        "Warsaw"
    };
}
