using System.Security.Claims;
using KundaliEngine.API.Controllers;
using KundaliEngine.API.DTOs.Practitioner;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using KundaliEngine.Infrastructure.Migrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var connectionString = Environment.GetEnvironmentVariable("CRM_SCHEMA_TEST_CONNECTION");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("[SKIP] Set CRM_SCHEMA_TEST_CONNECTION to run the isolated CRM schema checks.");
    return 0;
}

await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync();

await ResetCrmSchemaAsync(connection);
await CreateBaseTablesAsync(connection);
await ApplyCrmSchemaAsync(connection);
await AssertColumnExistsAsync(connection, "PractitionerClients", "Status");
await AssertColumnExistsAsync(connection, "PractitionerAppointments", "CompletedAtUtc");
await AssertIndexExistsAsync(connection, "IX_AppNotifications_DedupeKey");
Console.WriteLine("[PASS] Fresh schema creates all CRM tables, expanded columns and indexes.");

await ResetCrmSchemaAsync(connection);
await CreateBaseTablesAsync(connection);
await ApplyCrmSchemaInitialAsync(connection);
var userId = Guid.NewGuid();
var otherUserId = Guid.NewGuid();
var clientId = Guid.NewGuid();
var appointmentId = Guid.NewGuid();
var updatedAt = new DateTime(2026, 6, 15, 4, 30, 0, DateTimeKind.Utc);
await ExecuteAsync(connection, "INSERT INTO public.\"Users\" (\"Id\") VALUES (@userId);", ("userId", userId));
await ExecuteAsync(connection, "INSERT INTO public.\"Users\" (\"Id\") VALUES (@userId);", ("userId", otherUserId));
await ExecuteAsync(connection, """
    INSERT INTO public."PractitionerClients"
        ("Id", "UserId", "FullName", "Gender", "DateOfBirth", "TimeOfBirth", "BirthPlace", "IsArchived", "CreatedAt", "UpdatedAt")
    VALUES (@clientId, @userId, 'Asha', 'Female', DATE '1990-01-01', TIME '10:30', 'Bengaluru', false, @updatedAt, @updatedAt);
    """, ("clientId", clientId), ("userId", userId), ("updatedAt", updatedAt));
await ExecuteAsync(connection, """
    INSERT INTO public."PractitionerAppointments"
        ("Id", "UserId", "ClientId", "StartsAtUtc", "EndsAtUtc", "TimeZoneId", "Status", "ReminderVersion", "CreatedAt", "UpdatedAt")
    VALUES (@appointmentId, @userId, @clientId, @startsAt, @endsAt, 'Asia/Kolkata', 'Scheduled', 1, @updatedAt, @updatedAt);
    """, ("appointmentId", appointmentId), ("userId", userId), ("clientId", clientId),
    ("startsAt", updatedAt.AddDays(2)), ("endsAt", updatedAt.AddDays(2).AddHours(1)), ("updatedAt", updatedAt));

await ApplyCrmSchemaExpansionAsync(connection);
var appointmentState = await ReadAppointmentUpgradeStateAsync(connection, appointmentId);
if (appointmentState.Status != "Consultation Pending" || !appointmentState.CompletedAtMatchesUpdatedAt)
{
    throw new InvalidOperationException("CRM expansion did not preserve and normalize the pre-expansion appointment.");
}

await ApplyCrmSchemaAsync(connection);
await AssertColumnExistsAsync(connection, "PractitionerClients", "Status");
await AssertColumnExistsAsync(connection, "PractitionerAppointments", "CompletedAtUtc");
var clientStatus = await ReadClientStatusAsync(connection, clientId);
if (clientStatus != "New") throw new InvalidOperationException("Existing clients did not receive the expected default status.");
Console.WriteLine("[PASS] Upgrade preserves client/appointment rows, normalizes legacy status, and is repeatable.");
await VerifyControllerOwnershipAsync(connectionString, userId, otherUserId, clientId, appointmentId);
return 0;

