using PriceHunt.Infrastructure.Persistence.Entities;
using PriceHunt.Infrastructure.Persistence.Mapping;

namespace PriceHunt.Tests.Infrastructure;

public class HistoryMapperTests
{
    private readonly HistoryMapper _mapper = new();

    [Fact]
    public void A_stored_outcome_becomes_a_history_row_with_the_search_details()
    {
        var record = Record(priceCents: 12_050);

        var item = _mapper.ToItem(record);

        Assert.Equal(record.SearchId, item.SearchId);
        Assert.Equal(new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc), item.SearchedAtUtc);
        Assert.Equal(("Rotterdam", "Hamburg"), (item.Origin, item.Destination));
        Assert.Equal(("Atlas Freight", "Succeeded", "USD"), (item.SupplierName, item.Status, item.Currency));
        Assert.Equal(900, item.ResponseTimeMilliseconds);
    }

    [Theory]
    [InlineData(12_050, 120.50)]
    [InlineData(5, 0.05)]
    public void Cents_become_a_decimal_price(long cents, double expected)
    {
        Assert.Equal((decimal)expected, _mapper.ToItem(Record(cents)).Price);
    }

    [Fact]
    public void A_missing_price_stays_missing()
    {
        Assert.Null(_mapper.ToItem(Record(priceCents: null)).Price);
    }

    private static SupplierOutcomeRecord Record(long? priceCents) => new()
    {
        SearchId = Guid.NewGuid(),
        SupplierId = Guid.NewGuid(),
        SupplierName = "Atlas Freight",
        Status = "Succeeded",
        PriceCents = priceCents,
        Currency = "USD",
        ResponseDurationMilliseconds = 900,
        Search = new SearchRecord
        {
            CreatedAtUtc = new DateTime(2024, 5, 1, 10, 0, 0, DateTimeKind.Utc),
            Origin = "Rotterdam",
            Destination = "Hamburg"
        }
    };
}
