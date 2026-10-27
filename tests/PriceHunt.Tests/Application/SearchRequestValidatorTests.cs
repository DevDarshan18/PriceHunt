using FluentValidation;
using Microsoft.Extensions.Time.Testing;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Searches.Validators;
using PriceHunt.Infrastructure.Simulation;
using PriceHunt.Tests.Support;

namespace PriceHunt.Tests.Application;

public class SearchRequestValidatorTests
{
    private static readonly DateOnly Today = new(2024, 5, 6);

    private readonly SearchRequestValidator _validator = new(
        TestLocations.Service(),
        TestSuppliers.Catalog(),
        new FakeTimeProvider(new DateTimeOffset(2024, 5, 6, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public async Task A_valid_request_passes()
    {
        await ValidateAsync(Request());
    }

    [Fact]
    public async Task Starting_today_or_yesterday_is_allowed_for_users_in_other_time_zones()
    {
        await ValidateAsync(Request(from: Today));
        await ValidateAsync(Request(from: Today.AddDays(-1), to: Today));
    }

    [Fact]
    public async Task A_from_date_in_the_past_is_rejected()
    {
        var errors = await ErrorsForAsync(Request(from: Today.AddDays(-2), to: Today.AddDays(3)));

        Assert.Equal("From date can't be in the past.", Assert.Single(errors["fromDate"]));
    }

    [Fact]
    public async Task A_to_date_before_the_from_date_is_rejected()
    {
        var errors = await ErrorsForAsync(Request(from: Today.AddDays(5), to: Today.AddDays(4)));

        Assert.Equal("To date can't be before the from date.", Assert.Single(errors["toDate"]));
    }

    [Fact]
    public async Task A_range_of_ninety_days_is_allowed_but_ninety_one_is_not()
    {
        await ValidateAsync(Request(from: Today, to: Today.AddDays(90)));

        var errors = await ErrorsForAsync(Request(from: Today, to: Today.AddDays(91)));

        Assert.Contains("90 days", Assert.Single(errors["toDate"]));
    }

    [Theory]
    [InlineData("", "Hamburg", "fromLocation", "Origin is required.")]
    [InlineData("Rotterdam", "  ", "toLocation", "Destination is required.")]
    public async Task Both_locations_are_required(string from, string to, string field, string message)
    {
        var errors = await ErrorsForAsync(Request(fromLocation: from, toLocation: to));

        Assert.Equal(message, Assert.Single(errors[field]));
    }

    [Theory]
    [InlineData("Atlantis", "Hamburg", "fromLocation")]
    [InlineData("Rotterdam", "Hamburgg", "toLocation")]
    public async Task A_place_that_is_not_in_the_list_is_rejected(string from, string to, string field)
    {
        var errors = await ErrorsForAsync(Request(fromLocation: from, toLocation: to));

        Assert.Equal("Choose a place from the list.", Assert.Single(errors[field]));
    }

    [Theory]
    [InlineData("hamburg", "ROTTERDAM")]
    [InlineData("  Hamburg  ", "rotterdam ")]
    public async Task Places_are_matched_ignoring_case_and_spaces(string from, string to)
    {
        await ValidateAsync(Request(fromLocation: from, toLocation: to));
    }

    [Fact]
    public async Task The_destination_must_differ_from_the_origin_ignoring_case()
    {
        var errors = await ErrorsForAsync(Request(fromLocation: "Oslo", toLocation: " oslo "));

        Assert.Contains("toLocation", errors.Keys);
    }

    [Fact]
    public async Task An_explicit_supplier_list_cannot_be_empty_duplicated_or_unknown()
    {
        Assert.Contains("supplierIds", (await ErrorsForAsync(Request(suppliers: Array.Empty<Guid>()))).Keys);
        Assert.Contains(
            "supplierIds",
            (await ErrorsForAsync(Request(suppliers: new[] { SimulatedSuppliers.AtlasFreightId, SimulatedSuppliers.AtlasFreightId }))).Keys);
        Assert.Contains("supplierIds", (await ErrorsForAsync(Request(suppliers: new[] { Guid.NewGuid() }))).Keys);
    }

    [Fact]
    public async Task Every_problem_is_reported_at_once()
    {
        var errors = await ErrorsForAsync(Request(fromLocation: "", from: Today.AddDays(-9), to: Today.AddDays(-10), suppliers: Array.Empty<Guid>()));

        Assert.Equal(new[] { "fromDate", "fromLocation", "supplierIds", "toDate" }, errors.Keys.Order().ToArray());
    }

    private async Task<IReadOnlyDictionary<string, string[]>> ErrorsForAsync(ShippingSearchRequest request)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => ValidateAsync(request));
        return exception.ErrorsByField();
    }

    private Task ValidateAsync(ShippingSearchRequest request) => _validator.ValidateAndThrowAsync(request);

    private static ShippingSearchRequest Request(
        string fromLocation = "Rotterdam",
        string toLocation = "Hamburg",
        DateOnly? from = null,
        DateOnly? to = null,
        IReadOnlyList<Guid>? suppliers = null) =>
        new(fromLocation, toLocation, from ?? Today.AddDays(1), to ?? Today.AddDays(8), suppliers);
}
