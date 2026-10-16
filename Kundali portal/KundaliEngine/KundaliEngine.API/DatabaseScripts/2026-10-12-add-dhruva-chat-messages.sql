-- Dhruva question and answer history for a saved kundali.
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

CREATE TABLE IF NOT EXISTS public."DhruvaChatMessages" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "KundaliId" uuid NOT NULL,
    "Question" character varying(2000) NOT NULL,
    "Response" text NOT NULL,
    "AskedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_DhruvaChatMessages" PRIMARY KEY ("Id")
);

CREATE INDEX IF NOT EXISTS "IX_DhruvaChatMessages_UserId_KundaliId_AskedAt"
    ON public."DhruvaChatMessages" ("UserId", "KundaliId", "AskedAt");

COMMIT;