static async Task VerifyControllerOwnershipAsync(string connectionString, Guid ownerUserId, Guid otherUserId, Guid clientId, Guid appointmentId)
{
    var options = new DbContextOptionsBuilder<KundaliDbContext>().UseNpgsql(connectionString).Options;
    await using var db = new KundaliDbContext(options);
    var premiumPlus = new TestEntitlementService(true);

    var clientController = new PractitionerClientsController(db, premiumPlus);
    SetActor(clientController, otherUserId);
    var client = await clientController.GetById(clientId, CancellationToken.None);
    if (client.Result is not NotFoundResult)
        throw new InvalidOperationException("A CRM account could read a client owned by another account.");

    var request = new PractitionerClientRequest
    {
        FullName = "Changed by another account",
        Gender = "Female",
        DateOfBirth = new DateOnly(1990, 1, 1),
        TimeOfBirth = new TimeOnly(10, 30),
        BirthPlace = "Bengaluru",
        Email = "test@example.com"
    };
    var update = await clientController.Update(clientId, request, CancellationToken.None);
    if (update.Result is not NotFoundResult)
        throw new InvalidOperationException("A CRM account could update a client owned by another account.");

    var appointmentController = new PractitionerAppointmentsController(db, premiumPlus);
    SetActor(appointmentController, otherUserId);
    var cancellation = await appointmentController.Cancel(appointmentId, CancellationToken.None);
    if (cancellation is not NotFoundResult)
        throw new InvalidOperationException("A CRM account could cancel an appointment owned by another account.");

    var freePlan = new PractitionerClientsController(db, new TestEntitlementService(false));
    SetActor(freePlan, ownerUserId);
    var denied = await freePlan.GetAll(null, false, CancellationToken.None);
    if (denied.Result is not ObjectResult { StatusCode: StatusCodes.Status402PaymentRequired })
        throw new InvalidOperationException("A non-Premium Plus account was not denied CRM access.");

    Console.WriteLine("[PASS] CRM API enforces client ownership, appointment ownership, and Premium Plus entitlement.");
}

static void SetActor(ControllerBase controller, Guid userId)
{
    var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "crm-test");
    controller.ControllerContext = new ControllerContext
    {
        HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
    };
}

static async Task ApplyCrmSchemaAsync(NpgsqlConnection connection)
{
    await ApplyCrmSchemaInitialAsync(connection);
    await ApplyCrmSchemaExpansionAsync(connection);
}

static Task ApplyCrmSchemaInitialAsync(NpgsqlConnection connection) => ExecuteAsync(connection, PractitionerCrmSchemaSql.Initial);

static Task ApplyCrmSchemaExpansionAsync(NpgsqlConnection connection) => ExecuteAsync(connection, PractitionerCrmSchemaSql.Expansion);

static async Task ResetCrmSchemaAsync(NpgsqlConnection connection) => await ExecuteAsync(connection, """
    DROP TABLE IF EXISTS public."AppNotifications" CASCADE;
    DROP TABLE IF EXISTS public."PractitionerAppointments" CASCADE;
    DROP TABLE IF EXISTS public."PractitionerClients" CASCADE;
    ALTER TABLE IF EXISTS public."Kundalis" DROP COLUMN IF EXISTS "ClientId" CASCADE;
    """);

