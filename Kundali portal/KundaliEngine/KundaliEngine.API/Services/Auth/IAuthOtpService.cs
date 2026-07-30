using KundaliEngine.API.DTOs.Auth;

namespace KundaliEngine.API.Services.Auth;

public interface IAuthOtpService
{
    Task<OtpChallengeResponse> StartEmailLoginAsync(StartEmailOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyEmailLoginAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    Task<OtpChallengeResponse> StartEmailRegistrationAsync(StartEmailOtpRequest request, CancellationToken cancellationToken);

    Task<VerifyRegistrationOtpResponse> VerifyEmailRegistrationAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> CompleteRegistrationAsync(CompleteOtpRegistrationRequest request, CancellationToken cancellationToken);
}
