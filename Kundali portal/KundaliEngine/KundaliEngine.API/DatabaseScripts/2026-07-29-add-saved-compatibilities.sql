CREATE TABLE IF NOT EXISTS public."SavedCompatibilities" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "PersonAName" character varying(150) NOT NULL,
    "PersonAGender" character varying(20) NULL,
    "PersonADateOfBirth" date NOT NULL,
    "PersonATimeOfBirth" time(6) without time zone NOT NULL,
    "PersonABirthPlace" character varying(200) NOT NULL,
    "PersonALatitude" numeric(9,6) NULL,
    "PersonALongitude" numeric(9,6) NULL,
    "PersonBName" character varying(150) NOT NULL,
    "PersonBGender" character varying(20) NULL,
    "PersonBDateOfBirth" date NOT NULL,
    "PersonBTimeOfBirth" time(6) without time zone NOT NULL,
    "PersonBBirthPlace" character varying(200) NOT NULL,
    "PersonBLatitude" numeric(9,6) NULL,
    "PersonBLongitude" numeric(9,6) NULL,
    "OverallCompatibilityScore" integer NOT NULL DEFAULT 0,
    "AshtakootaScore" numeric(5,2) NULL,
    "RequestJson" text NOT NULL,
    "ResultJson" text NOT NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    CONSTRAINT "FK_SavedCompatibilities_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS "IX_SavedCompatibilities_UserId_CreatedAt"
    ON public."SavedCompatibilities" ("UserId", "CreatedAt" DESC);