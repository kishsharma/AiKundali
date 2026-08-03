namespace HoraLife.Store.Application.Categories.Dtos;

public sealed record CategoryDto(
    Guid Id,
    Guid? ParentId,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    int ProductCount);
