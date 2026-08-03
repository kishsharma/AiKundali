using KundaliEngine.API.DTOs.Store;
using HoraLife.Store.Application.Common;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Products.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HoraLife.Api.Controllers.Store;

[ApiController]
[Route("api/store/products")]
public sealed class StoreProductsController : ControllerBase
{
    private readonly IStoreCatalogService _catalogService;

    public StoreProductsController(
        IStoreCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResult<ProductListItemDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ProblemDetails),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetProducts(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.TryToFilter(
                out var filter,
                out var validationError))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid product search parameters",
                Detail = validationError,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var products =
            await _catalogService.GetProductsAsync(
                filter,
                cancellationToken);

        return Ok(products);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(
        typeof(ProductDetailDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDetailDto>> GetProductBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return NotFound();
        }

        var product =
            await _catalogService.GetProductBySlugAsync(
                slug,
                cancellationToken);

        return product is null
            ? NotFound()
            : Ok(product);
    }
}
