# HoraLife Store – Customer Address CRUD

This package implements canonical Store roadmap step:

```text
1. Customer Address CRUD
```

The next roadmap steps remain unchanged:

```text
2. Checkout Preview
3. Revalidate Price and Stock
4. Create Order
5. Create Order Items
6. Reserve/Deduct Inventory Transactionally
7. Convert Cart
8. Postman End-to-End Order Flow Tests
```

## Endpoints

```text
GET    /api/store/addresses
GET    /api/store/addresses/{addressId}
POST   /api/store/addresses
PUT    /api/store/addresses/{addressId}
DELETE /api/store/addresses/{addressId}
PUT    /api/store/addresses/{addressId}/default
```

All endpoints require authentication.

## Project placement

```text
Application/*     → HoraLife.Store.Application
Infrastructure/*  → HoraLife.Store.Infrastructure
Api/*             → KundaliEngine.Api
```

## Dependency injection

Merge the registration snippets from:

```text
Registration/StoreApplicationRegistration.txt
Registration/StoreInfrastructureRegistration.txt
```

Do not replace the entire existing DI classes.

## Default-address rule

HoraLife supports:

```text
One default Shipping address per user
One default Billing address per user
```

Rules:

1. The first address of a type becomes default automatically.
2. Creating another address with `isDefault: true` removes the previous default of that type.
3. Setting an address as default affects only addresses of the same type.
4. Deleting a default address automatically promotes another address of that type when one exists.
5. Updating a default address to `isDefault: false` promotes another address of that type when possible.
6. A user can never read, edit, delete, or set another user's address as default.

## Database protection

Run:

```text
Database/ensure_unique_default_address.sql
```

Also merge the EF configuration update so future migrations retain the same rule.

## Example create request

```json
{
  "addressType": "shipping",
  "fullName": "Kishore Sharma",
  "phone": "9876543210",
  "addressLine1": "123 Main Road",
  "addressLine2": "Near Landmark",
  "city": "Bengaluru",
  "state": "Karnataka",
  "postalCode": "560064",
  "countryCode": "IN",
  "isDefault": true
}
```

Allowed address types:

```text
shipping
billing
```

## Expected status codes

```text
200 OK             Read, update, or set default succeeded
201 Created        Address created
204 No Content     Address deleted
400 Bad Request    Validation failed
401 Unauthorized   JWT missing or user claim invalid
404 Not Found      Address does not exist for this user
```

## Swagger test order

1. Authorize Swagger using a valid JWT.
2. Create the first Shipping address with `isDefault: false`.
3. Verify it automatically becomes default.
4. Create a second Shipping address with `isDefault: true`.
5. Verify the first Shipping address is no longer default.
6. Create a Billing address.
7. Verify Shipping and Billing each have one default.
8. Update an address.
9. Set an address as default.
10. Attempt to read an unknown address and verify `404`.
11. Delete a default address and verify another same-type address is promoted.
12. Call the list endpoint and verify the final state.

## Build order

```text
1. HoraLife.Store.Domain
2. HoraLife.Store.Application
3. HoraLife.Store.Infrastructure
4. KundaliEngine.Api
```

## Postman

After Swagger verification, add the Address requests to the existing:

```text
HoraLife API
└── Store
    └── Addresses
```

The Postman collection can then save `addressId` automatically for update,
default, and delete tests.
