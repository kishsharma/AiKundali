# HoraLife Store Cart – Implementation Conditions and Rules

## 1. Scope

This document defines the conditions, technical rules, and expected behaviour for the HoraLife Store cart module.

The cart module exposes the following authenticated APIs:

```text
GET    /api/store/cart
POST   /api/store/cart/items
PUT    /api/store/cart/items/{cartItemId}
DELETE /api/store/cart/items/{cartItemId}
DELETE /api/store/cart
```

---

## 2. Project Placement

Copy the code into the existing projects as follows:

```text
Application/*     → HoraLife.Store.Application
Infrastructure/*  → HoraLife.Store.Infrastructure
Api/*             → KundaliEngine.Api
```

Recommended API folders:

```text
KundaliEngine.Api
├── Controllers
│   └── Store
│       └── StoreCartController.cs
└── DTOs
    └── Store
        └── Cart
            ├── AddCartItemRequestDto.cs
            └── UpdateCartItemRequestDto.cs
```

---

## 3. Dependency Injection Registration

Inside `AddStoreApplication()` add:

```csharp
services.AddScoped<IStoreCartService, StoreCartService>();
```

Inside `AddStoreInfrastructure()` add:

```csharp
services.AddScoped<ICartRepository, CartRepository>();
```

Keep the existing catalogue registrations:

```csharp
services.AddScoped<IStoreCatalogService, StoreCatalogService>();
services.AddScoped<ICategoryRepository, CategoryRepository>();
services.AddScoped<IProductRepository, ProductRepository>();
```

---

## 4. Authentication Conditions

All cart endpoints must use:

```csharp
[Authorize]
```

The controller should identify the logged-in user from the first available claim:

```text
ClaimTypes.NameIdentifier
sub
userId
user_id
```

The user identifier must be a valid GUID.

If the JWT uses a different claim name, update the `TryGetUserId` method accordingly.

If no valid user GUID is found, the API must return:

```text
401 Unauthorized
```

---

## 5. Active Cart Rules

1. A cart must not be created when the user only calls `GET /api/store/cart`.
2. The first successful add-to-cart operation creates the active cart.
3. A user can have only one active cart.
4. A converted cart must remain stored for audit and order history.
5. A converted cart must never be reused as the active cart.
6. When the user adds another product after conversion, a new active cart must be created.
7. The cart currency must remain consistent.
8. A product using a different currency from the active cart must be rejected.

---

## 6. Cart Item Rules

1. Quantity must be at least `1`.
2. Quantity must not exceed `99`.
3. Quantity `0` must not be used to delete an item.
4. The remove-item endpoint must be used for deletion.
5. Adding the same product with the same variant must increase the existing item quantity.
6. Adding the same product with a different variant must create a separate cart item.
7. The cart item must store the current unit price at the time of add or update.
8. The API must recalculate line totals and subtotal from item quantity and stored unit price.

---

## 7. Product Validation Rules

Before adding a product:

1. The product must exist.
2. The product must be active.
3. The selected variant must belong to that product.
4. The selected variant must be active.
5. If the product has active variants, `variantId` is mandatory.
6. If the product has no variants, `variantId` may be null.
7. Inactive products or variants must not be added.

Expected failures:

```text
Product not found        → 404
Variant not found        → 404
Variant required         → 400
```

---

## 8. Inventory Conditions

Available inventory must be calculated as:

```text
available quantity = stock quantity - reserved quantity
```

Rules:

1. The requested quantity must not exceed available inventory.
2. When the same item already exists in the cart, validation must use the final combined quantity.
3. Products with zero available inventory must not be added.
4. Updating quantity must validate stock again.
5. Cart operations must not directly reduce stock quantity.
6. Cart operations must not reserve inventory.

If inventory is insufficient, return:

```text
409 Conflict
```

---

## 9. Inventory Reservation Policy

Adding a product to the cart does not reserve inventory.

Inventory reservation must happen during checkout or order creation, inside a transaction.

This prevents abandoned carts from blocking stock for other customers.

At checkout, the application must:

1. Revalidate product availability.
2. Revalidate product and variant status.
3. Revalidate prices.
4. Reserve or deduct inventory transactionally.
5. Prevent overselling under concurrent requests.

---

## 10. Price Rules

