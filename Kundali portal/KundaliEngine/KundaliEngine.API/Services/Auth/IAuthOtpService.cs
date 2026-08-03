using KundaliEngine.API.DTOs.Auth;

namespace KundaliEngine.API.Services.Auth;

public interface IAuthOtpService
{
    Task<OtpChallengeResponse> StartOtpLoginAsync(StartEmailOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyOtpLoginAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    Task<OtpChallengeResponse> StartOtpRegistrationAsync(StartEmailOtpRequest request, CancellationToken cancellationToken);

    Task<VerifyRegistrationOtpResponse> VerifyOtpRegistrationAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> CompleteRegistrationAsync(CompleteOtpRegistrationRequest request, CancellationToken cancellationToken);
}
