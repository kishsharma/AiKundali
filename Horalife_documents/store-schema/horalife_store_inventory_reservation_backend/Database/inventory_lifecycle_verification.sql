-- Inventory lifecycle verification helpers for HoraLife store.
-- Run after reserve / confirm / release flows against schema store.

-- 1) Current stock vs reserved vs available
SELECT
    i.id,
    p.name AS product_name,
    pv.name AS variant_name,
    i.stock_quantity,
    i.reserved_quantity,
    (i.stock_quantity - i.reserved_quantity) AS available_quantity,
    i.reorder_level,
    CASE
        WHEN (i.stock_quantity - i.reserved_quantity) <= i.reorder_level THEN TRUE
        ELSE FALSE
    END AS is_low_stock
FROM store.inventory i
JOIN store.products p ON p.id = i.product_id
LEFT JOIN store.product_variants pv ON pv.id = i.variant_id
ORDER BY p.name, pv.name NULLS FIRST;

-- 2) Orders that are reserved but not confirmed or released
SELECT
    o.id AS order_id,
    o.order_number,
    o.order_status,
    o.payment_status,
    o.placed_at,
    COUNT(*) FILTER (WHERE t.transaction_type = 'order_reserved') AS reserved_rows,
    COUNT(*) FILTER (WHERE t.transaction_type = 'order_confirmed') AS confirmed_rows,
    COUNT(*) FILTER (WHERE t.transaction_type = 'reservation_released') AS released_rows
FROM store.orders o
JOIN store.inventory_transactions t
  ON t.reference_type = 'order'
 AND t.reference_id = o.id
GROUP BY o.id, o.order_number, o.order_status, o.payment_status, o.placed_at
HAVING COUNT(*) FILTER (WHERE t.transaction_type = 'order_reserved') > 0
   AND COUNT(*) FILTER (WHERE t.transaction_type = 'order_confirmed') = 0
   AND COUNT(*) FILTER (WHERE t.transaction_type = 'reservation_released') = 0
ORDER BY o.placed_at;

-- 3) Ledger for one inventory row (replace :inventory_id)
-- SELECT *
-- FROM store.inventory_transactions
-- WHERE inventory_id = :inventory_id
-- ORDER BY created_at DESC, id DESC;

-- 4) Confirm path expectation
-- After POST /api/store/orders/{id}/inventory/confirm (or /mark-paid):
--   reserved_quantity decreases by reserved qty
--   stock_quantity decreases by same qty
--   inventory_transactions contains order_confirmed for that order

-- 5) Release path expectation
-- After POST /api/store/orders/{id}/inventory/release (or /cancel):
--   reserved_quantity decreases
--   stock_quantity unchanged
--   inventory_transactions contains reservation_released for that order
