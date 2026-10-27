using FluentValidation;
using PriceHunt.Application.Locations.Interfaces;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Application.Validation;

/// <summary>
/// Rules that more than one validator uses.
/// </summary>
public static class ValidationRules
{
    public static IRuleBuilderOptions<T, int> IsPageNumber<T>(this IRuleBuilder<T, int> rule)
    {
        return rule.GreaterThanOrEqualTo(1).WithMessage("Page numbers start at 1.");
    }

    public static IRuleBuilderOptions<T, int> IsPageSize<T>(this IRuleBuilder<T, int> rule, int maximum)
    {
        return rule.InclusiveBetween(1, maximum).WithMessage($"Page size must be 1 to {maximum}.");
    }

    public static IRuleBuilderOptions<T, string> IsKnownLocation<T>(this IRuleBuilder<T, string> rule, ILocationService locations)
    {
        return rule
            .MustAsync(async (name, cancellationToken) => await locations.FindCanonicalAsync(name, cancellationToken) is not null)
            .WithMessage("Choose a place from the list.");
    }

    public static IRuleBuilderOptions<T, IReadOnlyList<Guid>> AreKnownSuppliers<T>(this IRuleBuilder<T, IReadOnlyList<Guid>> rule, SupplierCatalog catalog)
    {
        return rule.Must(ids => ids.All(catalog.Contains)).WithMessage("Unknown supplier selected.");
    }
}
