ALTER TABLE public."Users"
ADD COLUMN IF NOT EXISTS "EmailVerifiedAt" timestamp without time zone NULL;

ALTER TABLE public."Users"
ADD COLUMN IF NOT EXISTS "PhoneVerifiedAt" timestamp without time zone NULL;

CREATE TABLE IF NOT EXISTS public."AuthOtpChallenges" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "Purpose" character varying(30) NOT NULL,
    "Channel" character varying(20) NOT NULL DEFAULT 'Email',
    "DestinationNormalized" character varying(150) NOT NULL,
    "UserId" uuid NULL,
    "CodeHash" character varying(500) NOT NULL,
    "ExpiresAt" timestamp without time zone NOT NULL,
    "VerifiedAt" timestamp without time zone NULL,
    "ConsumedAt" timestamp without time zone NULL,
    "AttemptCount" integer NOT NULL DEFAULT 0,
    "ResendCount" integer NOT NULL DEFAULT 0,
    "LastSentAt" timestamp without time zone NOT NULL,
    "RegistrationTokenHash" character varying(500) NULL,
    "RegistrationTokenExpiresAt" timestamp without time zone NULL,
    "CreatedAt" timestamp without time zone NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),
    "IpAddress" character varying(64) NULL,
    "UserAgent" character varying(500) NULL,
    CONSTRAINT "FK_AuthOtpChallenges_Users_UserId"
        FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS "IX_AuthOtpChallenges_Channel_Purpose_Destination_CreatedAt"
    ON public."AuthOtpChallenges" ("Channel", "Purpose", "DestinationNormalized", "CreatedAt" DESC);

CREATE INDEX IF NOT EXISTS "IX_AuthOtpChallenges_ExpiresAt"
    ON public."AuthOtpChallenges" ("ExpiresAt");

CREATE INDEX IF NOT EXISTS "IX_AuthOtpChallenges_RegistrationTokenExpiresAt"
    ON public."AuthOtpChallenges" ("RegistrationTokenExpiresAt");
