ALTER TABLE public."Users"
    ALTER COLUMN "Email" DROP NOT NULL;

DROP INDEX IF EXISTS public."IX_Users_Email";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email"
    ON public."Users" ("Email")
    WHERE "Email" IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_PhoneNumber"
    ON public."Users" ("PhoneNumber")
    WHERE "PhoneNumber" IS NOT NULL;