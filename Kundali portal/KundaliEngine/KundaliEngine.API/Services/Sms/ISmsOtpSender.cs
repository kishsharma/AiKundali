namespace KundaliEngine.API.Services.Sms;

public interface ISmsOtpSender
{
    Task SendOtpAsync(string phoneNumber, string code, CancellationToken cancellationToken);
}