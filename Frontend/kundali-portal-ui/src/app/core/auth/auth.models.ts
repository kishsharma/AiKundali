export type OtpChannel = 'Phone' | 'Email';
export type PreferredLanguage = 'en' | 'hi' | 'kn';

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber?: string;
  preferredLanguage?: PreferredLanguage;
  password: string;
  rememberMe: boolean;
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
  rememberMe?: boolean;
}

export interface CompleteOtpRegistrationRequest {
  registrationToken: string;
  fullName: string;
  email?: string;
  phoneNumber?: string;
  preferredLanguage?: PreferredLanguage;
  rememberMe: boolean;
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
  profileImageUrl?: string | null;
  phoneNumber?: string | null;
  preferredLanguage?: PreferredLanguage | null;
  role: string;
  planCode?: string | null;
  planExpiresAt?: string | null;
  isPremium?: boolean | null;
  isPaid?: boolean | null;
  isPlus?: boolean | null;
  kundaliSaveLimit?: number | null;
  matchSaveLimit?: number | null;
  token: string;
}

export interface UserProfileResponse {
  userId: string;
  fullName: string;
  email?: string | null;
  profileImageUrl?: string | null;
  phoneNumber?: string | null;
  preferredLanguage?: PreferredLanguage | null;
  role: string;
  planCode?: string | null;
  planExpiresAt?: string | null;
  isPremium?: boolean | null;
  isPaid?: boolean | null;
  isPlus?: boolean | null;
  kundaliSaveLimit?: number | null;
  matchSaveLimit?: number | null;
}

export interface UpdatePreferredLanguageRequest {
  preferredLanguage: PreferredLanguage;
}

export interface UpdatePreferredLanguageResponse {
  preferredLanguage: PreferredLanguage;
}

export interface PracticeItem {
  key: string;
  label: string;
}

export interface PracticeGroup {
  key: string;
  title: string;
  items: PracticeItem[];
}

export interface PracticeProfile {
  groups: PracticeGroup[];
  selected: string[];
  note?: string | null;
}

export interface UpdatePracticeProfileRequest {
  services: string[];
  note?: string | null;
}

export interface UpdatePhoneNumberRequest {
  phoneNumber: string;
}

export interface UpdatePhoneNumberResponse {
  phoneNumber: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
