namespace HoraLife.Store.Application.Inventory.Dtos;

public sealed record InventoryListItemDto(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    string ProductName,
    string ProductSku,
    string? VariantName,
    string? VariantSku,
    int StockQuantity,
    int ReservedQuantity,
    int AvailableQuantity,
    int ReorderLevel,
    bool IsLowStock,
    DateTimeOffset UpdatedAt);

public sealed record InventoryTransactionListItemDto(
    Guid Id,
    Guid InventoryId,
    string TransactionType,
    int QuantityChange,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record InventoryAdjustRequestDto(
    int QuantityChange,
    string? Reason,
    string? TransactionType);

public sealed record InventoryReorderLevelRequestDto(
    int ReorderLevel);

public sealed record InventoryExpiryReleaseDto(
    int ReleasedOrderCount,
    int MaxAgeHours);
