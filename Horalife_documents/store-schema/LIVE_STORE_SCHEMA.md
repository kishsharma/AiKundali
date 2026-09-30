# HoraLife live store schema

Captured from the local `horalife` database, schema `store`, on 27 September 2026. Credentials are intentionally omitted. All 16 tables existed and were empty at capture time.

The shop is not a separate database. Portal identity stays in `public."Users"` (`Id uuid`). Store rows point at that id through `user_id` columns. There is no foreign key from `store` to `public`.

`StoreDbContext` in `HoraLife.Store.Infrastructure` maps this schema. Status columns are `varchar`, not Postgres enums. The API writes them as lowercase snake_case, matching `HoraLife.Store.Domain.Enums.StoreEnums`.

## Identity link

| Store column | Matches |
| --- | --- |
| `carts.user_id` | `public."Users"."Id"` |
| `customer_addresses.user_id` | `public."Users"."Id"` |
| `orders.user_id` | `public."Users"."Id"` |
| `user_recommendations.user_id` | `public."Users"."Id"` |
| `orders.recommended_kundali_id` | portal kundali id, no FK |
| `user_recommendations.kundali_id` | portal kundali id, no FK |

`carts.user_id` is nullable because a cart may instead carry `guest_token`. The current API only creates user carts from the JWT `NameIdentifier` claim.

## Status vocabularies

| Column | Allowed values |
| --- | --- |
| `products.product_type` | `gemstone`, `yantra`, `rudraksha`, `puja_item`, `remedy_kit`, `other` |
| `carts.status` | `active`, `converted`, `abandoned`, `expired` |
| `customer_addresses.address_type` | `shipping`, `billing` |
| `orders.order_status` | `pending`, `confirmed`, `processing`, `shipped`, `delivered`, `cancelled`, `returned` |
| `orders.payment_status` | `pending`, `authorized`, `paid`, `failed`, `refunded`, `partially_refunded` |
| `orders.order_source` | `direct_store`, `kundali_recommendation`, `compatibility_recommendation`, `admin` |
| `payments.payment_status` | `pending`, `authorized`, `captured`, `failed`, `refunded`, `partially_refunded` |
| `shipments.shipment_status` | `pending`, `ready_to_ship`, `shipped`, `in_transit`, `out_for_delivery`, `delivered`, `failed`, `returned` |
| `inventory_transactions.transaction_type` | `stock_added`, `order_reserved`, `order_confirmed`, `reservation_released`, `return_received`, `manual_adjustment` |

## Tables

### categories

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| parent_id | uuid | yes | |
| name | varchar(150) | no | |
| slug | varchar(160) | no | |
| description | text | yes | |
| display_order | integer | no | `0` |
| is_active | boolean | no | `true` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Unique: `slug`. FK: `parent_id` -> `categories.id`.

### products

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| category_id | uuid | no | |
| sku | varchar(100) | no | |
| slug | varchar(180) | no | |
| name | varchar(250) | no | |
| short_description | varchar(500) | yes | |
| description | text | yes | |
| product_type | varchar(40) | no | |
| base_price | numeric(12,2) | no | `0` |
| compare_at_price | numeric(12,2) | yes | |
| currency | char(3) | no | `INR` |
| attributes | jsonb | no | `{}` |
| requires_consultation | boolean | no | `false` |
| is_featured | boolean | no | `false` |
| is_active | boolean | no | `true` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Unique: `sku`, `slug`. FK: `category_id` -> `categories.id`.

### product_variants

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| product_id | uuid | no | |
| sku | varchar(100) | no | |
| name | varchar(200) | no | |
| attributes | jsonb | no | `{}` |
| price | numeric(12,2) | no | |
| compare_at_price | numeric(12,2) | yes | |
| weight_grams | numeric(10,3) | yes | |
| is_active | boolean | no | `true` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Unique: `sku`. FK: `product_id` -> `products.id`.

### product_images

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| product_id | uuid | no | |
| variant_id | uuid | yes | |
| image_url | text | no | |
| alt_text | varchar(250) | yes | |
| display_order | integer | no | `0` |
| is_primary | boolean | no | `false` |
| created_at | timestamptz | no | `now()` |

FK: `product_id` -> `products.id`, `variant_id` -> `product_variants.id`.

### inventory

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| product_id | uuid | no | |
| variant_id | uuid | yes | |
| stock_quantity | integer | no | `0` |
| reserved_quantity | integer | no | `0` |
| reorder_level | integer | no | `0` |
| updated_at | timestamptz | no | `now()` |

Unique: `(product_id, coalesce(variant_id, zero-uuid))`. Available quantity is `stock_quantity - reserved_quantity`.

