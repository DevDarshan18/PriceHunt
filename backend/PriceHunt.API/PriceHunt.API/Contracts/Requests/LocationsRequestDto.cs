namespace PriceHunt.API.Contracts.Requests;

/// <summary>
/// Query string of the locations request.
/// </summary>
public sealed class LocationsRequestDto
{
    /// <summary>
    /// The text typed so far. Leave empty to list every place.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// The page wanted, starting at 1.
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// How many places a page holds. The default is 10 and the most is 50.
    /// </summary>
    public int PageSize { get; init; } = 10;
}
