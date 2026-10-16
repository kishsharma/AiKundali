-- Plan configuration, coupons, and preferred language for the VM.
-- Safe to run more than once.
-- Database: the Postgres database used by KundaliEngine.API (local name: horalife).
-- Numerology does not use the database. Do not run Database/2026-08-02-fix-future-otp-timestamps.sql.

BEGIN;

ALTER TABLE public."Users"
    ADD COLUMN IF NOT EXISTS "PreferredLanguage" character varying(2);

ALTER TABLE public."UserSubscriptions"
    ADD COLUMN IF NOT EXISTS "CouponOfferId" uuid;

ALTER TABLE public."UserSubscriptions"
    ADD COLUMN IF NOT EXISTS "CouponCode" character varying(24);

CREATE TABLE IF NOT EXISTS public."BillingPlanConfigs" (
    "Code" character varying(50) NOT NULL,
    "Name" character varying(80) NOT NULL,
    "Tagline" character varying(160) NOT NULL,
    "MonthlyAmountPaise" integer NOT NULL,
    "YearlyAmountPaise" integer NOT NULL,
    "KundaliSaveLimit" integer NOT NULL,
    "MatchSaveLimit" integer NOT NULL,
    "AiChatEnabled" boolean NOT NULL DEFAULT false,
    "LifeQuestionLimit" integer NULL,
    "CheckoutOpen" boolean NOT NULL DEFAULT false,
    "RazorpayMonthlyPlanId" character varying(80) NULL,
    "RazorpayYearlyPlanId" character varying(80) NULL,
    "UpdatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_BillingPlanConfigs" PRIMARY KEY ("Code")
);

ALTER TABLE public."BillingPlanConfigs"
    ADD COLUMN IF NOT EXISTS "CheckoutOpen" boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'BillingPlanConfigs'
          AND column_name = 'AiChatEnabled'
    ) THEN
        ALTER TABLE public."BillingPlanConfigs" ADD COLUMN "AiChatEnabled" boolean NOT NULL DEFAULT false;
        ALTER TABLE public."BillingPlanConfigs" ADD COLUMN "LifeQuestionLimit" integer NULL;
        UPDATE public."BillingPlanConfigs"
        SET "AiChatEnabled" = ("Code" <> 'Free'),
            "LifeQuestionLimit" = CASE WHEN "Code" = 'Free' THEN 2 ELSE NULL END;
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS public."CouponOffers" (
    "Id" uuid NOT NULL,
    "Name" character varying(80) NOT NULL,
    "Code" character varying(24) NOT NULL,
    "PercentOff" integer NOT NULL,
    "PlanCodes" character varying(80) NOT NULL,
    "BillingCycles" character varying(40) NOT NULL,
    "StartsAt" timestamp with time zone NULL,
    "EndsAt" timestamp with time zone NULL,
    "MaxRedemptions" integer NULL,
    "RedeemedCount" integer NOT NULL DEFAULT 0,
    "Active" boolean NOT NULL DEFAULT true,
    "CreatedAt" timestamp with time zone NOT NULL,
    "UpdatedAt" timestamp with time zone NULL,
    CONSTRAINT "PK_CouponOffers" PRIMARY KEY ("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_CouponOffers_Code"
    ON public."CouponOffers" ("Code");

CREATE TABLE IF NOT EXISTS public."CouponRedemptions" (
    "Id" uuid NOT NULL,
    "CouponOfferId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "UserSubscriptionId" uuid NULL,
    "RazorpayOrderId" character varying(80) NULL,
    "PlanCode" character varying(50) NOT NULL,
    "BillingCycle" character varying(20) NOT NULL,
    "PercentOff" integer NOT NULL,
    "AmountPaise" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_CouponRedemptions" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_CouponRedemptions_CouponOfferId"
    ON public."CouponRedemptions" ("CouponOfferId");

CREATE INDEX IF NOT EXISTS "IX_CouponRedemptions_RazorpayOrderId"
    ON public."CouponRedemptions" ("RazorpayOrderId");

COMMIT;
