UPDATE public."AuthOtpChallenges"
SET
    "LastSentAt" = "LastSentAt" - INTERVAL '5 hours 30 minutes',
    "CreatedAt" = "CreatedAt" - INTERVAL '5 hours 30 minutes',
    "ExpiresAt" = "ExpiresAt" - INTERVAL '5 hours 30 minutes',
    "VerifiedAt" = CASE WHEN "VerifiedAt" IS NULL THEN NULL ELSE "VerifiedAt" - INTERVAL '5 hours 30 minutes' END,
    "ConsumedAt" = CASE WHEN "ConsumedAt" IS NULL THEN NULL ELSE "ConsumedAt" - INTERVAL '5 hours 30 minutes' END,
    "RegistrationTokenExpiresAt" = CASE WHEN "RegistrationTokenExpiresAt" IS NULL THEN NULL ELSE "RegistrationTokenExpiresAt" - INTERVAL '5 hours 30 minutes' END
WHERE "LastSentAt" > (now() AT TIME ZONE 'utc') + INTERVAL '1 minute';