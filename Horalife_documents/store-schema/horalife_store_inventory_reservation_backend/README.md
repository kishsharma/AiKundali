# HoraLife Store – Transactional Inventory Reservation

This implements canonical Store Step 6:

```text
1. Customer Address CRUD                     ✓
2. Checkout Preview                          ✓
3. Revalidate Price and Stock                ✓
4. Create Order                              ✓
5. Create Order Items                        ✓
6. Reserve/Deduct Inventory Transactionally  In progress
7. Convert Cart
8. Postman End-to-End Order Flow Tests
```

## Decision for the current unpaid-order flow

The order currently has:

```text
order_status = pending
payment_status = pending
```

Therefore Step 6 **reserves** inventory:

```text
stock_quantity     remains unchanged
reserved_quantity  increases
available_quantity decreases
```

It also writes an `order_reserved` inventory transaction with a negative
`quantity_change`, representing the reduction in sellable availability.

Permanent stock deduction happens after successful payment via:

```text
POST /api/store/orders/{orderId}/inventory/confirm
POST /api/store/orders/{orderId}/mark-paid
```

```text
stock_quantity     decreases
reserved_quantity  decreases
transaction_type   order_confirmed
```

Payment failure, cancellation, or timeout release the reservation via:

```text
POST /api/store/orders/{orderId}/inventory/release
POST /api/store/orders/{orderId}/cancel
POST /api/store/inventory/release-expired   (admin / sweeper)
```

```text
stock_quantity     unchanged
reserved_quantity  decreases
transaction_type   reservation_released
```

## Endpoints

```text
POST /api/store/orders/{orderId}/inventory/reserve
POST /api/store/orders/{orderId}/inventory/confirm
POST /api/store/orders/{orderId}/inventory/release
POST /api/store/orders/{orderId}/mark-paid
POST /api/store/orders/{orderId}/cancel
```

Current test order:

```text
POST /api/store/orders/379e8f91-9938-4b08-983d-282c4e5a59b7/inventory/reserve
```

No request body is required for reserve/confirm. Release accepts optional
`{ "reason": "cancelled" }`. Authentication is required.

## Transactional flow

Inside one PostgreSQL transaction:

```text
Lock order row with FOR UPDATE
→ confirm ownership and pending status
→ reject an existing reservation
→ load order items
→ lock matching inventory rows with FOR UPDATE
→ recheck available quantity
→ increment reserved_quantity
→ insert inventory transaction rows
→ SaveChanges
→ COMMIT
```

If any order item lacks stock:

```text
ROLLBACK
```

No inventory row or transaction row is partially saved.

## Concurrency protection

The order row is locked first, which serializes concurrent reservation
requests for the same order.

Each inventory row is also locked before availability is checked, preventing
two different orders from reserving the same final unit.

The supplied database unique index is a final idempotency safeguard.

## Existing entity compatibility

This package is aligned to the existing HoraLife entities:

```text
Inventory.StockQuantity
Inventory.ReservedQuantity
Inventory.UpdatedAt
InventoryTransaction.TransactionType
InventoryTransaction.QuantityChange
InventoryTransaction.ReferenceType
InventoryTransaction.ReferenceId
OrderItem.ProductId is Guid?
StoreDbContext.Inventory
StoreDbContext.InventoryTransactions
```

No Domain entity change or migration is required.

## Project placement

```text
Application/*     → HoraLife.Store.Application
Infrastructure/*  → HoraLife.Store.Infrastructure
Api/*             → KundaliEngine.Api
```

## Registrations

Merge both files under `Registration/`.

## Database safeguards

Run once:

```text
Database/inventory_reservation_safeguards.sql
```

## Expected first response

For the current one-quantity Blue Sapphire order:

```text
201 Created
inventoryReserved = true
stockQuantityDeducted = false
totalReservedQuantity = 1
```

Inventory should change from:

```text
stock_quantity = 5
reserved_quantity = 0
available_quantity = 5
```

to:

```text
stock_quantity = 5
reserved_quantity = 1
available_quantity = 4
```

## Duplicate retry

Calling the same endpoint again should return:

```text
409 Conflict
Inventory already reserved
```

No second inventory transaction or quantity change should occur.

## Test rollback

To test transactional rollback with multiple order items:

1. Use an order containing at least two different items.
2. Make the second item unavailable.
3. Call the reservation endpoint.
4. Expect `409 Insufficient stock`.
5. Confirm that the first item was also not reserved.

The full multi-item rollback case will also be covered by canonical Step 8.
