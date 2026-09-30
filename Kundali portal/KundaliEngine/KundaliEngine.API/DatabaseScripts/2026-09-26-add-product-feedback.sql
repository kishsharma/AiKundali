-- Product feedback from finished Kundali, match, Dhruva, and report results
-- Safe to run more than once.
-- Database: horalife (or the VM Postgres database used by KundaliEngine.API)

BEGIN;

CREATE TABLE IF NOT EXISTS public."ProductFeedback" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Surface" character varying(40) NOT NULL,
    "ContextKey" character varying(200) NOT NULL,
    "Sentiment" character varying(20) NOT NULL,
    "Topic" character varying(40) NULL,
    "Note" character varying(400) NULL,
    "PlanCode" character varying(50) NOT NULL,
    "FeedbackDate" date NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ProductFeedback" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ProductFeedback_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductFeedback_User_Surface_Context_Date"
    ON public."ProductFeedback" ("UserId", "Surface", "ContextKey", "FeedbackDate");

CREATE INDEX IF NOT EXISTS "IX_ProductFeedback_CreatedAt"
    ON public."ProductFeedback" ("CreatedAt");

COMMIT;
