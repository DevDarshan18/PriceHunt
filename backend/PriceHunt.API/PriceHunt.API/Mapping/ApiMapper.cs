using PriceHunt.API.Contracts.Events;
using PriceHunt.API.Contracts.Requests;
using PriceHunt.API.Contracts.Responses;
using PriceHunt.Application.History.Models;
using PriceHunt.Application.Locations.Models;
using PriceHunt.Application.Searches.Models;
using PriceHunt.Domain.Suppliers;
using Riok.Mapperly.Abstractions;

namespace PriceHunt.API.Mapping;

/// <summary>
/// Copies between the types the API sends and receives and the types of the application.
/// </summary>
[Mapper]
public partial class ApiMapper
{
    /// <summary>
    /// Turns the body of a search request into the application's request.
    /// </summary>
    /// <param name="request">The body of the search request.</param>
    /// <returns>The request in the form the application uses.</returns>
    public partial ShippingSearchRequest ToSearchRequest(SearchRequestDto request);

    /// <summary>
    /// Turns the query string of a history request into the application's query.
    /// </summary>
    /// <param name="request">The query string of the history request.</param>
    /// <returns>The query in the form the application uses.</returns>
    public partial HistoryQuery ToHistoryQuery(HistoryRequestDto request);

    /// <summary>
    /// Turns the query string of a locations request into the application's query.
    /// </summary>
    /// <param name="request">The query string of the locations request.</param>
    /// <returns>The query in the form the application uses.</returns>
    public partial LocationSearchQuery ToLocationQuery(LocationsRequestDto request);

    /// <summary>
    /// Turns a supplier into the form the API returns.
    /// </summary>
    /// <param name="supplier">The supplier to convert.</param>
    /// <returns>The supplier as the API returns it.</returns>
    public partial SupplierDto ToDto(Supplier supplier);

    /// <summary>
    /// Turns a list of suppliers into the form the API returns.
    /// </summary>
    /// <param name="suppliers">The suppliers to convert.</param>
    /// <returns>The suppliers as the API returns them.</returns>
    public partial List<SupplierDto> ToDtos(IReadOnlyList<Supplier> suppliers);

    /// <summary>
    /// Turns the start of a search into its event.
    /// </summary>
    /// <param name="update">The start of the search.</param>
    /// <returns>The event to send.</returns>
    public partial SearchStartedEvent ToEvent(ShippingSearchUpdate.Started update);

    /// <summary>
    /// Turns the end of a search into its event.
    /// </summary>
    /// <param name="update">The end of the search.</param>
    /// <returns>The event to send.</returns>
    public partial SearchFinishedEvent ToEvent(ShippingSearchUpdate.Finished update);

    /// <summary>
    /// Turns a supplier's answer into its event.
    /// </summary>
    /// <param name="update">The supplier's answer.</param>
    /// <returns>The event to send.</returns>
    [MapNestedProperties(nameof(ShippingSearchUpdate.SupplierResponded.Outcome))]
    [MapProperty("Outcome.Price.Amount", nameof(SupplierResultEvent.Price))]
    [MapProperty("Outcome.Price.Currency", nameof(SupplierResultEvent.Currency))]
    [MapProperty("Outcome.ResponseDurationMilliseconds", nameof(SupplierResultEvent.ResponseTimeMilliseconds))]
    public partial SupplierResultEvent ToEvent(ShippingSearchUpdate.SupplierResponded update);
}
