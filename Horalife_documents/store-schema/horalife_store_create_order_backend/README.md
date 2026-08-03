# HoraLife Store – Create Order

This package implements only canonical Store Step 4:

```text
1. Customer Address CRUD                     ✓
2. Checkout Preview                          ✓
3. Revalidate Price and Stock                ✓
4. Create Order                              In progress
5. Create Order Items
6. Reserve/Deduct Inventory Transactionally
7. Convert Cart
8. Postman End-to-End Order Flow Tests
```

## Scope boundary

Step 4 creates the **order header only**:

```text
Order ID and generated order number
Authenticated user and active cart reference
Customer email/login identifier and phone
Pending order/payment statuses
Server-calculated totals
Immutable Shipping/Billing address snapshots
Order source and optional notes
```

It intentionally does **not**:

```text
Create order items                         Step 5
Reserve or deduct inventory               Step 6
Convert the cart                           Step 7
Start payment                              After all eight steps
```

The response therefore contains:

```json
"orderItemsCreated": false
```

## Endpoint

```text
POST /api/store/orders
```

Authentication is required.

## Request

Shipping also used for Billing:

```json
{
  "shippingAddressId": "SHIPPING-ADDRESS-GUID",
  "billingAddressId": null,
  "useShippingAsBilling": true,
  "notes": null
}
```

Separate Billing address:

```json
{
  "shippingAddressId": "SHIPPING-ADDRESS-GUID",
  "billingAddressId": "BILLING-ADDRESS-GUID",
  "useShippingAsBilling": false,
  "notes": "Customer-visible fulfilment note"
}
```

Do not send prices or totals. The backend:

```text
Repeats Step 3 revalidation
→ Runs Step 2 preview calculation
→ Uses server-side totals
→ Snapshots addresses
→ Inserts pending order header
```

## Project placement

```text
Application/*     → HoraLife.Store.Application
Infrastructure/*  → HoraLife.Store.Infrastructure
Api/*             → KundaliEngine.Api
```

## Registration

Merge both files under:

```text
Registration/
```

into existing Store registration methods.

## Database safeguard

Run once:

```text
Database/ensure_unique_order_per_cart.sql
```

Also merge the provided OrderConfiguration index update so the EF model
matches PostgreSQL.

The unique partial index prevents two order headers from being created from
the same cart.

## Expected successful response

```text
201 Created
orderStatus = pending
paymentStatus = pending
orderItemsCreated = false
```

## Expected errors

```text
400 Bad Request    Missing customer identity or unsupported country
401 Unauthorized   Missing/invalid JWT
404 Not Found      Cart or selected address not found
409 Conflict       Cart invalid/empty, duplicate order, or blocked preview
```

## Important final architecture note

The current endpoint allows Step 4 to be tested independently. Before payment
goes live, Steps 4–7 should be orchestrated as one checkout workflow, with the
inventory-sensitive portion protected by the Step 6 database transaction.
