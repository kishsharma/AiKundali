export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber?: string;
  preferredLanguage?: string;
  password: string;
}

export interface StartEmailOtpRequest {
  channel: 'Email';
  email: string;
}

export interface VerifyEmailOtpRequest {
  challengeId: string;
  channel: 'Email';
  email: string;
  code: string;
}

export interface CompleteOtpRegistrationRequest {
  registrationToken: string;
  fullName: string;
  email: string;
  preferredLanguage: string;
}

export interface OtpChallengeResponse {
  challengeId: string;
  expiresAt: string;
  resendAfterSeconds: number;
  maskedDestination: string;
  message: string;
}

export interface VerifyRegistrationOtpResponse {
  registrationToken: string;
  expiresAt: string;
  email?: string | null;
}

export interface AuthResponse {
  userId: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  preferredLanguage?: string | null;
  role: string;
  planCode?: string | null;
  isPremium?: boolean | null;
  isPlus?: boolean | null;
  token: string;
}
