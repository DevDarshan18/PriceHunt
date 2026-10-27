using PriceHunt.Domain.Searches;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Domain;

public class ShippingSearchTests
{
    private static readonly Guid FirstSupplier = Guid.NewGuid();
    private static readonly Guid SecondSupplier = Guid.NewGuid();
    private static readonly DateTime Now = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    [Fact]
    public void A_new_search_is_running_with_no_outcomes()
    {
        var search = StartSearch(FirstSupplier, SecondSupplier);

        Assert.Equal(SearchStatus.Running, search.Status);
        Assert.Empty(search.Outcomes);
        Assert.Equal(new[] { FirstSupplier, SecondSupplier }, search.SelectedSupplierIds);
        Assert.NotEqual(Guid.Empty, search.Id);
    }

    [Fact]
    public void At_least_one_supplier_is_required()
    {
        Assert.Throws<ArgumentException>(() => StartSearch());
    }

    [Fact]
    public void A_supplier_cannot_be_selected_twice()
    {
        Assert.Throws<ArgumentException>(() => StartSearch(FirstSupplier, FirstSupplier));
    }

    [Fact]
    public void Outcomes_are_kept_in_the_order_they_are_recorded()
    {
        var search = StartSearch(FirstSupplier, SecondSupplier);

        search.RecordOutcome(Price(SecondSupplier));
        search.RecordOutcome(Price(FirstSupplier));

        Assert.Equal(new[] { SecondSupplier, FirstSupplier }, search.Outcomes.Select(outcome => outcome.SupplierId));
    }

    [Fact]
    public void A_supplier_can_only_have_one_outcome()
    {
        var search = StartSearch(FirstSupplier);
        search.RecordOutcome(Price(FirstSupplier));

        Assert.Throws<InvalidOperationException>(() => search.RecordOutcome(Price(FirstSupplier)));
    }

    [Theory]
    [InlineData(SearchStatus.Completed)]
    [InlineData(SearchStatus.TimedOut)]
    [InlineData(SearchStatus.Cancelled)]
    public void A_search_can_finish_once(SearchStatus status)
    {
        var search = StartSearch(FirstSupplier);

        Finish(search, status);

        Assert.Equal(status, search.Status);
        Assert.Throws<InvalidOperationException>(search.Complete);
        Assert.Throws<InvalidOperationException>(search.Cancel);
    }

    [Fact]
    public void A_finished_search_does_not_accept_more_outcomes()
    {
        var search = StartSearch(FirstSupplier);
        search.Cancel();

        Assert.Throws<InvalidOperationException>(() => search.RecordOutcome(Price(FirstSupplier)));
    }

    private static ShippingSearch StartSearch(params Guid[] supplierIds) => ShippingSearch.Start(
        new ShippingRoute("Rotterdam", "Hamburg"),
        new SearchDateRange(new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 8)),
        supplierIds,
        Now);

    private static SupplierOutcome Price(Guid supplierId) =>
        SupplierOutcome.Succeeded(supplierId, new Money(10m, "USD"), 500, Now);

    private static void Finish(ShippingSearch search, SearchStatus status)
    {
        switch (status)
        {
            case SearchStatus.Completed:
                search.Complete();
                break;
            case SearchStatus.TimedOut:
                search.TimeOut();
                break;
            default:
                search.Cancel();
                break;
        }
    }
}
