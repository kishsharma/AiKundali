namespace KundaliEngine.API.Services;

public enum PractitionerAppointmentCancellationResult
{
    Cancelled,
    AlreadyCancelled,
    Conflict
}

public static class PractitionerAppointmentCancellation
{
    public static PractitionerAppointmentCancellationResult Evaluate(string? status)
    {
        if (string.Equals(status?.Trim(), "Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            return PractitionerAppointmentCancellationResult.AlreadyCancelled;
        }

        if (string.Equals(status?.Trim(), "New", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status?.Trim(), "Consultation Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status?.Trim(), "Scheduled", StringComparison.OrdinalIgnoreCase))
        {
            return PractitionerAppointmentCancellationResult.Cancelled;
        }

        return PractitionerAppointmentCancellationResult.Conflict;
    }
}
