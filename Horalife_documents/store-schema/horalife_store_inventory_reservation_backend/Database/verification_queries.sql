-- Replace the values when testing another order or variant.

-- Current Blue Sapphire inventory:
SELECT
    id,
    product_id,
    variant_id,
    stock_quantity,
    reserved_quantity,
    stock_quantity - reserved_quantity
        AS available_quantity
FROM store.inventory
WHERE variant_id =
    '5d78efc8-0d0b-4f3b-843e-61e3c963ca5c';

-- Reservation ledger for current order:
SELECT
    id,
    inventory_id,
    transaction_type,
    quantity_change,
    reference_type,
    reference_id,
    notes,
    created_at
FROM store.inventory_transactions
WHERE reference_type = 'order'
  AND reference_id =
    '379e8f91-9938-4b08-983d-282c4e5a59b7'
ORDER BY created_at;
