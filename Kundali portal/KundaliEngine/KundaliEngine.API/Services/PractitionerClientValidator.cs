using KundaliEngine.API.DTOs.Practitioner;

namespace KundaliEngine.API.Services;

public static class PractitionerClientValidator
{
    public static string? Validate(PractitionerClientRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName)) return "Client name is required.";
        if (string.IsNullOrWhiteSpace(request.Gender)) return "Gender is required.";
        if (request.DateOfBirth == default || request.DateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow)) return "A valid birth date is required.";
        if (string.IsNullOrWhiteSpace(request.BirthPlace)) return "Birth place is required.";
        if (request.Status is not ("New" or "Consultation Pending" or "Completed")) return "Choose a valid client status.";
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) && string.IsNullOrWhiteSpace(request.Email)) return "Enter a phone number, an email address, or both.";
        if (!string.IsNullOrWhiteSpace(request.Email) && !System.Net.Mail.MailAddress.TryCreate(request.Email.Trim(), out _)) return "Enter a valid email address.";
        if (request.FullName.Trim().Length > 150 || request.BirthPlace.Trim().Length > 200) return "Name or birth place is too long.";
        if (request.PhoneNumber?.Trim().Length > 30 || request.Email?.Trim().Length > 150) return "Contact details are too long.";
        if (request.Topic?.Trim().Length > 200) return "Topic is too long.";
        if (request.Notes?.Trim().Length > 2000) return "Client notes are too long.";
        if (request.Latitude.HasValue != request.Longitude.HasValue) return "Enter both latitude and longitude, or leave both blank.";
        if ((request.Latitude is < -90 or > 90) || (request.Longitude is < -180 or > 180)) return "Location coordinates are invalid.";
        return null;
    }
}
