using HoraLife.Store.Application.Categories.Dtos;
using HoraLife.Store.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HoraLife.Api.Controllers.Store;

[ApiController]
[Route("api/store/categories")]
public sealed class StoreCategoriesController : ControllerBase
{
    private readonly IStoreCatalogService _catalogService;

    public StoreCategoriesController(
        IStoreCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<CategoryDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(
        CancellationToken cancellationToken)
    {
        var categories =
            await _catalogService.GetCategoriesAsync(cancellationToken);

        return Ok(categories);
    }
}
