CREATE UNIQUE INDEX IF NOT EXISTS
    ux_order_items_order_product_variant
ON store.order_items
(
    order_id,
    product_id,
    COALESCE(variant_id, '00000000-0000-0000-0000-000000000000'::uuid)
);