1. Variant price must be used when a variant is selected.
2. Product base price must be used when no variant exists.
3. Price must be refreshed when cart quantity is updated.
4. Checkout must perform a final price validation.
5. The client must never be trusted to supply product price.
6. All totals must be calculated by the backend.

---

## 11. Cart Response Rules

A cart response should contain:

```text
Cart ID
Currency
Total quantity
Subtotal
Cart items
```

Each cart item should contain:

```text
Cart item ID
Product ID
Variant ID
Product slug
Product name
Variant name
SKU
Image URL
Image alt text
Unit price
Quantity
Line total
Currency
Available quantity
Requires consultation
```

For a user without an active cart, return an empty response instead of creating a database record.

Example:

```json
{
  "id": null,
  "currency": "INR",
  "totalQuantity": 0,
  "subtotal": 0,
  "items": []
}
```

---

## 12. Remove and Clear Rules

### Remove Item

```text
DELETE /api/store/cart/items/{cartItemId}
```

Conditions:

1. The cart item must belong to the authenticated user's active cart.
2. If not found, return `404 Not Found`.
3. On success, return `204 No Content`.

### Clear Cart

```text
DELETE /api/store/cart
```

Conditions:

1. Remove all items from the active cart.
2. Keep the cart row unless business rules later require otherwise.
3. If the cart is already empty, still return success.
4. On success, return `204 No Content`.

---

## 13. Expected HTTP Status Codes

```text
200 OK             Successful cart read, add, or quantity update
204 No Content     Successful remove item or clear cart
400 Bad Request    Invalid quantity, missing variant, or currency mismatch
401 Unauthorized   Missing or invalid authenticated user
404 Not Found      Product, variant, or cart item not found
409 Conflict       Insufficient inventory
```

---

## 14. Database Conditions

The following tables must already exist:

```text
store.carts
store.cart_items
store.products
store.product_variants
store.product_images
store.inventory
```

Recommended database constraints:

1. One active cart per user.
2. Unique product-and-variant combination inside a cart.
3. Positive quantity check.
4. Non-negative unit price.
5. Valid foreign keys to product and variant.
6. Cart status stored using the configured PostgreSQL enum or mapped value.

No new migration is required if these tables and constraints already exist.

---

## 15. Build Order

Build in this order:

```text
1. HoraLife.Store.Domain
2. HoraLife.Store.Application
3. HoraLife.Store.Infrastructure
4. KundaliEngine.Api
```

---

## 16. Swagger Testing Order

1. Login and generate a valid JWT.
2. Open Swagger authorization.
3. Enter:

```text
Bearer <access-token>
```

4. Get a product and variant ID.
5. Add the item to the cart.
6. Read the cart.
7. Update the quantity.
8. Remove the item.
9. Clear the cart.
10. Test insufficient-stock behaviour.
11. Test unauthenticated access.

Example add request:

```json
{
  "productId": "PRODUCT-GUID",
  "variantId": "VARIANT-GUID",
  "quantity": 1
}
```

Example update request:

```json
{
  "quantity": 2
}
```

---

## 17. Postman Collection Conditions

Create requests for:

```text
Get Cart
Add Cart Item
Update Cart Item
Remove Cart Item
Clear Cart
Invalid Quantity
Insufficient Stock
Unauthorized Cart Access
Unknown Cart Item
```

Recommended Postman variables:

```text
baseUrl
accessToken
productId
variantId
cartItemId
```

Use an Authorization header:

```text
Bearer {{accessToken}}
```

After adding an item, automatically save:

```javascript
const body = pm.response.json();

if (body.items && body.items.length > 0) {
    pm.environment.set(
        "cartItemId",
        body.items[0].id
    );
}
```

---

## 18. Future Checkout Conditions

The checkout module must later:

1. Revalidate cart ownership.
2. Revalidate product status.
3. Revalidate variant status.
4. Revalidate prices.
5. Revalidate inventory.
6. Create the order.
7. Create order items using a price snapshot.
8. Reserve or deduct inventory.
9. Mark the cart as converted.
10. Link the converted cart to the created order.
11. Perform all critical operations inside a database transaction.
12. Roll back the complete transaction if any step fails.

---

## 19. Important Business Rule

Gemstones and other consultation-sensitive products must not be blindly recommended or sold only because a planet is weak.

The recommendation engine must evaluate suitability separately.

The cart module only handles products explicitly selected by the user or approved by the recommendation workflow.

Products marked `RequiresConsultation` should remain visibly identified in cart and checkout responses.
