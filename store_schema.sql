CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS store;

-- =========================================================
-- PRODUCT CATALOG
-- =========================================================

CREATE TABLE store.categories
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id       UUID NULL REFERENCES store.categories(id),
    name            VARCHAR(150) NOT NULL,
    slug            VARCHAR(160) NOT NULL UNIQUE,
    description     TEXT NULL,
    display_order   INTEGER NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE store.products
(
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    category_id             UUID NOT NULL REFERENCES store.categories(id),
    sku                     VARCHAR(100) NOT NULL UNIQUE,
    slug                    VARCHAR(180) NOT NULL UNIQUE,
    name                    VARCHAR(250) NOT NULL,
    short_description       VARCHAR(500),
    description             TEXT,
    product_type            VARCHAR(40) NOT NULL,
    base_price              NUMERIC(12,2) NOT NULL DEFAULT 0,
    compare_at_price        NUMERIC(12,2),
    currency                CHAR(3) NOT NULL DEFAULT 'INR',

    -- Useful for gemstones, yantras and category-specific properties.
    attributes              JSONB NOT NULL DEFAULT '{}'::JSONB,

    requires_consultation   BOOLEAN NOT NULL DEFAULT FALSE,
    is_featured             BOOLEAN NOT NULL DEFAULT FALSE,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,

    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_product_type CHECK
    (
        product_type IN
        (
            'gemstone',
            'yantra',
            'rudraksha',
            'puja_item',
            'remedy_kit',
            'other'
        )
    ),

    CONSTRAINT ck_product_prices CHECK
    (
        base_price >= 0
        AND (compare_at_price IS NULL OR compare_at_price >= 0)
    )
);

CREATE TABLE store.product_variants
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id          UUID NOT NULL
                        REFERENCES store.products(id) ON DELETE CASCADE,

    sku                 VARCHAR(100) NOT NULL UNIQUE,
    name                VARCHAR(200) NOT NULL,

    -- Example:
    -- {"weightCarat": 5.25, "grade": "Premium", "size": "Medium"}
    attributes          JSONB NOT NULL DEFAULT '{}'::JSONB,

    price               NUMERIC(12,2) NOT NULL,
    compare_at_price    NUMERIC(12,2),
    weight_grams        NUMERIC(10,3),
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,

    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_variant_price CHECK (price >= 0)
);

CREATE TABLE store.product_images
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      UUID NOT NULL
                    REFERENCES store.products(id) ON DELETE CASCADE,
    variant_id      UUID NULL
                    REFERENCES store.product_variants(id) ON DELETE CASCADE,

    image_url       TEXT NOT NULL,
    alt_text        VARCHAR(250),
    display_order   INTEGER NOT NULL DEFAULT 0,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =========================================================
-- INVENTORY
-- =========================================================

CREATE TABLE store.inventory
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id          UUID NOT NULL
                        REFERENCES store.products(id) ON DELETE CASCADE,
    variant_id          UUID NULL
                        REFERENCES store.product_variants(id) ON DELETE CASCADE,

    stock_quantity      INTEGER NOT NULL DEFAULT 0,
    reserved_quantity   INTEGER NOT NULL DEFAULT 0,
    reorder_level       INTEGER NOT NULL DEFAULT 0,
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_inventory_quantity CHECK
    (
        stock_quantity >= 0
        AND reserved_quantity >= 0
        AND reserved_quantity <= stock_quantity
    )
);

CREATE UNIQUE INDEX ux_inventory_product_variant
ON store.inventory
(
    product_id,
    COALESCE
    (
        variant_id,
        '00000000-0000-0000-0000-000000000000'::UUID
    )
);

CREATE TABLE store.inventory_transactions
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    inventory_id        UUID NOT NULL
                        REFERENCES store.inventory(id),

    transaction_type    VARCHAR(30) NOT NULL,
    quantity_change     INTEGER NOT NULL,
    reference_type      VARCHAR(50),
    reference_id        UUID,
    notes               TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_inventory_transaction_type CHECK
    (
        transaction_type IN
        (
            'stock_added',
            'order_reserved',
            'order_confirmed',
            'reservation_released',
            'return_received',
            'manual_adjustment'
        )
    )
);

-- =========================================================
-- KUNDALI AND REMEDY INTEGRATION
-- =========================================================

CREATE TABLE store.remedy_keys
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    remedy_key      VARCHAR(100) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    remedy_type     VARCHAR(40) NOT NULL,
    description     TEXT,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE store.product_remedy_mappings
