-- Google account subject stored on the user row for Sign in with Google
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "GoogleSubject" character varying(128);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_GoogleSubject"
    ON public."Users" ("GoogleSubject")
    WHERE "GoogleSubject" IS NOT NULL;

COMMIT;

SELECT
    column_name,
    data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'Users'
  AND column_name = 'GoogleSubject';
