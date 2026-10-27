using FluentValidation;
using PriceHunt.Application.Locations.Models;
using PriceHunt.Application.Validation;

namespace PriceHunt.Application.Locations.Validators;

/// <summary>
/// Rules for looking up places.
/// </summary>
public sealed class LocationSearchQueryValidator : AbstractValidator<LocationSearchQuery>
{
    /// <summary>
    /// The most places one page can hold.
    /// </summary>
    public const int MaximumPageSize = 50;

    /// <summary>
    /// The longest search text accepted.
    /// </summary>
    public const int MaximumSearchLength = 100;

    public LocationSearchQueryValidator()
    {
        RuleFor(query => query.Search)
            .MaximumLength(MaximumSearchLength).WithMessage($"Search can't be longer than {MaximumSearchLength} characters.");

        RuleFor(query => query.Page).IsPageNumber();
        RuleFor(query => query.PageSize).IsPageSize(MaximumPageSize);
    }
}
