using KundaliEngine.API.DTOs.Practitioner;
using KundaliEngine.API.Services;

namespace KundaliEngine.Muhurtha.Tests;

public sealed class PractitionerCrmValidationTests
{
    [Fact]
    public void Client_requires_at_least_one_contact_method()
    {
        var request = ValidClient();
        Assert.Contains("phone number", PractitionerClientValidator.Validate(request));
        request.Email = "maya@example.com";
        Assert.Null(PractitionerClientValidator.Validate(request));
        request.PhoneNumber = "+1 555 0100";
        request.Email = null;
        Assert.Null(PractitionerClientValidator.Validate(request));
    }

    [Fact]
    public void Client_rejects_malformed_email()
    {
        var request = ValidClient();
        request.Email = "not-an-email";
        Assert.Contains("valid email", PractitionerClientValidator.Validate(request));
    }

    [Fact]
    public void Appointment_resolves_local_window_using_saved_zone()
    {
        var resolved = PractitionerAppointmentTimeResolver.TryResolve(
            "2026-01-02T10:00", "2026-01-02T11:00", "Asia/Kolkata",
            out var startUtc, out var endUtc, out var error);

        Assert.True(resolved, error);
        Assert.Equal(new DateTime(2026, 1, 2, 4, 30, 0, DateTimeKind.Utc), startUtc);
        Assert.Equal(new DateTime(2026, 1, 2, 5, 30, 0, DateTimeKind.Utc), endUtc);
    }

    [Fact]
    public void Appointment_rejects_reversed_window_and_dst_gap()
    {
        Assert.False(PractitionerAppointmentTimeResolver.TryResolve(
            "2026-01-02T11:00", "2026-01-02T10:00", "Asia/Kolkata", out _, out _, out var reversedError));
        Assert.Contains("after start", reversedError);

        Assert.False(PractitionerAppointmentTimeResolver.TryResolve(
            "2026-03-08T02:30", "2026-03-08T03:30", "America/New_York", out _, out _, out var dstError));
        Assert.Contains("daylight-saving", dstError);
    }

    [Theory]
    [InlineData("New", PractitionerAppointmentCancellationResult.Cancelled)]
    [InlineData("Consultation Pending", PractitionerAppointmentCancellationResult.Cancelled)]
    [InlineData("Scheduled", PractitionerAppointmentCancellationResult.Cancelled)]
    [InlineData("Cancelled", PractitionerAppointmentCancellationResult.AlreadyCancelled)]
    [InlineData("Completed", PractitionerAppointmentCancellationResult.Conflict)]
    public void Appointment_cancellation_only_changes_active_consultations(
        string status,
        PractitionerAppointmentCancellationResult expected)
    {
        Assert.Equal(expected, PractitionerAppointmentCancellation.Evaluate(status));
    }

    private static PractitionerClientRequest ValidClient() => new()
    {
        FullName = "Maya", Gender = "Female", DateOfBirth = new DateOnly(1990, 5, 8),
        TimeOfBirth = new TimeOnly(10, 30), BirthPlace = "Bengaluru"
    };
}
