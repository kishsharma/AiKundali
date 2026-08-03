# HoraLife Store Application Catalogue

Copy these folders into `HoraLife.Store.Application`.

## Included

- Product and category DTOs
- Pagination response
- Product search filter
- Repository interfaces
- `IStoreCatalogService`
- `StoreCatalogService`
- Application dependency registration

## Project reference

`HoraLife.Store.Application` must reference:

- `HoraLife.Store.Domain`

It must not reference:

- Entity Framework Core
- PostgreSQL/Npgsql
- `HoraLife.Store.Infrastructure`
- `HoraLife.Api`

## Required package

The dependency registration extension uses
`Microsoft.Extensions.DependencyInjection.Abstractions`.

In many solutions this is already available transitively. If the project does
not compile, install it in `HoraLife.Store.Application`:

```bash
dotnet add HoraLife.Store.Application package Microsoft.Extensions.DependencyInjection.Abstractions
```

Do not install Entity Framework Core in Application.

## Next step

Implement these interfaces in `HoraLife.Store.Infrastructure`:

- `ICategoryRepository`
- `IProductRepository`

The repository implementation must load:

- Category and active products
- Product category
- Active variants
- Product images
- Inventory records

Then register them in `AddStoreInfrastructure`.