(
    product_id          UUID NOT NULL
                        REFERENCES store.products(id) ON DELETE CASCADE,
    remedy_key_id       UUID NOT NULL
                        REFERENCES store.remedy_keys(id) ON DELETE CASCADE,

    recommendation_rank INTEGER NOT NULL DEFAULT 100,
    match_strength      NUMERIC(5,2) NOT NULL DEFAULT 50,
    notes               TEXT,

    PRIMARY KEY (product_id, remedy_key_id),

    CONSTRAINT ck_match_strength CHECK
    (
        match_strength BETWEEN 0 AND 100
    )
);

CREATE TABLE store.user_recommendations
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    -- Connect these to your existing HoraLife tables.
    user_id             UUID NOT NULL,
    kundali_id          UUID NOT NULL,

    remedy_key_id       UUID NOT NULL
                        REFERENCES store.remedy_keys(id),

    reason_code         VARCHAR(100),
    display_reason      VARCHAR(500),
    confidence_score    NUMERIC(5,2),
    valid_until         TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_recommendation_confidence CHECK
    (
        confidence_score IS NULL
        OR confidence_score BETWEEN 0 AND 100
    )
);

CREATE INDEX ix_user_recommendations_user
ON store.user_recommendations(user_id, kundali_id);

-- =========================================================
-- CART
-- =========================================================

CREATE TABLE store.carts
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NULL,
    guest_token     VARCHAR(150) NULL,

    status          VARCHAR(20) NOT NULL DEFAULT 'active',
    currency        CHAR(3) NOT NULL DEFAULT 'INR',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_cart_owner CHECK
    (
        user_id IS NOT NULL OR guest_token IS NOT NULL
    ),

    CONSTRAINT ck_cart_status CHECK
    (
        status IN ('active', 'converted', 'abandoned', 'expired')
    )
);

CREATE UNIQUE INDEX ux_active_user_cart
ON store.carts(user_id)
WHERE user_id IS NOT NULL AND status = 'active';

CREATE UNIQUE INDEX ux_active_guest_cart
ON store.carts(guest_token)
WHERE guest_token IS NOT NULL AND status = 'active';

CREATE TABLE store.cart_items
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cart_id         UUID NOT NULL
                    REFERENCES store.carts(id) ON DELETE CASCADE,
    product_id      UUID NOT NULL
                    REFERENCES store.products(id),
    variant_id      UUID NULL
                    REFERENCES store.product_variants(id),

    quantity        INTEGER NOT NULL DEFAULT 1,
    unit_price      NUMERIC(12,2) NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_cart_item_quantity CHECK (quantity > 0),
    CONSTRAINT ck_cart_item_price CHECK (unit_price >= 0)
);

CREATE UNIQUE INDEX ux_cart_product_variant
ON store.cart_items
(
    cart_id,
    product_id,
    COALESCE
    (
        variant_id,
        '00000000-0000-0000-0000-000000000000'::UUID
    )
);

-- =========================================================
-- CUSTOMER ADDRESS
-- =========================================================

CREATE TABLE store.customer_addresses
(
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL,

    address_type    VARCHAR(20) NOT NULL DEFAULT 'shipping',
    full_name       VARCHAR(200) NOT NULL,
    phone           VARCHAR(30) NOT NULL,
    address_line1   VARCHAR(250) NOT NULL,
    address_line2   VARCHAR(250),
    city            VARCHAR(100) NOT NULL,
    state           VARCHAR(100) NOT NULL,
    postal_code     VARCHAR(20) NOT NULL,
    country_code    CHAR(2) NOT NULL DEFAULT 'IN',
    is_default      BOOLEAN NOT NULL DEFAULT FALSE,

    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_address_type CHECK
    (
        address_type IN ('shipping', 'billing')
    )
);

CREATE INDEX ix_customer_addresses_user
ON store.customer_addresses(user_id);

-- =========================================================
-- ORDERS
-- =========================================================

