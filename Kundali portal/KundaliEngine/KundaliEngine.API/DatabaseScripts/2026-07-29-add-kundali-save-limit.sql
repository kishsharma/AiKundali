ALTER TABLE public."Users"
ADD COLUMN IF NOT EXISTS "KundaliSaveLimit" integer;

UPDATE public."Users"
SET "KundaliSaveLimit" = 5
WHERE "KundaliSaveLimit" IS NULL;

ALTER TABLE public."Users"
ALTER COLUMN "KundaliSaveLimit" SET DEFAULT 5;

ALTER TABLE public."Users"
ALTER COLUMN "KundaliSaveLimit" SET NOT NULL;
