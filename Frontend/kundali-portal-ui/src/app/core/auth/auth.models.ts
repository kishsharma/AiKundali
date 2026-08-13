export type OtpChannel = 'Phone' | 'Email';
export type PreferredLanguage = 'en' | 'hi' | 'kn';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber?: string;
  preferredLanguage?: PreferredLanguage;
  password: string;
}

export interface StartEmailOtpRequest {
  channel?: OtpChannel;
  email?: string;
  phoneNumber?: string;
}

export interface VerifyEmailOtpRequest {
  challengeId: string;
  channel?: OtpChannel;
  email?: string;
  phoneNumber?: string;
  code: string;
}

export interface CompleteOtpRegistrationRequest {
  registrationToken: string;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  preferredLanguage?: PreferredLanguage;
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
  phoneNumber?: string | null;
}

export interface AuthResponse {
  userId: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  preferredLanguage?: PreferredLanguage | null;
  role: string;
  planCode?: string | null;
  planExpiresAt?: string | null;
  isPremium?: boolean | null;
  token: string;
}

export interface UserProfileResponse {
  userId: string;
  fullName: string;
  email?: string | null;
  phoneNumber?: string | null;
  preferredLanguage?: PreferredLanguage | null;
  role: string;
  planCode?: string | null;
  planExpiresAt?: string | null;
  isPremium?: boolean | null;
}

export interface UpdatePreferredLanguageRequest {
  preferredLanguage: PreferredLanguage;
}

export interface UpdatePreferredLanguageResponse {
  preferredLanguage: PreferredLanguage;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
