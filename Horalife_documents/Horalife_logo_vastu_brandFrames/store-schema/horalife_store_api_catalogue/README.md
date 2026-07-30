# HoraLife Store API Catalogue

Copy these folders into your existing `HoraLife.Api` project.

## Endpoints

```text
GET /api/store/categories
GET /api/store/products
GET /api/store/products/{slug}
```

## Query examples

```text
/api/store/products?category=gemstones
/api/store/products?productType=gemstone
/api/store/products?search=sapphire
/api/store/products?isFeatured=true
/api/store/products?inStockOnly=true
/api/store/products?minimumPrice=1000&maximumPrice=10000
/api/store/products?pageNumber=1&pageSize=12
/api/store/products?sortBy=price&sortDirection=asc
```

Allowed `productType` values:

```text
gemstone
yantra
rudraksha
puja_item
remedy_kit
other
```

Allowed `sortBy` values:

```text
display_order
name
price
newest
```

Confirm these registrations already exist in `Program.cs`:

```csharp
builder.Services.AddStoreApplication();
builder.Services.AddStoreInfrastructure(builder.Configuration);
```

Also confirm:

```csharp
builder.Services.AddControllers();
app.MapControllers();
```

The catalogue endpoints are public. Authorization can be added later to cart,
orders, checkout, addresses, and personalized recommendations.
