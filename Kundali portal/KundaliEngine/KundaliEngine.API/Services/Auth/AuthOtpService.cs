using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Email;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.API.Services.Sms;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;

namespace KundaliEngine.API.Services.Auth;

public sealed class AuthOtpService : IAuthOtpService
{
    private const string EmailChannel = "Email";
    private const string PhoneChannel = "Phone";
    private const string LoginPurpose = "Login";
    private const string RegistrationPurpose = "Registration";
    private const int OtpDigits = 6;
    private const int OtpExpiryMinutes = 10;
    private const int RegistrationTokenExpiryMinutes = 15;
    private const int ResendCooldownSeconds = 10;
    private const int MaxAttempts = 5;
    private const int MaxRequestsPerWindow = 4;

    private readonly KundaliDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailSender _emailSender;
    private readonly ISmsOtpSender _smsOtpSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEntitlementService _entitlementService;

    public AuthOtpService(
        KundaliDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailSender emailSender,
        ISmsOtpSender smsOtpSender,
        IHttpContextAccessor httpContextAccessor,
        IEntitlementService entitlementService)
    {
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailSender = emailSender;
        _smsOtpSender = smsOtpSender;
        _httpContextAccessor = httpContextAccessor;
        _entitlementService = entitlementService;
    }

    public async Task<OtpChallengeResponse> StartOtpLoginAsync(StartEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var target = ResolveOtpTarget(request);
        var user = await FindUserByTargetAsync(target, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException(target.Channel == PhoneChannel
                ? "Mobile number is not registered. Please create an account."
                : "Email is not registered. Please create an account.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Your account is inactive. Please contact support.");
        }

        var resendCount = await EnsureCanSendAsync(target.Channel, target.Destination, LoginPurpose, cancellationToken);
        var code = GenerateOtpCode();
        var now = DateTime.UtcNow;
        await InvalidateOpenChallengesAsync(target.Channel, target.Destination, LoginPurpose, now, cancellationToken);
        var challenge = BuildChallenge(LoginPurpose, target.Channel, target.Destination, code, now, user.Id, resendCount);

        _dbContext.AuthOtpChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendOtpAsync(target, code, "login", cancellationToken);

        return BuildChallengeResponse(challenge, target, "Verification code sent.");
    }
    public async Task<AuthResponse> VerifyOtpLoginAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var target = ResolveOtpTarget(request);
        var challenge = await GetChallengeAsync(request.ChallengeId, LoginPurpose, target.Channel, target.Destination, cancellationToken);
        EnsureChallengeCanBeVerified(challenge);

        if (!VerifyCode(request.Code, challenge))
        {
            await RegisterFailedAttemptAsync(challenge, cancellationToken);
            throw new UnauthorizedAccessException("Invalid or expired verification code.");
        }

