namespace KundaliEngine.Infrastructure.Migrations;

public static class PractitionerCrmSchemaSql
{
    public const string Initial = """
        CREATE TABLE IF NOT EXISTS public."PractitionerClients" (
            "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
            "UserId" uuid NOT NULL,
            "FullName" character varying(150) NOT NULL,
            "Surname" character varying(150) NULL,
            "FatherName" character varying(150) NULL,
            "MotherName" character varying(150) NULL,
            "Gotra" character varying(100) NULL,
            "Gender" character varying(20) NOT NULL,
            "DateOfBirth" date NOT NULL,
            "TimeOfBirth" time(6) without time zone NOT NULL,
            "BirthPlace" character varying(200) NOT NULL,
            "Latitude" numeric(9,6) NULL,
            "Longitude" numeric(9,6) NULL,
            "PhoneNumber" character varying(30) NULL,
            "Email" character varying(150) NULL,
            "IsArchived" boolean NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_PractitionerClients" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS public."PractitionerAppointments" (
            "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
            "UserId" uuid NOT NULL,
            "ClientId" uuid NOT NULL,
            "KundaliId" uuid NULL,
            "StartsAtUtc" timestamp with time zone NOT NULL,
            "EndsAtUtc" timestamp with time zone NOT NULL,
            "TimeZoneId" character varying(100) NOT NULL,
            "Status" character varying(24) NOT NULL,
            "ReminderLeadMinutes" integer NULL,
            "ReminderVersion" integer NOT NULL,
            "ReminderSentVersion" integer NULL,
            "Notes" character varying(1000) NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "UpdatedAt" timestamp with time zone NOT NULL,
            CONSTRAINT "PK_PractitionerAppointments" PRIMARY KEY ("Id")
        );

        CREATE TABLE IF NOT EXISTS public."AppNotifications" (
            "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
            "UserId" uuid NOT NULL,
            "Type" character varying(40) NOT NULL,
            "Title" character varying(160) NOT NULL,
            "Message" character varying(500) NOT NULL,
            "Link" character varying(300) NOT NULL,
            "DedupeKey" character varying(180) NOT NULL,
            "CreatedAt" timestamp with time zone NOT NULL,
            "ReadAt" timestamp with time zone NULL,
            CONSTRAINT "PK_AppNotifications" PRIMARY KEY ("Id")
        );

        ALTER TABLE public."Kundalis" ADD COLUMN IF NOT EXISTS "ClientId" uuid NULL;

        DO $$ BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PractitionerClients_Users_UserId' AND conrelid = 'public."PractitionerClients"'::regclass) THEN
                ALTER TABLE public."PractitionerClients" ADD CONSTRAINT "FK_PractitionerClients_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PractitionerAppointments_PractitionerClients_ClientId' AND conrelid = 'public."PractitionerAppointments"'::regclass) THEN
                ALTER TABLE public."PractitionerAppointments" ADD CONSTRAINT "FK_PractitionerAppointments_PractitionerClients_ClientId" FOREIGN KEY ("ClientId") REFERENCES public."PractitionerClients" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PractitionerAppointments_Kundalis_KundaliId' AND conrelid = 'public."PractitionerAppointments"'::regclass) THEN
                ALTER TABLE public."PractitionerAppointments" ADD CONSTRAINT "FK_PractitionerAppointments_Kundalis_KundaliId" FOREIGN KEY ("KundaliId") REFERENCES public."Kundalis" ("Id") ON DELETE SET NULL;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_PractitionerAppointments_Users_UserId' AND conrelid = 'public."PractitionerAppointments"'::regclass) THEN
                ALTER TABLE public."PractitionerAppointments" ADD CONSTRAINT "FK_PractitionerAppointments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_AppNotifications_Users_UserId' AND conrelid = 'public."AppNotifications"'::regclass) THEN
                ALTER TABLE public."AppNotifications" ADD CONSTRAINT "FK_AppNotifications_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE CASCADE;
            END IF;
            IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Kundalis_PractitionerClients_ClientId' AND conrelid = 'public."Kundalis"'::regclass) THEN
                ALTER TABLE public."Kundalis" ADD CONSTRAINT "FK_Kundalis_PractitionerClients_ClientId" FOREIGN KEY ("ClientId") REFERENCES public."PractitionerClients" ("Id") ON DELETE SET NULL;
            END IF;
        END $$;

        CREATE INDEX IF NOT EXISTS "IX_PractitionerClients_UserId_IsArchived_FullName" ON public."PractitionerClients" ("UserId", "IsArchived", "FullName");
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_UserId_StartsAtUtc" ON public."PractitionerAppointments" ("UserId", "StartsAtUtc");
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_Status_StartsAtUtc_ReminderSentVersion" ON public."PractitionerAppointments" ("Status", "StartsAtUtc", "ReminderSentVersion");
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_ClientId" ON public."PractitionerAppointments" ("ClientId");
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_KundaliId" ON public."PractitionerAppointments" ("KundaliId");
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_UserId" ON public."PractitionerAppointments" ("UserId");
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_AppNotifications_DedupeKey" ON public."AppNotifications" ("DedupeKey");
        CREATE INDEX IF NOT EXISTS "IX_AppNotifications_UserId_CreatedAt" ON public."AppNotifications" ("UserId", "CreatedAt");
        CREATE INDEX IF NOT EXISTS "IX_Kundalis_ClientId" ON public."Kundalis" ("ClientId");
        """;

    public const string Expansion = """
        ALTER TABLE public."PractitionerClients" ADD COLUMN IF NOT EXISTS "Topic" character varying(200) NULL;
        ALTER TABLE public."PractitionerClients" ADD COLUMN IF NOT EXISTS "Status" character varying(32) NOT NULL DEFAULT 'New';
        ALTER TABLE public."PractitionerClients" ADD COLUMN IF NOT EXISTS "Notes" character varying(2000) NULL;
        ALTER TABLE public."PractitionerAppointments" ADD COLUMN IF NOT EXISTS "Topic" character varying(200) NULL;
        ALTER TABLE public."PractitionerAppointments" ADD COLUMN IF NOT EXISTS "CompletedAtUtc" timestamp with time zone NULL;
        UPDATE public."PractitionerAppointments" SET "Status" = 'Consultation Pending' WHERE "Status" = 'Scheduled';
        UPDATE public."PractitionerAppointments" SET "CompletedAtUtc" = "UpdatedAt" WHERE "Status" = 'Completed' AND "CompletedAtUtc" IS NULL;
        CREATE INDEX IF NOT EXISTS "IX_PractitionerAppointments_UserId_Status_CompletedAtUtc" ON public."PractitionerAppointments" ("UserId", "Status", "CompletedAtUtc");
        """;
}
