-- Run once against the HoraLife PostgreSQL database.

-- 1. Prevent impossible inventory values.
DO $$
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_constraint
        WHERE conname =
            'ck_inventory_valid_quantities'
          AND conrelid =
            'store.inventory'::regclass
    )
    THEN
        ALTER TABLE store.inventory
        ADD CONSTRAINT ck_inventory_valid_quantities
        CHECK
        (
            stock_quantity >= 0
            AND reserved_quantity >= 0
            AND reserved_quantity <= stock_quantity
        );
    END IF;
END $$;

-- 2. A retry must not reserve the same inventory row twice
--    for the same order.
CREATE UNIQUE INDEX IF NOT EXISTS
    ux_inventory_transactions_order_reserved
ON store.inventory_transactions
(
    reference_id,
    inventory_id
)
WHERE reference_type = 'order'
  AND transaction_type = 'order_reserved';
