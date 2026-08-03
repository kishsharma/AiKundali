# HoraLife Store - Create Order Items

Canonical Step 5 only:

1. Customer Address CRUD                     complete
2. Checkout Preview                          complete
3. Revalidate Price and Stock                complete
4. Create Order                              complete
5. Create Order Items                        in progress
6. Reserve/Deduct Inventory Transactionally
7. Convert Cart
8. Postman End-to-End Order Flow Tests

## Endpoint

POST /api/store/orders/{orderId}/items

For the current order:

POST /api/store/orders/379e8f91-9938-4b08-983d-282c4e5a59b7/items

No request body is required.

## What it does

- Loads the authenticated user's pending order.
- Confirms its linked active cart still matches.
- Confirms the cart subtotal still matches the order subtotal.
- Copies product, variant, SKU, price, quantity and consultation flag as immutable snapshots.
- Rejects duplicate item creation.

## What it does not do

- No stock reservation or deduction: Step 6.
- No cart conversion: Step 7.
- No payment initiation yet.

## Expected success

201 Created
orderItemsCreated = true
totalsMatch = true
itemsSubtotal = orderSubtotal

Calling it again must return 409.

## Placement

Application/* -> HoraLife.Store.Application
Infrastructure/* -> HoraLife.Store.Infrastructure
Api/* -> KundaliEngine.Api

Merge both registration files and run the database safeguard once.
Read the compatibility note before compiling because existing entity property names may differ.
