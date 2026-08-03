# HoraLife Store Complete E2E Postman Collection

## Import
Import:
- `HoraLife_Store_Complete_E2E.postman_collection.json`
- `HoraLife_Local_Store_E2E.postman_environment.json`

Select the environment and set `loginPassword`. Adjust `baseUrl`, login path,
identifier field, identifier, product or variant IDs when required.

## Run
Run the whole collection in its existing folder/request order using Collection Runner.

## Coverage
Login → catalogue → cart → Shipping/Billing addresses → Checkout Preview →
price/stock revalidation → Create Order → duplicate order 409 → Create Order
Items → duplicate items 409 → transactional inventory reservation → duplicate
reservation 409 → Convert Cart → duplicate conversion 409 → verify fresh-cart
behaviour → invalid-address and unauthorized checks → cleanup → final summary.

## Result
The final request stores:
- `e2eOverallStatus`
- `e2eSummary`

Expected: `e2eOverallStatus = PASS`.

The final request also emits one Postman test for each major section and prints
the detailed JSON summary to Postman Console.

## Important
A successful run creates a real pending order, converted cart, order-item
snapshots and inventory reservation. These remain as audit records. Temporary
addresses and the fresh post-conversion active cart are cleaned up.

Because payment/cancellation reservation release is outside the current eight
steps, every successful rerun increases `reserved_quantity`. Use a test database
or reset/release test reservations before repeated runs.


## Login URL correction

Use:

```text
baseUrl = https://localhost:7245
authLoginPath = api/auth/login
```

Do not add `/` at the beginning of `authLoginPath`. The login request now sends exactly:

```text
https://localhost:7245/api/auth/login
```

The login test safely handles empty or non-JSON error responses.