        var user = challenge.UserId.HasValue
            ? await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == challenge.UserId.Value, cancellationToken)
            : null;

        if (user == null || !user.IsActive)
        {
            await RegisterFailedAttemptAsync(challenge, cancellationToken);
            throw new UnauthorizedAccessException("Invalid or expired verification code.");
        }

        var now = DateTime.UtcNow;
        challenge.VerifiedAt = now;
        challenge.ConsumedAt = now;
        user.LastLoginAt = now;
        if (target.Channel == EmailChannel)
        {
            user.EmailVerifiedAt ??= now;
        }
        else
        {
            user.PhoneVerifiedAt ??= now;
        }
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return BuildAuthResponse(user);
    }

    public async Task<OtpChallengeResponse> StartOtpRegistrationAsync(StartEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var target = ResolveOtpTarget(request);
        var existingUser = await UserExistsForTargetAsync(target, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException(target.Channel == EmailChannel
                ? "Email already registered. Please login."
                : "Mobile number already registered. Please login.");
        }

        var resendCount = await EnsureCanSendAsync(target.Channel, target.Destination, RegistrationPurpose, cancellationToken);
        var code = GenerateOtpCode();
        var now = DateTime.UtcNow;
        await InvalidateOpenChallengesAsync(target.Channel, target.Destination, RegistrationPurpose, now, cancellationToken);
        var challenge = BuildChallenge(RegistrationPurpose, target.Channel, target.Destination, code, now, null, resendCount);

        _dbContext.AuthOtpChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await SendOtpAsync(target, code, "registration", cancellationToken);

        return BuildChallengeResponse(challenge, target, "Verification code sent.");
    }

    public async Task<VerifyRegistrationOtpResponse> VerifyOtpRegistrationAsync(VerifyEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var target = ResolveOtpTarget(request);
        var challenge = await GetChallengeAsync(request.ChallengeId, RegistrationPurpose, target.Channel, target.Destination, cancellationToken);
        EnsureChallengeCanBeVerified(challenge);

        if (!VerifyCode(request.Code, challenge))
        {
            await RegisterFailedAttemptAsync(challenge, cancellationToken);
            throw new UnauthorizedAccessException("Invalid or expired verification code.");
        }

        var existingUser = await UserExistsForTargetAsync(target, cancellationToken);
        if (existingUser)
        {
            throw new InvalidOperationException(target.Channel == EmailChannel
                ? "Email already registered. Please login."
                : "Mobile number already registered. Please login.");
        }

        var now = DateTime.UtcNow;
        var registrationToken = CreateRegistrationToken(challenge.Id);
        challenge.VerifiedAt = now;
        challenge.RegistrationTokenHash = BCrypt.Net.BCrypt.HashPassword(registrationToken);
        challenge.RegistrationTokenExpiresAt = now.AddMinutes(RegistrationTokenExpiryMinutes);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VerifyRegistrationOtpResponse
        {
            RegistrationToken = registrationToken,
            ExpiresAt = challenge.RegistrationTokenExpiresAt.Value,
            Email = target.Channel == EmailChannel ? target.Destination : null,
            PhoneNumber = target.Channel == PhoneChannel ? target.Destination : null
        };
    }

    public async Task<AuthResponse> CompleteRegistrationAsync(CompleteOtpRegistrationRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetChallengeIdFromRegistrationToken(request.RegistrationToken, out var challengeId))
        {
            throw new UnauthorizedAccessException("Registration session has expired. Please verify again.");
        }

        var fullName = (request.FullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        var challenge = await _dbContext.AuthOtpChallenges
            .FirstOrDefaultAsync(x => x.Id == challengeId
                && x.Purpose == RegistrationPurpose, cancellationToken);

        if (challenge == null
            || challenge.VerifiedAt == null
            || challenge.ConsumedAt != null
            || challenge.RegistrationTokenExpiresAt == null
            || challenge.RegistrationTokenExpiresAt <= DateTime.UtcNow
            || string.IsNullOrWhiteSpace(challenge.RegistrationTokenHash)
            || !BCrypt.Net.BCrypt.Verify(request.RegistrationToken, challenge.RegistrationTokenHash))
        {
            throw new UnauthorizedAccessException("Registration session has expired. Please verify again.");
        }

        var now = DateTime.UtcNow;
        var email = challenge.Channel == EmailChannel
            ? challenge.DestinationNormalized
            : NormalizeOptionalEmail(request.Email);
        var phoneNumber = challenge.Channel == PhoneChannel
            ? challenge.DestinationNormalized
            : NormalizeOptionalPhoneNumber(request.PhoneNumber);
        var preferredLanguage = PreferredLanguageHelper.Normalize(request.PreferredLanguage) ?? "en";

        if (!string.IsNullOrWhiteSpace(email) && await UserExistsForTargetAsync(new OtpTarget(EmailChannel, email), cancellationToken))
        {
            throw new InvalidOperationException("Email already registered. Please login.");
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && await UserExistsForTargetAsync(new OtpTarget(PhoneChannel, phoneNumber), cancellationToken))
        {
            throw new InvalidOperationException("Mobile number already registered. Please login.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PhoneNumber = phoneNumber,
            PreferredLanguage = preferredLanguage,
            PasswordHash = null,
            AuthProvider = challenge.Channel == PhoneChannel ? "PhoneOtp" : "EmailOtp",
            Role = "User",
            IsActive = true,
            EmailVerifiedAt = challenge.Channel == EmailChannel ? now : null,
            PhoneVerifiedAt = challenge.Channel == PhoneChannel ? now : null,
            CreatedAt = now,
            LastLoginAt = now
        };

        challenge.ConsumedAt = now;
        challenge.UserId = user.Id;
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user);
    }

    private AuthOtpChallenge BuildChallenge(string purpose, string channel, string destination, string code, DateTime now, Guid? userId, int resendCount)
    {
        var context = _httpContextAccessor.HttpContext;
        return new AuthOtpChallenge
        {
            Id = Guid.NewGuid(),
            Purpose = purpose,
            Channel = channel,
            DestinationNormalized = destination,
            UserId = userId,
            CodeHash = BCrypt.Net.BCrypt.HashPassword(code),
            ExpiresAt = now.AddMinutes(OtpExpiryMinutes),
            ResendCount = resendCount,
            LastSentAt = now,
            CreatedAt = now,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = context?.Request.Headers.UserAgent.ToString()
        };
    }

    private async Task<int> EnsureCanSendAsync(string channel, string destination, string purpose, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var recentRows = await _dbContext.AuthOtpChallenges
            .Where(x => x.Channel == channel && x.Purpose == purpose)
            .OrderByDescending(x => x.LastSentAt)
            .Take(40)
            .ToListAsync(cancellationToken);
        var recentChallenge = recentRows.FirstOrDefault(item => DestinationsMatch(item.Channel, item.DestinationNormalized, destination));

        if (recentChallenge != null)
        {
            var lastSentAt = NormalizeOtpTimestamp(recentChallenge.LastSentAt, now);
            var nextAllowedAt = lastSentAt.AddSeconds(ResendCooldownSeconds);
            if (nextAllowedAt > now)
            {
                var remainingSeconds = Math.Max(1, (int)Math.Ceiling((nextAllowedAt - now).TotalSeconds));
                throw new InvalidOperationException($"Please wait {remainingSeconds} seconds before requesting another verification code.");
            }
        }

        var windowStart = now.AddMinutes(-OtpExpiryMinutes);
        var recentCount = recentRows.Count(item =>
            DestinationsMatch(item.Channel, item.DestinationNormalized, destination)
            && NormalizeOtpTimestamp(item.CreatedAt, now) >= windowStart
            && NormalizeOtpTimestamp(item.CreatedAt, now) <= now.AddSeconds(30));

        if (recentCount >= MaxRequestsPerWindow)
        {
            throw new InvalidOperationException("Too many verification code requests. Please try again later.");
        }

        return recentCount;
    }

    private static DateTime NormalizeOtpTimestamp(DateTime timestamp, DateTime nowUtc)
    {
        if (timestamp > nowUtc.AddMinutes(1))
        {
            var indiaOffsetTimestamp = timestamp.AddHours(-5.5);
            if (indiaOffsetTimestamp <= nowUtc.AddMinutes(1))
            {
                return indiaOffsetTimestamp;
            }

            return nowUtc.AddSeconds(-ResendCooldownSeconds);
        }

        return timestamp;
    }

    private async Task InvalidateOpenChallengesAsync(
        string channel,
        string destination,
        string purpose,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var openChallenges = await _dbContext.AuthOtpChallenges
            .Where(x => x.Channel == channel
                && x.Purpose == purpose
                && x.ConsumedAt == null
                && x.VerifiedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var open in openChallenges.Where(item => DestinationsMatch(item.Channel, item.DestinationNormalized, destination)))
        {
            open.ConsumedAt = now;
        }
    }

    private async Task<AuthOtpChallenge> GetChallengeAsync(
        Guid challengeId,
        string purpose,
        string channel,
        string destination,
        CancellationToken cancellationToken)
    {
        AuthOtpChallenge? challenge = null;
        if (challengeId != Guid.Empty)
        {
            challenge = await _dbContext.AuthOtpChallenges
                .FirstOrDefaultAsync(x => x.Id == challengeId && x.Purpose == purpose, cancellationToken);
        }

        if (challenge == null
            || !string.Equals(challenge.Channel, channel, StringComparison.OrdinalIgnoreCase)
            || !DestinationsMatch(challenge.Channel, challenge.DestinationNormalized, destination))
        {
            var candidates = await _dbContext.AuthOtpChallenges
                .Where(x => x.Channel == channel
                    && x.Purpose == purpose
                    && x.ConsumedAt == null
                    && x.VerifiedAt == null)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync(cancellationToken);
            challenge = candidates.FirstOrDefault(item => DestinationsMatch(item.Channel, item.DestinationNormalized, destination));
        }

        if (challenge == null)
        {
            throw new UnauthorizedAccessException("Invalid or expired verification code.");
        }

        return challenge;
    }

    private static void EnsureChallengeCanBeVerified(AuthOtpChallenge challenge)
    {
        var now = DateTime.UtcNow;
        var expiresAt = NormalizeOtpTimestamp(challenge.ExpiresAt, now);
        if (challenge.ConsumedAt != null || expiresAt <= now || challenge.AttemptCount >= MaxAttempts)
        {
            throw new UnauthorizedAccessException("Invalid or expired verification code.");
        }
    }

    private async Task<User?> FindUserByTargetAsync(OtpTarget target, CancellationToken cancellationToken)
    {
        if (target.Channel == EmailChannel)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == target.Destination, cancellationToken);
        }

        var phoneUsers = await _dbContext.Users
            .Where(x => x.PhoneNumber != null)
            .ToListAsync(cancellationToken);

        return phoneUsers.FirstOrDefault(x => DestinationsMatch(PhoneChannel, x.PhoneNumber ?? string.Empty, target.Destination));
    }

    private async Task<bool> UserExistsForTargetAsync(OtpTarget target, CancellationToken cancellationToken)
    {
        if (target.Channel == EmailChannel)
        {
            return await _dbContext.Users.AnyAsync(x => x.Email != null && x.Email.ToLower() == target.Destination, cancellationToken);
        }

        var phoneUsers = await _dbContext.Users
            .Where(x => x.PhoneNumber != null)
            .Select(x => x.PhoneNumber)
            .ToListAsync(cancellationToken);

        return phoneUsers.Any(phoneNumber => DestinationsMatch(PhoneChannel, phoneNumber ?? string.Empty, target.Destination));
    }

    private async Task RegisterFailedAttemptAsync(AuthOtpChallenge challenge, CancellationToken cancellationToken)
    {
        challenge.AttemptCount += 1;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool VerifyCode(string code, AuthOtpChallenge challenge)
    {
        var normalizedCode = new string((code ?? string.Empty).Where(char.IsDigit).ToArray());
        return normalizedCode.Length == OtpDigits && BCrypt.Net.BCrypt.Verify(normalizedCode, challenge.CodeHash);
    }

    private async Task SendOtpAsync(OtpTarget target, string code, string purpose, CancellationToken cancellationToken)
    {
        if (target.Channel == PhoneChannel)
        {
            await _smsOtpSender.SendOtpAsync(FormatSmsPhoneNumber(target.Destination), code, cancellationToken);
            return;
        }

        await SendOtpEmailAsync(target.Destination, code, purpose, cancellationToken);
    }

    private async Task SendOtpEmailAsync(string email, string code, string purpose, CancellationToken cancellationToken)
    {
        await _emailSender.SendAsync(new EmailMessage
        {
            ToEmail = email,
            Subject = "Your HoraLife verification code",
            TextBody = $"Your HoraLife {purpose} verification code is {code}. It is valid for {OtpExpiryMinutes} minutes. If you did not request this code, you can ignore this email.",
            HtmlBody = BuildOtpEmailHtml(purpose, code, OtpExpiryMinutes)
        }, cancellationToken);
    }

    private static OtpChallengeResponse BuildChallengeResponse(AuthOtpChallenge challenge, OtpTarget target, string message)
    {
        return new OtpChallengeResponse
        {
            ChallengeId = challenge.Id,
            ExpiresAt = challenge.ExpiresAt,
            ResendAfterSeconds = ResendCooldownSeconds,
            MaskedDestination = target.Channel == PhoneChannel ? MaskPhoneNumber(target.Destination) : MaskEmail(target.Destination),
            Message = message
        };
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        return AuthResponseFactory.Create(user, _jwtTokenGenerator.GenerateToken(user), _entitlementService);
    }

    private static OtpTarget ResolveOtpTarget(StartEmailOtpRequest request)
    {
        var channel = NormalizeChannel(request.Channel, request.Email, request.PhoneNumber);
        return channel == PhoneChannel
            ? new OtpTarget(PhoneChannel, NormalizePhoneNumber(request.PhoneNumber))
            : new OtpTarget(EmailChannel, NormalizeEmail(request.Email));
    }

    private static OtpTarget ResolveOtpTarget(VerifyEmailOtpRequest request)
    {
        var channel = NormalizeChannel(request.Channel, request.Email, request.PhoneNumber);
        return channel == PhoneChannel
            ? new OtpTarget(PhoneChannel, NormalizePhoneNumber(request.PhoneNumber))
            : new OtpTarget(EmailChannel, NormalizeEmail(request.Email));
    }

    private static string NormalizeChannel(string? channel, string? email, string? phoneNumber)
    {
        if (string.Equals(channel, PhoneChannel, StringComparison.OrdinalIgnoreCase)
            || string.Equals(channel, "Mobile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(channel, "Sms", StringComparison.OrdinalIgnoreCase))
        {
            return PhoneChannel;
        }

        if (string.Equals(channel, EmailChannel, StringComparison.OrdinalIgnoreCase))
        {
            return EmailChannel;
        }

        return !string.IsNullOrWhiteSpace(phoneNumber) && string.IsNullOrWhiteSpace(email)
            ? PhoneChannel
            : EmailChannel;
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !normalized.Contains('@'))
        {
            throw new InvalidOperationException("A valid email address is required.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return NormalizeEmail(email);
    }

    private static string NormalizePhoneNumber(string? phoneNumber)
    {
        var normalized = TryNormalizePhoneNumber(phoneNumber);
        if (normalized == null)
        {
            throw new InvalidOperationException("A valid mobile number is required.");
        }

        return normalized;
    }

    private static string? TryNormalizePhoneNumber(string? phoneNumber)
    {
        var digits = new string((phoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits.Length is >= 10 and <= 15 ? digits : null;
    }

    private static bool DestinationsMatch(string channel, string stored, string requested)
    {
        if (string.Equals(channel, EmailChannel, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(stored, requested, StringComparison.OrdinalIgnoreCase);
        }

        var left = TryNormalizePhoneNumber(stored);
        var right = TryNormalizePhoneNumber(requested);
        return left != null && right != null && left == right;
    }

    private static string FormatSmsPhoneNumber(string normalizedPhone)
    {
        return normalizedPhone.Length == 10 ? $"91{normalizedPhone}" : normalizedPhone;
    }

    private static string? NormalizeOptionalPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        return NormalizePhoneNumber(phoneNumber);
    }

    private static string GenerateOtpCode()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    private static string CreateRegistrationToken(Guid challengeId)
    {
        return $"{challengeId:N}.{Base64UrlEncode(RandomNumberGenerator.GetBytes(32))}";
    }

    private static bool TryGetChallengeIdFromRegistrationToken(string token, out Guid challengeId)
    {
        challengeId = Guid.Empty;
        var parts = (token ?? string.Empty).Split('.', 2);
        return parts.Length == 2 && Guid.TryParseExact(parts[0], "N", out challengeId);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1)
        {
            return $"***{email[atIndex..]}";
        }

        return $"{email[0]}***{email[(atIndex - 1)..]}";
    }

    private static string MaskPhoneNumber(string phoneNumber)
    {
        return phoneNumber.Length <= 4
            ? "****"
            : $"******{phoneNumber[^4..]}";
    }

    private sealed record OtpTarget(string Channel, string Destination);
    private static string BuildOtpEmailHtml(
    string purpose,
    string code,
    int expiryMinutes)
    {
        var safePurpose = WebUtility.HtmlEncode(purpose);
        var safeCode = WebUtility.HtmlEncode(code);

        return $$"""
    <!DOCTYPE html>
    <html lang="en">
    <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <meta name="color-scheme" content="light">
        <meta name="supported-color-schemes" content="light">

        <title>HoraLife Verification Code</title>
    </head>

    <body style="
        margin: 0;
        padding: 0;
        background-color: #f5f2eb;
        font-family: Arial, Helvetica, sans-serif;
        color: #262033;
    ">

        <!-- Hidden email preview text -->
        <div style="
            display: none;
            max-height: 0;
            overflow: hidden;
            opacity: 0;
            color: transparent;
        ">
            Your HoraLife verification code is {{safeCode}}.
            It expires in {{expiryMinutes}} minutes.
        </div>

        <table
            role="presentation"
            width="100%"
            cellspacing="0"
            cellpadding="0"
            border="0"
            style="background-color: #f5f2eb;"
        >
            <tr>
                <td align="center" style="padding: 32px 16px;">

                    <table
                        role="presentation"
                        width="100%"
                        cellspacing="0"
                        cellpadding="0"
                        border="0"
                        style="
                            width: 100%;
                            max-width: 560px;
                            background-color: #ffffff;
                            border: 1px solid #e6dfd1;
                            border-radius: 16px;
                            overflow: hidden;
                            box-shadow: 0 8px 24px rgba(45, 30, 70, 0.08);
                        "
                    >

                        <!-- Brand header -->
                        <tr>
                            <td
                                align="center"
                                style="
                                    padding: 28px 24px 22px;
                                    background-color: #2f174f;
                                    border-bottom: 4px solid #d5a94e;
                                "
                            >
                                <!--
                                Optional hosted logo:

                                <img
                                    src="https://horalife.in/assets/brand/horalife/horalife-icon-color.svg"
                                    width="64"
                                    alt="HoraLife"
                                    style="
                                        display: block;
                                        width: 64px;
                                        max-width: 64px;
                                        margin: 0 auto 12px;
                                        border: 0;
                                    "
                                >
                                -->

                                <div style="
                                    color: #f2cc72;
                                    font-family: Georgia, 'Times New Roman', serif;
                                    font-size: 27px;
                                    font-weight: 700;
                                    letter-spacing: 1.5px;
                                    line-height: 1.2;
                                ">
                                    HoraLife
                                </div>

                                <div style="
                                    margin-top: 6px;
                                    color: #e8dfef;
                                    font-size: 12px;
                                    letter-spacing: 0.6px;
                                    line-height: 1.5;
                                ">
                                    Guardian of Timeless Vedic Wisdom
                                </div>
                            </td>
                        </tr>

                        <!-- Email content -->
                        <tr>
                            <td style="padding: 36px 36px 24px;">

                                <h1 style="
                                    margin: 0 0 14px;
                                    color: #2f174f;
                                    font-size: 23px;
                                    font-weight: 700;
                                    line-height: 1.35;
                                    text-align: center;
                                ">
                                    Verify your email
                                </h1>

                                <p style="
                                    margin: 0 0 24px;
                                    color: #5e5965;
                                    font-size: 15px;
                                    line-height: 1.7;
                                    text-align: center;
                                ">
                                    Use the following code to complete your
                                    <strong style="color: #362d42;">
                                        {{safePurpose}}
                                    </strong>
                                    verification.
                                </p>

                                <!-- OTP -->
                                <table
                                    role="presentation"
                                    width="100%"
                                    cellspacing="0"
                                    cellpadding="0"
                                    border="0"
                                >
                                    <tr>
                                        <td align="center">
                                            <div style="
                                                display: inline-block;
                                                min-width: 220px;
                                                padding: 18px 24px;
                                                background-color: #f7f2fc;
                                                border: 1px solid #ddcce9;
                                                border-radius: 12px;
                                                color: #2f174f;
                                                font-family: 'Courier New', Courier, monospace;
                                                font-size: 34px;
                                                font-weight: 700;
                                                letter-spacing: 8px;
                                                line-height: 1.2;
                                                text-align: center;
                                            ">
                                                {{safeCode}}
                                            </div>
                                        </td>
                                    </tr>
                                </table>

                                <p style="
                                    margin: 22px 0 0;
                                    color: #6a6470;
                                    font-size: 14px;
                                    line-height: 1.6;
                                    text-align: center;
                                ">
                                    This code is valid for
                                    <strong style="color: #2f174f;">
                                        {{expiryMinutes}} minutes
                                    </strong>.
                                </p>

                                <!-- Security notice -->
                                <table
                                    role="presentation"
                                    width="100%"
                                    cellspacing="0"
                                    cellpadding="0"
                                    border="0"
                                    style="
                                        margin-top: 28px;
                                        background-color: #fff9ea;
                                        border: 1px solid #f0ddb0;
                                        border-radius: 10px;
                                    "
                                >
                                    <tr>
                                        <td style="
                                            padding: 14px 16px;
                                            color: #6c5729;
                                            font-size: 13px;
                                            line-height: 1.6;
                                        ">
                                            <strong>Security reminder:</strong>
                                            HoraLife will never ask you to share
                                            this verification code with anyone.
                                        </td>
                                    </tr>
                                </table>

                                <p style="
                                    margin: 24px 0 0;
                                    color: #817b85;
                                    font-size: 13px;
                                    line-height: 1.6;
                                    text-align: center;
                                ">
                                    If you did not request this code, you can
                                    safely ignore this email.
                                </p>
                            </td>
                        </tr>

                        <!-- Footer -->
                        <tr>
                            <td style="
                                padding: 20px 24px;
                                background-color: #faf8f4;
                                border-top: 1px solid #ece6dc;
                                text-align: center;
                            ">
                                <p style="
                                    margin: 0;
                                    color: #817b85;
                                    font-size: 12px;
                                    line-height: 1.6;
                                ">
                                    &copy; {{DateTime.UtcNow.Year}} HoraLife
                                    <br>
                                    Precision Vedic Astrology
                                </p>

                                <p style="
                                    margin: 8px 0 0;
                                    color: #9a949d;
                                    font-size: 11px;
                                    line-height: 1.5;
                                ">
                                    This is an automated security email.
                                    Please do not reply.
                                </p>
                            </td>
                        </tr>

                    </table>
                </td>
            </tr>
        </table>
    </body>
    </html>
    """;
    }
}






