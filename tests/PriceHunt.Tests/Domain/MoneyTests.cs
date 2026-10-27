using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Domain;

public class MoneyTests
{
    [Fact]
    public void Currency_is_upper_cased()
    {
        var money = new Money(10.5m, "usd");

        Assert.Equal(10.5m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Amount_is_rounded_to_two_decimals()
    {
        Assert.Equal(10.56m, new Money(10.555m, "USD").Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Amount_must_be_positive(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(amount, "USD"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("DOLLAR")]
    public void Currency_must_be_three_letters(string currency)
    {
        Assert.Throws<ArgumentException>(() => new Money(5m, currency));
    }

    [Fact]
    public void Equal_amounts_in_the_same_currency_are_equal()
    {
        Assert.Equal(new Money(5m, "usd"), new Money(5.00m, "USD"));
    }
}
