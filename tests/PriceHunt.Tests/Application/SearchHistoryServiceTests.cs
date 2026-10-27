using FluentValidation;
using PriceHunt.Application.Common;
using PriceHunt.Application.History;
using PriceHunt.Application.History.Interfaces;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.History.Validators;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Application;

public class SearchHistoryServiceTests
{
    private readonly EmptyReader _reader = new();
    private readonly SearchHistoryService _service;

    public SearchHistoryServiceTests()
    {
        var locations = TestLocations.Service();
        _service = new SearchHistoryService(_reader, new HistoryQueryValidator(locations, TestSuppliers.Catalog()), locations);
    }

    [Fact]
    public async Task A_valid_query_reaches_the_reader()
    {
        var result = await _service.GetAsync(Query(), CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task An_end_date_before_the_start_date_is_rejected()
    {
        var query = Query() with { From = new DateOnly(2024, 5, 10), To = new DateOnly(2024, 5, 9) };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetAsync(query, CancellationToken.None));

        Assert.Contains("to", exception.ErrorsByField().Keys);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Page_and_page_size_must_be_in_range(int page, int pageSize)
    {
        var query = Query() with { Page = page, PageSize = pageSize };

        await Assert.ThrowsAsync<ValidationException>(() => _service.GetAsync(query, CancellationToken.None));
    }

    [Fact]
    public async Task Unknown_suppliers_are_rejected()
    {
        var query = Query() with { SupplierIds = new[] { Guid.NewGuid() } };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetAsync(query, CancellationToken.None));

        Assert.Contains("supplierIds", exception.ErrorsByField().Keys);
    }

    [Fact]
    public async Task A_place_that_is_not_in_the_list_is_rejected()
    {
        var query = Query() with { Origin = "Atlantis", Destination = "Hamburgg" };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.GetAsync(query, CancellationToken.None));

        Assert.Equal(new[] { "destination", "origin" }, exception.ErrorsByField().Keys.Order().ToArray());
    }

    [Fact]
    public async Task Places_reach_the_reader_under_their_proper_names()
    {
        await _service.GetAsync(Query() with { Origin = "  hamburg ", Destination = "ROTTERDAM" }, CancellationToken.None);

        Assert.Equal(("Hamburg", "Rotterdam"), (_reader.LastQuery!.Origin, _reader.LastQuery.Destination));
    }

    [Fact]
    public async Task A_blank_place_means_no_filter()
    {
        await _service.GetAsync(Query() with { Origin = "  ", Destination = "" }, CancellationToken.None);

        Assert.Null(_reader.LastQuery!.Origin);
        Assert.Null(_reader.LastQuery.Destination);
    }

    private static HistoryQuery Query() =>
        new(null, null, Array.Empty<Guid>(), null, null, HistorySortField.Date, true, 1, 20);

    private sealed class EmptyReader : ISearchHistoryReader
    {
        public HistoryQuery? LastQuery { get; private set; }

        public Task<PagedResult<HistoryItem>> QueryAsync(HistoryQuery query, CancellationToken cancellationToken)
        {
            LastQuery = query;
            return Task.FromResult(new PagedResult<HistoryItem>(Array.Empty<HistoryItem>(), query.Page, query.PageSize, 0));
        }
    }
}
