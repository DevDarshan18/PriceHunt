using FluentValidation;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Suppliers;
using PriceHunt.Application.Validation;

namespace PriceHunt.Application.History.Validators;

/// <summary>
/// Rules for the history filters, field by field.
/// </summary>
public sealed class HistoryQueryValidator : AbstractValidator<HistoryQuery>
{
    /// <summary>
    /// The most rows one page of history can hold.
    /// </summary>
    public const int MaximumPageSize = 100;

    public HistoryQueryValidator(ILocationService locations, SupplierCatalog suppliers)
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(query => query.To)
            .GreaterThanOrEqualTo(query => query.From).WithMessage("To date can't be before the from date.")
            .When(query => query.From.HasValue && query.To.HasValue);

        RuleFor(query => query.SupplierIds).AreKnownSuppliers(suppliers);

        RuleFor(query => query.Origin!).IsKnownLocation(locations).When(query => !string.IsNullOrWhiteSpace(query.Origin));
        RuleFor(query => query.Destination!).IsKnownLocation(locations).When(query => !string.IsNullOrWhiteSpace(query.Destination));

        RuleFor(query => query.Page).IsPageNumber();
        RuleFor(query => query.PageSize).IsPageSize(MaximumPageSize);
    }
}
