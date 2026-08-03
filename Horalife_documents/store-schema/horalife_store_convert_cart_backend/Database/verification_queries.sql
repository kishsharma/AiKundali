-- Current order/cart link:
SELECT
    o.id AS order_id,
    o.order_number,
    o.order_status,
    o.payment_status,
    o.cart_id,
    c.status AS cart_status,
    c.updated_at AS cart_updated_at
FROM store.orders o
JOIN store.carts c
    ON c.id = o.cart_id
WHERE o.id =
    '379e8f91-9938-4b08-983d-282c4e5a59b7';

-- Confirm no active cart remains for the current user:
SELECT
    id,
    user_id,
    status,
    currency,
    created_at,
    updated_at
FROM store.carts
WHERE user_id =
    'd3d441a5-ca17-4806-85d1-8f5c7bd9e4f2'
ORDER BY created_at DESC;

-- Confirm reservation exists before conversion:
SELECT
    id,
    inventory_id,
    transaction_type,
    quantity_change,
    reference_type,
    reference_id,
    created_at
FROM store.inventory_transactions
WHERE reference_type = 'order'
  AND reference_id =
    '379e8f91-9938-4b08-983d-282c4e5a59b7'
  AND transaction_type = 'order_reserved';
