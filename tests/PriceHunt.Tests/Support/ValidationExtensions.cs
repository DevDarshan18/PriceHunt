using System.Text.Json;
using FluentValidation;

namespace PriceHunt.Tests.Support;

public static class ValidationExtensions
{
    public static IReadOnlyDictionary<string, string[]> ErrorsByField(this ValidationException exception) => exception.Errors
        .GroupBy(failure => JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName))
        .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());
}
