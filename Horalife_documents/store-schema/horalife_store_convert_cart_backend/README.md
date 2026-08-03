# HoraLife Store – Convert Cart

This implements canonical Store Step 7:

```text
1. Customer Address CRUD                     ✓
2. Checkout Preview                          ✓
3. Revalidate Price and Stock                ✓
4. Create Order                              ✓
5. Create Order Items                        ✓
6. Reserve/Deduct Inventory Transactionally  ✓
7. Convert Cart                              In progress
8. Postman End-to-End Order Flow Tests
```

## Meaning of conversion

The source cart is not deleted.

```text
CartStatus.Active
→ CartStatus.Converted
```

Cart items remain stored for traceability. The existing order already links
back to the source cart through `orders.cart_id`.

No new active cart is created during conversion. The existing
`StoreCartService.AddItemAsync()` creates a fresh active cart automatically
when the user later adds another product.

## Endpoint

```text
POST /api/store/orders/{orderId}/cart/convert
```

Current test order:

```text
POST /api/store/orders/379e8f91-9938-4b08-983d-282c4e5a59b7/cart/convert
```

No request body is required. Authentication is required.

## Transactional checks

Inside one PostgreSQL transaction:

```text
Lock order row
→ verify order belongs to user and is pending
→ verify order has a source cart
→ verify order items exist
→ verify Step 6 inventory reservation exists
→ lock source cart
→ verify ownership
→ verify cart is active
→ set status to Converted
→ update timestamp
→ commit
```

If any prerequisite fails, the cart remains active.

## Expected first response

```text
200 OK
previousCartStatus = active
currentCartStatus = converted
inventoryReserved = true
cartConverted = true
newActiveCartCreated = false
```

## Expected cart endpoint after conversion

Calling:

```text
GET /api/store/cart
```

should return the existing empty-cart DTO because no active cart exists:

```json
{
  "id": null,
  "currency": "INR",
  "totalQuantity": 0,
  "subtotal": 0,
  "items": []
}
```

The converted cart is still present in PostgreSQL.

## Duplicate retry

Calling the conversion endpoint again returns:

```text
409 Conflict
Cart is already converted
```

## New cart behavior

After conversion, add a product through the existing cart endpoint.

Expected:

```text
A new CartStatus.Active cart is created
The converted cart remains unchanged
The new cart has a different ID
```

## Project placement

```text
Application/*     → HoraLife.Store.Application
Infrastructure/*  → HoraLife.Store.Infrastructure
Api/*             → KundaliEngine.Api
```

## Registrations

Merge both files under `Registration/`.

## Database changes

No schema or migration is required. The existing entity already contains:

```text
Cart.Status
Cart.UpdatedAt
CartStatus.Converted
Order.CartId
```
