# HoraLife Store Cart Backend

This package adds the authenticated Store cart backend.

## Endpoints

```text
GET    /api/store/cart
POST   /api/store/cart/items
PUT    /api/store/cart/items/{cartItemId}
DELETE /api/store/cart/items/{cartItemId}
DELETE /api/store/cart
```

## Project placement

Copy the folders as follows:

```text
Application/*   -> HoraLife.Store.Application
Infrastructure/* -> HoraLife.Store.Infrastructure
Api/*           -> KundaliEngine.Api
```

The API DTO namespace intentionally uses:

```csharp
KundaliEngine.Api.DTOs.Store.Cart
```

## Registration

Merge the two registration snippets:

```text
Registration/StoreApplicationRegistration.txt
Registration/StoreInfrastructureRegistration.txt
```

Do not replace your complete dependency-injection classes unnecessarily.

## Authentication requirement

All cart endpoints use `[Authorize]`.

The controller resolves a GUID user ID from the first available claim:

```text
ClaimTypes.NameIdentifier
sub
userId
user_id
```

Confirm your JWT contains one of these claims. Adjust `TryGetUserId` if your
existing token uses a different claim name or a non-GUID user key.

## Cart lifecycle

- No cart row is created by `GET`.
- The first successful add creates the active cart.
- Re-adding the same product variant increases its quantity.
- One active cart is allowed per authenticated user.
- Converted carts are retained; checkout will mark the cart as converted.
- A later add creates a fresh active cart after conversion.

## Inventory policy

Adding to cart does not reserve inventory.

The service validates:

```text
available = stock_quantity - reserved_quantity
```

Actual reservation belongs in the checkout/order transaction. This prevents
abandoned carts from blocking stock.

## Variant policy

When an active product has variants, `variantId` is required. Products without
variants can be added with a null `variantId`.

## Build order

```text
1. HoraLife.Store.Application
2. HoraLife.Store.Infrastructure
3. KundaliEngine.Api
```

## Suggested Swagger test order

1. Authenticate and copy the JWT.
2. Authorize Swagger with `Bearer <token>`.
3. Call `GET /api/store/products/{slug}` and copy a product and variant ID.
4. Call `POST /api/store/cart/items`.
5. Call `GET /api/store/cart`.
6. Copy the returned cart item ID.
7. Test update, remove and clear.

Example add body:

```json
{
  "productId": "PRODUCT-GUID",
  "variantId": "VARIANT-GUID",
  "quantity": 1
}
```

Example update body:

```json
{
  "quantity": 2
}
```

## Important

No database migration is required if the existing `store.carts` and
`store.cart_items` tables and the unique active-cart indexes are already present.
