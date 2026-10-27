using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Domain.Searches;

/// <summary>
/// What one supplier did in one search: a price, a failure, no answer in time, or a cancellation.
/// </summary>
public sealed class SupplierOutcome
{
    private SupplierOutcome(Guid supplierId, SupplierOutcomeStatus status, int responseDurationMilliseconds)
    {
        SupplierId = supplierId;
        Status = status;
        ResponseDurationMilliseconds = responseDurationMilliseconds;
    }

    /// <summary>
    /// The supplier this outcome belongs to.
    /// </summary>
    public Guid SupplierId { get; }

    /// <summary>
    /// How the supplier's attempt ended.
    /// </summary>
    public SupplierOutcomeStatus Status { get; }

    /// <summary>
    /// How long the supplier took, or how long the search waited for it.
    /// </summary>
    public int ResponseDurationMilliseconds { get; }

    /// <summary>
    /// The price offered. Only set when the supplier answered with one.
    /// </summary>
    public Money? Price { get; private init; }

    /// <summary>
    /// When the supplier answered, in UTC. Not set when it never answered.
    /// </summary>
    public DateTime? RespondedAtUtc { get; private init; }

    /// <summary>
    /// Why the supplier failed. Only set when it answered with an error.
    /// </summary>
    public string? ErrorCode { get; private init; }

    /// <summary>
    /// True when the supplier answered, whether with a price or an error.
    /// </summary>
    public bool Responded => Status is SupplierOutcomeStatus.Succeeded or SupplierOutcomeStatus.Failed;

    /// <summary>
    /// The supplier answered with a price.
    /// </summary>
    /// <param name="supplierId">The supplier that answered.</param>
    /// <param name="price">The price offered.</param>
    /// <param name="durationMilliseconds">How long the supplier took.</param>
    /// <param name="respondedAtUtc">When the supplier answered, in UTC.</param>
    /// <returns>The outcome.</returns>
    public static SupplierOutcome Succeeded(Guid supplierId, Money price, int durationMilliseconds, DateTime respondedAtUtc) =>
        new(supplierId, SupplierOutcomeStatus.Succeeded, durationMilliseconds)
        {
            Price = price,
            RespondedAtUtc = respondedAtUtc
        };

    /// <summary>
    /// The supplier answered with an error.
    /// </summary>
    /// <param name="supplierId">The supplier that answered.</param>
    /// <param name="durationMilliseconds">How long the supplier took.</param>
    /// <param name="errorCode">Why the supplier failed.</param>
    /// <param name="respondedAtUtc">When the supplier answered, in UTC.</param>
    /// <returns>The outcome.</returns>
    public static SupplierOutcome Failed(Guid supplierId, int durationMilliseconds, string errorCode, DateTime respondedAtUtc) =>
        new(supplierId, SupplierOutcomeStatus.Failed, durationMilliseconds)
        {
            ErrorCode = errorCode,
            RespondedAtUtc = respondedAtUtc
        };

    /// <summary>
    /// The supplier had not answered when the search ran out of time.
    /// </summary>
    /// <param name="supplierId">The supplier that did not answer.</param>
    /// <param name="waitedMilliseconds">How long the search waited for it.</param>
    /// <returns>The outcome.</returns>
    public static SupplierOutcome TimedOut(Guid supplierId, int waitedMilliseconds) =>
        new(supplierId, SupplierOutcomeStatus.TimedOut, waitedMilliseconds);

    /// <summary>
    /// The supplier had not answered when the caller cancelled.
    /// </summary>
    /// <param name="supplierId">The supplier that did not answer.</param>
    /// <param name="waitedMilliseconds">How long the search waited for it.</param>
    /// <returns>The outcome.</returns>
    public static SupplierOutcome Cancelled(Guid supplierId, int waitedMilliseconds) =>
        new(supplierId, SupplierOutcomeStatus.Cancelled, waitedMilliseconds);
}
