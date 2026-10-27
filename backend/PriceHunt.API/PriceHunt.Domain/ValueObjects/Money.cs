namespace PriceHunt.Domain.ValueObjects;

/// <summary>
/// An amount in a currency.
/// </summary>
public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        }

        if (currency?.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter code.", nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// The amount, greater than zero.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// The three letter currency code, for example USD.
    /// </summary>
    public string Currency { get; }
}
