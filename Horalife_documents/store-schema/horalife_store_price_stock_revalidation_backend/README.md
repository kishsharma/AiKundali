# HoraLife Store – Price and Stock Revalidation

This package implements canonical Store roadmap Step 3:

```text
1. Customer Address CRUD                     ✓
2. Checkout Preview                          ✓
3. Revalidate Price and Stock                In progress
4. Create Order
5. Create Order Items
6. Reserve/Deduct Inventory Transactionally
7. Convert Cart
8. Postman End-to-End Order Flow Tests
```

## Endpoint

```text
POST /api/store/checkout/revalidate
```

No request body is required. Authentication is required.

## Project placement

```text
Application/* → HoraLife.Store.Application
Api/*         → KundaliEngine.Api
```

No Domain, Infrastructure, or database changes are required. The service
reuses the existing tracked cart query and `ICartRepository.SaveChangesAsync()`.

## Dependency injection

Merge:

```text
Registration/StoreApplicationRegistration.txt
```

into the existing `AddStoreApplication()` method.

## What this step validates

For every active-cart item:

```text
Product is active
Selected variant is active
Cart and product currencies match
Current available stock covers requested quantity
Stored cart price matches current catalogue price
```

Available stock is:

```text
stock_quantity - reserved_quantity
```

## Response behaviour

### `200 OK`

Returned only when every item is still valid and no price changed.

```json
{
  "isValid": true,
  "requiresNewPreview": false,
  "cartPricesUpdated": false
}
```

### `409 Conflict` – price changed

When a current catalogue price differs from the cart price:

1. The service updates the cart item to the current price.
2. It returns `409 Conflict`.
3. `requiresNewPreview` is `true`.
4. The frontend must generate Checkout Preview again.
5. A second revalidation should pass when nothing else changed.

This prevents an order from being created using a stale displayed total.

### `409 Conflict` – availability problem

Returned when a product/variant is inactive, currency differs, or stock is
insufficient. The customer must correct the cart before continuing.

### Other status codes

```text
401 Unauthorized   Missing/invalid JWT
404 Not Found      No active cart
409 Conflict       Empty cart or validation failed
```

## Important transaction boundary

This step does not reserve or deduct inventory.

A product can still sell out between this call and order processing. Canonical
Step 6 must recheck and reserve/deduct inventory inside the final database
transaction. Step 3 is the pre-order customer-facing validation; Step 6 is the
concurrency-safe inventory guarantee.

## Suggested test order

1. Use the current valid Blue Sapphire cart.
2. Call `POST /api/store/checkout/revalidate`.
3. Expect `200` and `isValid = true`.
4. Change the selected variant price directly in PostgreSQL.
5. Call revalidate.
6. Expect `409`, `requiresNewPreview = true`, and `cartPricesUpdated = true`.
7. Call Checkout Preview and confirm the new price.
8. Call revalidate again and expect `200`.
9. Reduce available inventory below the cart quantity.
10. Call revalidate and expect `409` with `INSUFFICIENT_STOCK`.
11. Restore the test price and inventory.
12. Test the endpoint with No Auth and expect `401`.

## Example price-change SQL

Use your actual variant ID and remember the original price:

```sql
UPDATE store.product_variants
SET price = 9600.00,
    updated_at = NOW()
WHERE id = '5d78efc8-0d0b-4f3b-843e-61e3c963ca5c';
```

Restore:

```sql
UPDATE store.product_variants
SET price = 9500.00,
    updated_at = NOW()
WHERE id = '5d78efc8-0d0b-4f3b-843e-61e3c963ca5c';
```

## Example stock-change SQL

First inspect the selected inventory row:

```sql
SELECT id, product_id, variant_id,
       stock_quantity, reserved_quantity
FROM store.inventory
WHERE variant_id =
    '5d78efc8-0d0b-4f3b-843e-61e3c963ca5c';
```

To simulate no available stock:

```sql
UPDATE store.inventory
SET reserved_quantity = stock_quantity,
    updated_at = NOW()
WHERE variant_id =
    '5d78efc8-0d0b-4f3b-843e-61e3c963ca5c';
```

Restore the original `reserved_quantity` after the test.
