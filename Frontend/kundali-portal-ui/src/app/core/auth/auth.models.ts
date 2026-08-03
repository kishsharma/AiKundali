export type OtpChannel = 'Phone' | 'Email';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber?: string;
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
  role: string;
  token: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}