-- Payment / subscription schema for VM
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

-- Existing user plan columns (needed if this VM never got the Aug entitlements migration)
ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "PlanCode" character varying(50);

UPDATE public."Users"
SET "PlanCode" = 'Free'
WHERE "PlanCode" IS NULL OR btrim("PlanCode") = '';

ALTER TABLE public."Users"
    ALTER COLUMN "PlanCode" SET DEFAULT 'Free';

ALTER TABLE public."Users"
    ALTER COLUMN "PlanCode" SET NOT NULL;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "PlanExpiresAt" timestamp with time zone;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "KundaliSaveLimit" integer;

UPDATE public."Users"
SET "KundaliSaveLimit" = 5
WHERE "KundaliSaveLimit" IS NULL;

ALTER TABLE public."Users"
    ALTER COLUMN "KundaliSaveLimit" SET DEFAULT 5;

ALTER TABLE public."Users"
    ALTER COLUMN "KundaliSaveLimit" SET NOT NULL;

-- New payment ledger
CREATE TABLE IF NOT EXISTS public."UserSubscriptions" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "PlanCode" character varying(50) NOT NULL,
    "BillingCycle" character varying(20) NOT NULL,
    "Status" character varying(40) NOT NULL,
    "RazorpaySubscriptionId" character varying(80) NULL,
    "RazorpayOrderId" character varying(80) NULL,
    "RazorpayPaymentId" character varying(80) NULL,
    "CurrentPeriodEnd" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_UserSubscriptions" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserSubscriptions_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_UserId"
    ON public."UserSubscriptions" ("UserId");

CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_RazorpaySubscriptionId"
    ON public."UserSubscriptions" ("RazorpaySubscriptionId");

CREATE INDEX IF NOT EXISTS "IX_UserSubscriptions_RazorpayOrderId"
    ON public."UserSubscriptions" ("RazorpayOrderId");

-- Record EF migration if that history table exists
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
        WHERE "MigrationId" = '20260922120000_AddUserSubscriptions'
    ) THEN
        INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
        VALUES ('20260922120000_AddUserSubscriptions', '8.0.0');
    END IF;
END $$;

COMMIT;

-- Quick check
SELECT
    to_regclass('public."UserSubscriptions"') AS user_subscriptions_table,
    COUNT(*) AS subscription_rows
FROM public."UserSubscriptions";
