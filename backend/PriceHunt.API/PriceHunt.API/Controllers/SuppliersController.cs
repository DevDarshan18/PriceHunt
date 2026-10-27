using Microsoft.AspNetCore.Mvc;
using PriceHunt.API.Contracts.Responses;
using PriceHunt.API.Mapping;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.API.Controllers;

/// <summary>
/// Serves the suppliers a search can ask.
/// </summary>
[ApiController]
[Route("api/suppliers")]
public sealed class SuppliersController : ControllerBase
{
    private readonly SupplierCatalog _catalog;
    private readonly ApiMapper _mapper;

    public SuppliersController(SupplierCatalog catalog, ApiMapper mapper)
    {
        _catalog = catalog;
        _mapper = mapper;
    }

    /// <summary>
    /// Returns the suppliers that a search can ask.
    /// </summary>
    [HttpGet]
    public IReadOnlyList<SupplierDto> GetAll() => _mapper.ToDtos(_catalog.All);
}