### inventory_transactions

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| inventory_id | uuid | no | |
| transaction_type | varchar(30) | no | |
| quantity_change | integer | no | |
| reference_type | varchar(50) | yes | |
| reference_id | uuid | yes | |
| notes | text | yes | |
| created_at | timestamptz | no | `now()` |

FK: `inventory_id` -> `inventory.id`. Unique partial index stops a second `order_reserved` row for the same order and inventory record.

### remedy_keys

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| remedy_key | varchar(100) | no | |
| name | varchar(200) | no | |
| remedy_type | varchar(40) | no | |
| description | text | yes | |
| is_active | boolean | no | `true` |
| created_at | timestamptz | no | `now()` |

Unique: `remedy_key`.

### product_remedy_mappings

Composite PK `(product_id, remedy_key_id)`.

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| product_id | uuid | no | |
| remedy_key_id | uuid | no | |
| recommendation_rank | integer | no | `100` |
| match_strength | numeric(5,2) | no | `50` |
| notes | text | yes | |

### user_recommendations

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| user_id | uuid | no | |
| kundali_id | uuid | no | |
| remedy_key_id | uuid | no | |
| reason_code | varchar(100) | yes | |
| display_reason | varchar(500) | yes | |
| confidence_score | numeric(5,2) | yes | |
| valid_until | timestamptz | yes | |
| created_at | timestamptz | no | `now()` |

Index: `(user_id, kundali_id)`. FK: `remedy_key_id` -> `remedy_keys.id`. No HTTP API yet.

### carts

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| user_id | uuid | yes | |
| guest_token | varchar(150) | yes | |
| status | varchar(20) | no | `active` |
| currency | char(3) | no | `INR` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Partial unique indexes: one active cart per `user_id`, and one active cart per `guest_token`.

### cart_items

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| cart_id | uuid | no | |
| product_id | uuid | no | |
| variant_id | uuid | yes | |
| quantity | integer | no | `1` |
| unit_price | numeric(12,2) | no | |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Unique: `(cart_id, product_id, coalesce(variant_id, zero-uuid))`.

### customer_addresses

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| user_id | uuid | no | |
| address_type | varchar(20) | no | `shipping` |
| full_name | varchar(200) | no | |
| phone | varchar(30) | no | |
| address_line1 | varchar(250) | no | |
| address_line2 | varchar(250) | yes | |
| city | varchar(100) | no | |
| state | varchar(100) | no | |
| postal_code | varchar(20) | no | |
| country_code | char(2) | no | `IN` |
| is_default | boolean | no | `false` |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Partial unique index: one default address per `(user_id, address_type)`. Checkout currently accepts India (`IN`) only.

### orders

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| order_number | bigint | no | |
| user_id | uuid | yes | |
| cart_id | uuid | yes | |
| customer_email | varchar(250) | no | |
| customer_phone | varchar(30) | yes | |
| order_status | varchar(30) | no | `pending` |
| payment_status | varchar(30) | no | `pending` |
| subtotal_amount | numeric(12,2) | no | `0` |
| discount_amount | numeric(12,2) | no | `0` |
| shipping_amount | numeric(12,2) | no | `0` |
| tax_amount | numeric(12,2) | no | `0` |
| total_amount | numeric(12,2) | no | `0` |
| currency | char(3) | no | `INR` |
| shipping_address | jsonb | no | |
| billing_address | jsonb | yes | |
| order_source | varchar(40) | no | `direct_store` |
| recommended_kundali_id | uuid | yes | |
| notes | text | yes | |
| placed_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Unique: `order_number`. Partial unique: one order per non-null `cart_id`. Indexes: `(user_id, placed_at desc)`, `(order_status, placed_at desc)`.

### order_items

Price snapshot. `product_id` is nullable so the line survives if the catalogue row is later removed.

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| order_id | uuid | no | |
| product_id | uuid | yes | |
| variant_id | uuid | yes | |
| product_name | varchar(250) | no | |
| sku | varchar(100) | no | |
| variant_name | varchar(200) | yes | |
| quantity | integer | no | |
| unit_price | numeric(12,2) | no | |
| discount_amount | numeric(12,2) | no | `0` |
| tax_amount | numeric(12,2) | no | `0` |
| line_total | numeric(12,2) | no | |
| recommendation_id | uuid | yes | |

Unique: `(order_id, product_id, coalesce(variant_id, zero-uuid))`. FK: `recommendation_id` -> `user_recommendations.id`.

### payments

Table exists. No store payment HTTP API yet. Portal Razorpay billing writes `public."Payments"`, not this table.

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| order_id | uuid | no | |
| provider | varchar(50) | no | |
| provider_payment_id | varchar(200) | yes | |
| provider_order_id | varchar(200) | yes | |
| provider_event_id | varchar(200) | yes | |
| payment_status | varchar(30) | no | `pending` |
| amount | numeric(12,2) | no | |
| currency | char(3) | no | `INR` |
| payment_method | varchar(50) | yes | |
| gateway_response | jsonb | yes | |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

