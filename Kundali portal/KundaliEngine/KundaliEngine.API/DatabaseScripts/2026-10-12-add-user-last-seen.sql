-- Last time the API saw this user's token. Used to show who is live.
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "LastSeenAt" timestamp with time zone;

COMMIT;

SELECT
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'Users'
  AND column_name = 'LastSeenAt';
