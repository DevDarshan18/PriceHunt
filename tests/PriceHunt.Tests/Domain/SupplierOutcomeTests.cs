using PriceHunt.Domain.Searches;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Domain;

public class SupplierOutcomeTests
{
    private static readonly Guid SupplierId = Guid.NewGuid();
    private static readonly DateTime Now = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    [Fact]
    public void A_price_counts_as_a_response()
    {
        var outcome = SupplierOutcome.Succeeded(SupplierId, new Money(10m, "USD"), 500, Now);

        Assert.True(outcome.Responded);
        Assert.Equal(new Money(10m, "USD"), outcome.Price);
        Assert.Equal(Now, outcome.RespondedAtUtc);
        Assert.Null(outcome.ErrorCode);
    }

    [Fact]
    public void A_failure_counts_as_a_response_without_a_price()
    {
        var outcome = SupplierOutcome.Failed(SupplierId, 500, "SUPPLIER_FAILURE", Now);

        Assert.True(outcome.Responded);
        Assert.Null(outcome.Price);
        Assert.Equal("SUPPLIER_FAILURE", outcome.ErrorCode);
    }

    [Fact]
    public void A_timed_out_supplier_has_no_price_error_or_response_time()
    {
        var outcome = SupplierOutcome.TimedOut(SupplierId, 6000);

        Assert.Equal(SupplierOutcomeStatus.TimedOut, outcome.Status);
        Assert.False(outcome.Responded);
        Assert.Null(outcome.Price);
        Assert.Null(outcome.ErrorCode);
        Assert.Null(outcome.RespondedAtUtc);
        Assert.Equal(6000, outcome.ResponseDurationMilliseconds);
    }

    [Fact]
    public void A_cancelled_supplier_is_not_a_response()
    {
        var outcome = SupplierOutcome.Cancelled(SupplierId, 1200);

        Assert.Equal(SupplierOutcomeStatus.Cancelled, outcome.Status);
        Assert.False(outcome.Responded);
    }
}
