# HoraLife Store Backend Entities

This package contains EF Core entity classes matching the PostgreSQL `store` schema.

## Included

- Category
- Product
- ProductVariant
- ProductImage
- Inventory
- InventoryTransaction
- RemedyKey
- ProductRemedyMapping
- UserRecommendation
- Cart
- CartItem
- CustomerAddress
- Order
- OrderItem
- Payment
- Shipment
- Store enums

## Namespace

The default namespace is:

`HoraLife.Store.Domain`

Change it to match your existing solution structure if needed.

## Important EF Core note

Several PostgreSQL columns store enum values as lowercase snake_case strings, for example:

- `direct_store`
- `partially_refunded`
- `order_reserved`

Your `DbContext` Fluent Configuration should add enum-to-string value converters that preserve these exact database values.

The following external relationships were intentionally left without navigation properties because your existing table names and entity types are not yet confirmed:

- `user_id`
- `kundali_id`
- `recommended_kundali_id`

Add those foreign keys after aligning these entities with your existing HoraLife `User` and `Kundali` entities.

## PostgreSQL JSONB

JSONB columns use `System.Text.Json.JsonDocument`.

## Target

Designed for modern .NET / EF Core with PostgreSQL and Npgsql.