CREATE TABLE store.orders
(
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_number            BIGINT GENERATED ALWAYS AS IDENTITY UNIQUE,

    user_id                 UUID NULL,
    cart_id                 UUID NULL REFERENCES store.carts(id),

    customer_email          VARCHAR(250) NOT NULL,
    customer_phone          VARCHAR(30),

    order_status            VARCHAR(30) NOT NULL DEFAULT 'pending',
    payment_status          VARCHAR(30) NOT NULL DEFAULT 'pending',

    subtotal_amount         NUMERIC(12,2) NOT NULL DEFAULT 0,
    discount_amount         NUMERIC(12,2) NOT NULL DEFAULT 0,
    shipping_amount         NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_amount              NUMERIC(12,2) NOT NULL DEFAULT 0,
    total_amount            NUMERIC(12,2) NOT NULL DEFAULT 0,
    currency                CHAR(3) NOT NULL DEFAULT 'INR',

    -- Address snapshot so historical orders do not change.
    shipping_address        JSONB NOT NULL,
    billing_address         JSONB,

    order_source            VARCHAR(40) NOT NULL DEFAULT 'direct_store',
    recommended_kundali_id  UUID NULL,

    notes                   TEXT,
    placed_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_order_status CHECK
    (
        order_status IN
        (
            'pending',
            'confirmed',
            'processing',
            'shipped',
            'delivered',
            'cancelled',
            'returned'
        )
    ),

    CONSTRAINT ck_order_payment_status CHECK
    (
        payment_status IN
        (
            'pending',
            'authorized',
            'paid',
            'failed',
            'refunded',
            'partially_refunded'
        )
    ),

    CONSTRAINT ck_order_source CHECK
    (
        order_source IN
        (
            'direct_store',
            'kundali_recommendation',
            'compatibility_recommendation',
            'admin'
        )
    )
);

CREATE INDEX ix_orders_user
ON store.orders(user_id, placed_at DESC);

CREATE INDEX ix_orders_status
ON store.orders(order_status, placed_at DESC);

CREATE TABLE store.order_items
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id            UUID NOT NULL
                        REFERENCES store.orders(id) ON DELETE CASCADE,

    product_id          UUID NULL REFERENCES store.products(id),
    variant_id          UUID NULL REFERENCES store.product_variants(id),

    -- Product snapshot.
    product_name        VARCHAR(250) NOT NULL,
    sku                 VARCHAR(100) NOT NULL,
    variant_name        VARCHAR(200),

    quantity            INTEGER NOT NULL,
    unit_price          NUMERIC(12,2) NOT NULL,
    discount_amount     NUMERIC(12,2) NOT NULL DEFAULT 0,
    tax_amount          NUMERIC(12,2) NOT NULL DEFAULT 0,
    line_total          NUMERIC(12,2) NOT NULL,

    recommendation_id   UUID NULL
                        REFERENCES store.user_recommendations(id),

    CONSTRAINT ck_order_item_quantity CHECK (quantity > 0)
);

-- =========================================================
-- PAYMENTS
-- =========================================================

CREATE TABLE store.payments
(
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id                UUID NOT NULL REFERENCES store.orders(id),

    provider                VARCHAR(50) NOT NULL,
    provider_payment_id     VARCHAR(200),
    provider_order_id       VARCHAR(200),
    provider_event_id       VARCHAR(200),

    payment_status          VARCHAR(30) NOT NULL DEFAULT 'pending',
    amount                  NUMERIC(12,2) NOT NULL,
    currency                CHAR(3) NOT NULL DEFAULT 'INR',

    payment_method          VARCHAR(50),
    gateway_response        JSONB,

    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_payment_status CHECK
    (
        payment_status IN
        (
            'pending',
            'authorized',
            'captured',
            'failed',
            'refunded',
            'partially_refunded'
        )
    )
);

CREATE UNIQUE INDEX ux_payment_provider_event
ON store.payments(provider, provider_event_id)
WHERE provider_event_id IS NOT NULL;

-- =========================================================
-- SHIPPING
-- =========================================================

CREATE TABLE store.shipments
(
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    order_id            UUID NOT NULL REFERENCES store.orders(id),

    courier_provider    VARCHAR(100),
    tracking_number     VARCHAR(150),
    tracking_url        TEXT,
    shipment_status     VARCHAR(30) NOT NULL DEFAULT 'pending',

    shipped_at          TIMESTAMPTZ,
    delivered_at        TIMESTAMPTZ,
    estimated_delivery  DATE,
    provider_response   JSONB,

    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT ck_shipment_status CHECK
    (
        shipment_status IN
        (
            'pending',
            'ready_to_ship',
            'shipped',
            'in_transit',
            'out_for_delivery',
            'delivered',
            'failed',
            'returned'
        )
    )
);