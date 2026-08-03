-- One cart must never create more than one order.
-- Run once against the HoraLife PostgreSQL database.

CREATE UNIQUE INDEX IF NOT EXISTS
    ux_orders_cart_id
ON store.orders (cart_id)
WHERE cart_id IS NOT NULL;
