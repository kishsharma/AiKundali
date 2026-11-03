using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KundaliEngine.API.DTOs.Practitioner;

public sealed class PractitionerClientRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? Gotra { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    [JsonConverter(typeof(FlexibleTimeOnlyJsonConverter))]
    public TimeOnly TimeOfBirth { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Topic { get; set; }
    public string Status { get; set; } = "New";
    public string? Notes { get; set; }
}

/// <summary>Accepts browser time inputs (HH:mm) as well as full TimeOnly values.</summary>
public sealed class FlexibleTimeOnlyJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] Formats = ["HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss", "HH:mm:ss.FFFFFFF"];

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            TimeOnly.TryParseExact(reader.GetString(), Formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
        {
            return value;
        }

        throw new JsonException("Time of birth must use HH:mm or HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString("HH:mm:ss", CultureInfo.InvariantCulture));
}

public sealed class PractitionerClientDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Surname { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? Gotra { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly TimeOfBirth { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public bool IsArchived { get; set; }
    public string? Topic { get; set; }
    public string Status { get; set; } = "New";
    public string? Notes { get; set; }
    public int LinkedChartCount { get; set; }
    public PractitionerAppointmentDto? NextAppointment { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<PractitionerClientChartDto> Charts { get; set; } = Array.Empty<PractitionerClientChartDto>();
    public IReadOnlyList<PractitionerAppointmentDto> Appointments { get; set; } = Array.Empty<PractitionerAppointmentDto>();
}

public sealed class PractitionerClientChartDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public TimeOnly TimeOfBirth { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
}

public sealed class PractitionerAppointmentRequest
{
    public Guid ClientId { get; set; }
    public Guid? KundaliId { get; set; }
    public string? Topic { get; set; }
    public string Status { get; set; } = "Consultation Pending";
    public string StartsAtLocal { get; set; } = string.Empty;
    public string EndsAtLocal { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public int? ReminderLeadMinutes { get; set; } = 60;
    public string? Notes { get; set; }
}

public sealed class PractitionerAppointmentDto
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public Guid? KundaliId { get; set; }
    public string? KundaliName { get; set; }
    public string? Topic { get; set; }
    public string? ClientName { get; set; }
    public string StartsAtLocal { get; set; } = string.Empty;
    public string EndsAtLocal { get; set; } = string.Empty;
    public DateTime StartsAtUtc { get; set; }
    public DateTime EndsAtUtc { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int? ReminderLeadMinutes { get; set; }
    public string? Notes { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public bool OverlapsExisting { get; set; }
}

public sealed class PractitionerCrmDashboardSummaryDto
{
    public int ActiveClients { get; set; }
    public int AllSavedCharts { get; set; }
    public int LinkedClientCharts { get; set; }
    public int UpcomingConsultations { get; set; }
    public int CompletedConsultationsLast30Days { get; set; }
    public int TodaysConsultations { get; set; }
}

public sealed class AppNotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}
