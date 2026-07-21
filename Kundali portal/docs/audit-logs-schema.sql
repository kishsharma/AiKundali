CREATE TABLE IF NOT EXISTS "AuditLogs" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Action" varchar(100) NOT NULL,
    "Category" varchar(50) NOT NULL,
    "UserId" uuid NULL,
    "UserName" varchar(150) NULL,
    "EntityType" varchar(100) NULL,
    "EntityId" varchar(100) NULL,
    "Description" varchar(1000) NULL,
    "IpAddress" varchar(100) NULL,
    "UserAgent" varchar(500) NULL,
    "RequestPath" varchar(300) NULL,
    "Status" varchar(30) NOT NULL DEFAULT 'Success',
    "FailureReason" varchar(1000) NULL
);

CREATE INDEX IF NOT EXISTS "IX_AuditLogs_CreatedAt" ON "AuditLogs" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_UserId" ON "AuditLogs" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Action" ON "AuditLogs" ("Action");
CREATE INDEX IF NOT EXISTS "IX_AuditLogs_Category" ON "AuditLogs" ("Category");

CREATE TABLE IF NOT EXISTS "CommunicationLogs" (
    "Id" uuid PRIMARY KEY,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "Channel" varchar(30) NOT NULL,
    "RecipientEmail" varchar(150) NULL,
    "RecipientMobile" varchar(30) NULL,
    "MessageType" varchar(50) NOT NULL,
    "TemplateName" varchar(100) NULL,
    "Subject" varchar(200) NULL,
    "Status" varchar(30) NOT NULL DEFAULT 'Sent',
    "Provider" varchar(100) NULL,
    "ProviderMessageId" varchar(150) NULL,
    "FailureReason" varchar(1000) NULL,
    "RelatedEntityType" varchar(100) NULL,
    "RelatedEntityId" varchar(100) NULL,
    "TriggeredByUserId" uuid NULL,
    "TriggeredByUserName" varchar(150) NULL
);

CREATE INDEX IF NOT EXISTS "IX_CommunicationLogs_CreatedAt" ON "CommunicationLogs" ("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_CommunicationLogs_Channel" ON "CommunicationLogs" ("Channel");
CREATE INDEX IF NOT EXISTS "IX_CommunicationLogs_Status" ON "CommunicationLogs" ("Status");
CREATE INDEX IF NOT EXISTS "IX_CommunicationLogs_RecipientEmail" ON "CommunicationLogs" ("RecipientEmail");
CREATE INDEX IF NOT EXISTS "IX_CommunicationLogs_RecipientMobile" ON "CommunicationLogs" ("RecipientMobile");
