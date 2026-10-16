-- Google profile photo URL captured at sign-in.
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "ProfileImageUrl" character varying(1000);

COMMIT;

SELECT
    column_name,
    data_type,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'Users'
  AND column_name = 'ProfileImageUrl';