static async Task CreateBaseTablesAsync(NpgsqlConnection connection) => await ExecuteAsync(connection, """
    CREATE TABLE IF NOT EXISTS public."Users" (
        "Id" uuid PRIMARY KEY,
        "FullName" character varying(150) NOT NULL DEFAULT '',
        "Email" character varying(150) NULL,
        "PhoneNumber" character varying(20) NULL,
        "PreferredLanguage" character varying(2) NULL,
        "PracticeProfile" text NULL,
        "PasswordHash" character varying(500) NULL,
        "AuthProvider" character varying(50) NOT NULL DEFAULT 'Local',
        "GoogleSubject" character varying(128) NULL,
        "ProfileImageUrl" character varying(1000) NULL,
        "Role" character varying(50) NOT NULL DEFAULT 'User',
        "PlanCode" character varying(50) NOT NULL DEFAULT 'Free',
        "PlanExpiresAt" timestamp with time zone NULL,
        "IsActive" boolean NOT NULL DEFAULT true,
        "KundaliSaveLimit" integer NOT NULL DEFAULT 5,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
        "UpdatedAt" timestamp with time zone NULL,
        "LastLoginAt" timestamp with time zone NULL,
        "LastSeenAt" timestamp with time zone NULL,
        "EmailVerifiedAt" timestamp with time zone NULL,
        "PhoneVerifiedAt" timestamp with time zone NULL
    );
    CREATE TABLE IF NOT EXISTS public."Kundalis" ("Id" uuid PRIMARY KEY);
    """);

static async Task AssertColumnExistsAsync(NpgsqlConnection connection, string table, string column)
{
    var exists = await ReadBooleanAsync(connection, """
        SELECT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = @table AND column_name = @column);
        """, ("table", table), ("column", column));
    if (!exists) throw new InvalidOperationException($"Expected {table}.{column} to exist.");
}

static async Task AssertIndexExistsAsync(NpgsqlConnection connection, string index)
{
    var exists = await ReadBooleanAsync(connection, "SELECT EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = @index);", ("index", index));
    if (!exists) throw new InvalidOperationException($"Expected index {index} to exist.");
}

static async Task<string?> ReadClientStatusAsync(NpgsqlConnection connection, Guid clientId)
{
    await using var command = new NpgsqlCommand("SELECT \"Status\" FROM public.\"PractitionerClients\" WHERE \"Id\" = @clientId;", connection);
    command.Parameters.AddWithValue("clientId", clientId);
    return (string?)await command.ExecuteScalarAsync();
}

static async Task<(string Status, bool CompletedAtMatchesUpdatedAt)> ReadAppointmentUpgradeStateAsync(NpgsqlConnection connection, Guid appointmentId)
{
    await using var command = new NpgsqlCommand("SELECT \"Status\", \"CompletedAtUtc\" = \"UpdatedAt\" FROM public.\"PractitionerAppointments\" WHERE \"Id\" = @appointmentId;", connection);
    command.Parameters.AddWithValue("appointmentId", appointmentId);
    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync()) throw new InvalidOperationException("Expected the legacy appointment row to remain after schema upgrade.");
    return (reader.GetString(0), reader.GetBoolean(1));
}

static async Task<bool> ReadBooleanAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters)
{
    await using var command = new NpgsqlCommand(sql, connection);
    foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
    return (bool)(await command.ExecuteScalarAsync() ?? false);
}

static async Task ExecuteAsync(NpgsqlConnection connection, string sql, params (string Name, object Value)[] parameters)
{
    await using var command = new NpgsqlCommand(sql, connection);
    foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value);
    await command.ExecuteNonQueryAsync();
}

sealed class TestEntitlementService(bool isPlus) : IEntitlementService
{
    public string GetEffectivePlanCode(User user) => isPlus ? "PremiumPlus" : "Free";
    public bool IsPaid(User user) => isPlus;
    public bool IsPremium(User user) => isPlus;
    public bool IsPlus(User user) => isPlus;
    public int GetDefaultKundaliSaveLimit(string planCode) => isPlus ? 500 : 5;
    public int GetMatchSaveLimit(User user) => isPlus ? 200 : 2;
    public EntitlementSnapshot GetSnapshot(User user) => new() { PlanCode = GetEffectivePlanCode(user), IsPaid = isPlus, IsPremium = isPlus, IsPlus = isPlus };
    public void ApplyPlan(User user, string planCode, DateTimeOffset? expiresAt)
    {
        user.PlanCode = planCode;
        user.PlanExpiresAt = expiresAt;
    }
}
