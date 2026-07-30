# HoraLife Store Infrastructure Catalogue Repositories

Copy these folders into `HoraLife.Store.Infrastructure`.

This package assumes you renamed the persistence namespace from:

`HoraLife.Store.Infrastructure.Persistence`

to:

`HoraLife.Store.Infrastructure.Data`

and that `StoreDbContext` is located under the `Data` folder.

## Included

- `CategoryRepository`
- `ProductRepository`
- Updated `AddStoreInfrastructure`

## Repository behavior

The product repository supports:

- Active products only
- Category filtering
- Search by product name, SKU, slug and short description
- Product type filtering
- Featured filtering
- Minimum and maximum price
- In-stock filtering
- Pagination
- Name, price, newest and catalogue-order sorting
- Category, images, variants and inventory loading

The queries use `AsNoTracking()` because catalogue operations are read-only.

`AsSplitQuery()` is used for product detail and listing queries because multiple
collection navigation properties are loaded.

## After copying

Build:

```bash
dotnet build HoraLife.Store.Infrastructure
```

At this point Store.Application can call repository interfaces, and
Store.Infrastructure provides the implementations.

The next step is API integration:

- Reference Store.Application from HoraLife.Api
- Reference Store.Infrastructure from HoraLife.Api
- Register `AddStoreApplication()`
- Register `AddStoreInfrastructure()`
- Add category and product controllers
