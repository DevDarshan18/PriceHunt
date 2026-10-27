namespace PriceHunt.Application.Common;

/// <summary>
/// One page of a larger result, with the numbers needed to page through it.
/// </summary>
/// <param name="Items">The items on this page.</param>
/// <param name="Page">The number of this page, starting at 1.</param>
/// <param name="PageSize">How many items a full page holds.</param>
/// <param name="TotalCount">How many items match in all, over every page.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>
    /// Converts every item and keeps the paging numbers.
    /// </summary>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> convert) =>
        new(Items.Select(convert).ToList(), Page, PageSize, TotalCount);
}
