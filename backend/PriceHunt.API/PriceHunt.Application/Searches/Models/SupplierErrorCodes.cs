namespace PriceHunt.Application.Searches.Models;

/// <summary>
/// Codes stored with a supplier outcome that failed.
/// </summary>
public static class SupplierErrorCodes
{
    /// <summary>
    /// The supplier answered with an error.
    /// </summary>
    public const string Failure = "SUPPLIER_FAILURE";

    /// <summary>
    /// Asking the supplier threw an exception.
    /// </summary>
    public const string Unexpected = "SUPPLIER_ERROR";
}
