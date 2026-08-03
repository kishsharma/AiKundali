-- Verify that no user currently has multiple defaults
-- for the same address type.

SELECT
    user_id,
    address_type,
    COUNT(*) AS default_count
FROM store.customer_addresses
WHERE is_default = TRUE
GROUP BY
    user_id,
    address_type
HAVING COUNT(*) > 1;

-- Run the CREATE INDEX only after the query above
-- returns no rows.

CREATE UNIQUE INDEX IF NOT EXISTS
    ux_customer_addresses_user_type_default
ON store.customer_addresses
    (user_id, address_type)
WHERE is_default = TRUE;
