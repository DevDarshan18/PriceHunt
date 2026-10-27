using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Searches.Models;

/// <summary>
/// What a supplier client returns when asked for a price.
/// </summary>
public abstract record SupplierQuote
{
    /// <summary>
    /// The supplier offered this price.
    /// </summary>
    /// <param name="Amount">The price offered.</param>
    /// <param name="DurationMilliseconds">How long the supplier took to answer.</param>
    public sealed record Price(Money Amount, int DurationMilliseconds) : SupplierQuote;

    /// <summary>
    /// The supplier answered with an error.
    /// </summary>
    /// <param name="ErrorCode">Why the supplier failed.</param>
    /// <param name="DurationMilliseconds">How long the supplier took to answer.</param>
    public sealed record Failure(string ErrorCode, int DurationMilliseconds) : SupplierQuote;

    private SupplierQuote()
    {
    }
}
