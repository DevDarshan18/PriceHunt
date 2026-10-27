using PriceHunt.API.Contracts.Requests;
using PriceHunt.API.Mapping;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Domain.Searches;
using PriceHunt.Domain.Suppliers;
using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Tests.Api;

public class ApiMapperTests
{
    private static readonly Supplier Atlas = new(Guid.NewGuid(), "Atlas Freight");
    private static readonly DateTime Now = new(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);

    private readonly ApiMapper _mapper = new();

    [Fact]
    public void A_search_request_keeps_a_missing_supplier_list_as_null()
    {
        var dto = new SearchRequestDto("Oslo", "Bergen", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 5), null);

        var request = _mapper.ToSearchRequest(dto);

        Assert.Null(request.SupplierIds);
        Assert.Equal("Oslo", request.FromLocation);
        Assert.Equal(new DateOnly(2024, 6, 5), request.ToDate);
    }

    [Fact]
    public void A_search_request_keeps_the_selected_suppliers()
    {
        var ids = new[] { Atlas.Id };

        var request = _mapper.ToSearchRequest(new SearchRequestDto("Oslo", "Bergen", new DateOnly(2024, 6, 1), new DateOnly(2024, 6, 5), ids));

        Assert.Equal(ids, request.SupplierIds);
    }

    [Fact]
    public void A_history_request_becomes_a_query_with_every_filter()
    {
        var supplierId = Guid.NewGuid();
        var dto = new HistoryRequestDto
        {
            From = new DateOnly(2024, 1, 1),
            To = new DateOnly(2024, 1, 31),
            SupplierIds = new List<Guid> { supplierId },
            Origin = "Oslo",
            Destination = "Bergen",
            SortBy = HistorySortField.Price,
            Descending = false,
            Page = 3,
            PageSize = 50
        };

        var query = _mapper.ToHistoryQuery(dto);

        Assert.Equal(new DateOnly(2024, 1, 31), query.To);
        Assert.Equal(new[] { supplierId }, query.SupplierIds);
        Assert.Equal((HistorySortField.Price, false, 3, 50), (query.SortBy, query.Descending, query.Page, query.PageSize));
        Assert.Equal(("Oslo", "Bergen"), (query.Origin, query.Destination));
    }

    [Fact]
    public void A_supplier_response_with_a_price_becomes_a_supplier_event()
    {
        var update = new ShippingSearchUpdate.SupplierResponded(
            Guid.NewGuid(),
            Atlas,
            SupplierOutcome.Succeeded(Atlas.Id, new Money(125.5m, "usd"), 900, Now));

        var result = _mapper.ToEvent(update);

        Assert.Equal(update.SearchId, result.SearchId);
        Assert.Equal((Atlas.Id, "Atlas Freight"), (result.SupplierId, result.SupplierName));
        Assert.Equal("Succeeded", result.Status);
        Assert.Equal((125.5m, "USD"), (result.Price, result.Currency));
        Assert.Equal(900, result.ResponseTimeMilliseconds);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void A_failed_supplier_has_no_price_but_keeps_its_error_code()
    {
        var update = new ShippingSearchUpdate.SupplierResponded(
            Guid.NewGuid(),
            Atlas,
            SupplierOutcome.Failed(Atlas.Id, 400, "SUPPLIER_FAILURE", Now));

        var result = _mapper.ToEvent(update);

        Assert.Equal("Failed", result.Status);
        Assert.Null(result.Price);
        Assert.Null(result.Currency);
        Assert.Equal("SUPPLIER_FAILURE", result.ErrorCode);
    }

    [Fact]
    public void The_started_and_finished_updates_map_to_events()
    {
        var searchId = Guid.NewGuid();

        var started = _mapper.ToEvent(new ShippingSearchUpdate.Started(searchId, Now, new[] { Atlas }));
        var finished = _mapper.ToEvent(new ShippingSearchUpdate.Finished(searchId, SearchStatus.TimedOut, 7, 6));

        Assert.Equal(Now, started.CreatedAtUtc);
        Assert.Equal("Atlas Freight", Assert.Single(started.Suppliers).Name);
        Assert.Equal(("TimedOut", 7, 6), (finished.Status, finished.SelectedCount, finished.RespondedCount));
    }
}
