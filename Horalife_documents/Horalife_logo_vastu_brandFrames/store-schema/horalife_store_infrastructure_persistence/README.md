# HoraLife Store Infrastructure Persistence

Copy these folders into `HoraLife.Store.Infrastructure`.

## Required project references

`HoraLife.Store.Infrastructure` must reference:

- `HoraLife.Store.Domain`
- `HoraLife.Store.Application`

## Required NuGet packages

Use package versions compatible with the EF Core version already used by your solution:

```bash
dotnet add HoraLife.Store.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add HoraLife.Store.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add HoraLife.Store.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
```

Keep the major versions aligned. For example, EF Core 10 should use the matching
Npgsql EF Core 10 provider.

## What this package contains

- `StoreDbContext`
- Configurations for all Store entities
- PostgreSQL JSONB mappings
- UUID and timestamp defaults
- Relationships and indexes
- Lowercase snake_case enum conversion
- Infrastructure dependency registration

## Connection registration later in HoraLife.Api

After adding the Infrastructure reference to the API:

```csharp
using HoraLife.Store.Infrastructure.DependencyInjection;

var storeConnection =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "DefaultConnection is missing.");

builder.Services.AddStoreInfrastructure(storeConnection);
```

You can use the same PostgreSQL connection because the Store tables are under
the separate `store` schema.

## Existing expression indexes

Your original SQL contains expression-based unique indexes for:

- `store.inventory(product_id, COALESCE(variant_id, zero UUID))`
- `store.cart_items(cart_id, product_id, COALESCE(variant_id, zero UUID))`

They are intentionally not recreated in the Fluent configurations because
ordinary EF indexes do not accurately express that `COALESCE` definition.
Keep those indexes in the database SQL.

## External HoraLife foreign keys

These IDs are mapped as scalar values only until your existing entity and table
names are confirmed:

- `UserRecommendation.UserId`
- `UserRecommendation.KundaliId`
- `CustomerAddress.UserId`
- `Order.UserId`
- `Order.RecommendedKundaliId`

Add their relationships later after referencing the appropriate existing
HoraLife domain model.

## Important Clean Architecture note

The previously generated Domain entities contain EF-related attributes. The
Fluent configurations here override those mappings. You may remove the
attributes later to make the Domain project fully persistence-independent.
