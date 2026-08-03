# HoraLife Store – Checkout Preview

Canonical Store roadmap:

```text
1. Customer Address CRUD                     ✓
2. Checkout Preview                          In progress
3. Revalidate Price and Stock
4. Create Order
5. Create Order Items
6. Reserve/Deduct Inventory Transactionally
7. Convert Cart
8. Postman End-to-End Order Flow Tests
```

## Endpoint

```text
POST /api/store/checkout/preview
```

## Placement

```text
Application/* → HoraLife.Store.Application
Api/*         → KundaliEngine.Api
```

No Domain, Infrastructure, or database changes are required. This feature
reuses `ICartRepository` and `ICustomerAddressRepository`.

## DI registration

```csharp
services.AddScoped<IStoreCheckoutPreviewService, StoreCheckoutPreviewService>();
```

## Request: same Shipping and Billing address

```json
{
  "shippingAddressId": "SHIPPING-ADDRESS-GUID",
  "billingAddressId": null,
  "useShippingAsBilling": true
}
```

## Request: separate Billing address

```json
{
  "shippingAddressId": "SHIPPING-ADDRESS-GUID",
  "billingAddressId": "BILLING-ADDRESS-GUID",
  "useShippingAsBilling": false
}
```

## V1 delivery policy

```text
India only
₹99 shipping below ₹1,500
Free shipping at ₹1,500 or above
Estimated delivery: 4–7 days
Discount: ₹0
Tax: ₹0
```

## Boundary

Checkout Preview is informational. It reads current catalogue price and stock,
calculates totals, and returns warnings. It does not create an order, change the
cart, or reserve inventory.

Canonical Step 3 remains the final blocking check immediately before order
creation:

```text
3. Revalidate Price and Stock
```

## Expected status codes

```text
200 Preview generated
400 Unsupported delivery country
401 Missing/invalid user
404 Cart or selected address not found
409 Active cart is empty
```

## Swagger preparation

1. Authenticate.
2. Add at least one cart item.
3. Create a Shipping address.
4. Call `POST /api/store/checkout/preview` with that address ID.

## Build order

```text
1. HoraLife.Store.Application
2. KundaliEngine.Api
```
