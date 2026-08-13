-- Portal notes and voice notes storage.
-- Page-level notes use ContextId = '00000000-0000-0000-0000-000000000000'.
-- Report-linked notes use the saved Kundali or saved Compatibility report ID.

CREATE TABLE IF NOT EXISTS public."PortalNotes" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "ContextType" character varying(50) NOT NULL,
    "ContextId" uuid NOT NULL,
    "Content" text NOT NULL,
    "ContextTitle" character varying(200) NULL,
    "ContextDetailsJson" text NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_PortalNotes" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PortalNotes_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE NO ACTION
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_PortalNotes_UserId_ContextType_ContextId"
    ON public."PortalNotes" ("UserId", "ContextType", "ContextId");

CREATE TABLE IF NOT EXISTS public."PortalNoteVoiceRecordings" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "ContextType" character varying(50) NOT NULL,
    "ContextId" uuid NOT NULL,
    "FileName" character varying(255) NOT NULL,
    "ContentType" character varying(100) NOT NULL,
    "StoragePath" character varying(500) NOT NULL,
    "DurationSeconds" integer NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_PortalNoteVoiceRecordings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PortalNoteVoiceRecordings_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users" ("Id") ON DELETE NO ACTION
);

CREATE INDEX IF NOT EXISTS "IX_PortalNoteVoiceRecordings_UserId_ContextType_ContextId"
    ON public."PortalNoteVoiceRecordings" ("UserId", "ContextType", "ContextId");

CREATE INDEX IF NOT EXISTS "IX_PortalNoteVoiceRecordings_CreatedAt"
    ON public."PortalNoteVoiceRecordings" ("CreatedAt");

