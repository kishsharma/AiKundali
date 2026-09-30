-- Optional family identity fields on saved Kundalis
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

ALTER TABLE public."Kundalis"
    ADD COLUMN IF NOT EXISTS "Surname" character varying(150);

ALTER TABLE public."Kundalis"
    ADD COLUMN IF NOT EXISTS "FatherName" character varying(150);

ALTER TABLE public."Kundalis"
    ADD COLUMN IF NOT EXISTS "MotherName" character varying(150);

ALTER TABLE public."Kundalis"
    ADD COLUMN IF NOT EXISTS "Gotra" character varying(100);

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = '__EFMigrationsHistory'
    )
    AND NOT EXISTS (
        SELECT 1
        FROM public."__EFMigrationsHistory"
        WHERE "MigrationId" = '20260923100000_AddKundaliFamilyDetails'
    ) THEN
        INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260923100000_AddKundaliFamilyDetails', '8.0.0');
    END IF;
END $$;

COMMIT;

SELECT
    column_name,
    data_type,
    character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'Kundalis'
  AND column_name IN ('Surname', 'FatherName', 'MotherName', 'Gotra')
ORDER BY column_name;