Partial unique: `(provider, provider_event_id)` when the event id is present.

### shipments

Table exists. No shipment HTTP API yet.

| Column | Type | Nullable | Default |
| --- | --- | --- | --- |
| id | uuid | no | `gen_random_uuid()` |
| order_id | uuid | no | |
| courier_provider | varchar(100) | yes | |
| tracking_number | varchar(150) | yes | |
| tracking_url | text | yes | |
| shipment_status | varchar(30) | no | `pending` |
| shipped_at | timestamptz | yes | |
| delivered_at | timestamptz | yes | |
| estimated_delivery | date | yes | |
| provider_response | jsonb | yes | |
| created_at | timestamptz | no | `now()` |
| updated_at | timestamptz | no | `now()` |

## Foreign keys inside store

| From | Column | To |
| --- | --- | --- |
| cart_items | cart_id | carts.id |
| cart_items | product_id | products.id |
| cart_items | variant_id | product_variants.id |
| categories | parent_id | categories.id |
| inventory | product_id | products.id |
| inventory | variant_id | product_variants.id |
| inventory_transactions | inventory_id | inventory.id |
| order_items | order_id | orders.id |
| order_items | product_id | products.id |
| order_items | variant_id | product_variants.id |
| order_items | recommendation_id | user_recommendations.id |
| orders | cart_id | carts.id |
| payments | order_id | orders.id |
| product_images | product_id | products.id |
| product_images | variant_id | product_variants.id |
| product_remedy_mappings | product_id | products.id |
| product_remedy_mappings | remedy_key_id | remedy_keys.id |
| product_variants | product_id | products.id |
| products | category_id | categories.id |
| shipments | order_id | orders.id |
| user_recommendations | remedy_key_id | remedy_keys.id |

## HTTP surface on KundaliEngine.API

Anonymous catalogue:

- `GET /api/store/categories`
- `GET /api/store/products` (`category`, `search`, `productType`, `isFeatured`, `inStockOnly`, `minimumPrice`, `maximumPrice`, `pageNumber`, `pageSize`, `sortBy`, `sortDirection`)
- `GET /api/store/products/{slug}`

Authenticated with the same HoraLife JWT (`ClaimTypes.NameIdentifier` = user id):

- Cart: `GET /api/store/cart`, `POST /api/store/cart/items`, `PUT /api/store/cart/items/{id}`, `DELETE /api/store/cart/items/{id}`, `DELETE /api/store/cart`
- Addresses: `GET/POST /api/store/addresses`, `GET/PUT/DELETE /api/store/addresses/{id}`, `PUT /api/store/addresses/{id}/default`
- `POST /api/store/checkout/preview`
- `POST /api/store/checkout/revalidate` (no body; 409 when the cart must be previewed again)
- `POST /api/store/orders`
- `POST /api/store/orders/{orderId}/items` (no body; snapshots the cart)
- `POST /api/store/orders/{orderId}/inventory/reserve`
- `POST /api/store/orders/{orderId}/inventory/confirm` (deducts stock + reserved; sets payment paid / order confirmed; idempotent)
- `POST /api/store/orders/{orderId}/inventory/release` (body optional `{ "reason": "cancelled" | "payment_failed" | "reservation_expired" }`)
- `POST /api/store/orders/{orderId}/mark-paid` (status hook → confirm)
- `POST /api/store/orders/{orderId}/cancel` (status hook → release)
- `POST /api/store/orders/{orderId}/cart/convert`

Admin only (`Authorize(Roles = "Admin")`):

- `GET /api/store/inventory?lowStockOnly=false`
- `GET /api/store/inventory/{inventoryId}/transactions`
- `POST /api/store/inventory/{inventoryId}/adjust` (`quantityChange`, optional `reason`, optional `transactionType` = `stock_added` | `manual_adjustment`)
- `PUT /api/store/inventory/{inventoryId}/reorder-level`
- `POST /api/store/inventory/release-expired?maxAgeHours=24`

Background sweeper (`StoreInventory` config: `ReservationExpiryHours`, `ExpirySweepMinutes`) auto-releases stale pending reservations.

Not exposed, so a storefront must not call them yet:

- reading an order or listing a customer's orders
- creating or capturing `store.payments`
- shipments
- remedy recommendations

Starter catalogue rows live in `horalife_store_master_seed.sql` in this folder. Image URLs in that seed are local `/assets/store/products/...` paths and are not files in the API.

Inventory lifecycle verification SQL: `horalife_store_inventory_reservation_backend/Database/inventory_lifecycle_verification.sql`.

