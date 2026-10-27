namespace PriceHunt.Application.Locations.Models;

/// <summary>
/// The text typed so far and the page of places wanted.
/// </summary>
/// <param name="Search">The text typed so far. Empty returns every place.</param>
/// <param name="Page">The page wanted, starting at 1.</param>
/// <param name="PageSize">How many places a page holds.</param>
public sealed record LocationSearchQuery(string? Search, int Page, int PageSize);
