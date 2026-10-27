using FluentValidation;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Application.Suppliers;
using PriceHunt.Application.Validation;

namespace PriceHunt.Application.Searches.Validators;

/// <summary>
/// Rules for a search request, field by field.
/// </summary>
public sealed class SearchRequestValidator : AbstractValidator<ShippingSearchRequest>
{
    /// <summary>
    /// The longest range between the from and to dates, in days.
    /// </summary>
    public const int MaximumRangeDays = 90;

    /// <summary>
    /// How many days in the past the from date may be, so that a user ahead of the server's time zone can still search for today.
    /// </summary>
    public const int TimeZoneToleranceDays = 1;

    private readonly TimeProvider _time;

    public SearchRequestValidator(ILocationService locations, SupplierCatalog suppliers, TimeProvider time)
    {
        _time = time;
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.FromLocation)
            .NotEmpty().WithMessage("Origin is required.")
            .IsKnownLocation(locations);

        RuleFor(request => request.ToLocation)
            .NotEmpty().WithMessage("Destination is required.")
            .IsKnownLocation(locations)
            .Must((request, to) => !IsSamePlace(request.FromLocation, to)).WithMessage("Destination can't be the same as the origin.");

        RuleFor(request => request.FromDate)
            .Must(date => date >= Today.AddDays(-TimeZoneToleranceDays)).WithMessage("From date can't be in the past.");

        RuleFor(request => request.ToDate)
            .GreaterThanOrEqualTo(request => request.FromDate).WithMessage("To date can't be before the from date.")
            .Must((request, to) => to.DayNumber - request.FromDate.DayNumber <= MaximumRangeDays)
            .WithMessage($"Date range can't be longer than {MaximumRangeDays} days.");

        When(request => request.SupplierIds is not null, () =>
        {
            RuleFor(request => request.SupplierIds!)
                .NotEmpty().WithMessage("Select at least one supplier.")
                .Must(ids => ids.Distinct().Count() == ids.Count).WithMessage("A supplier can only be picked once.")
                .AreKnownSuppliers(suppliers);
        });
    }

    private DateOnly Today => DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime);

    private static bool IsSamePlace(string? first, string? second)
    {
        return string.Equals(first?.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
