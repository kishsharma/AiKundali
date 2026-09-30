import { AuthResponse, UserProfileResponse } from './auth.models';

export function isPaidUser(user?: AuthResponse | UserProfileResponse | null): boolean {
  if (!user) {
    return false;
  }

  if (user.isPaid === true || user.isPremium === true || user.isPlus === true) {
    return true;
  }

  const planCode = String(user.planCode || '').trim().toLowerCase();
  if (planCode !== 'premium' && planCode !== 'premiumplus' && planCode !== 'premium plus') {
    return false;
  }

  const expiresAt = user.planExpiresAt;
  return !expiresAt || new Date(expiresAt).getTime() > Date.now();
}

export function isPlusUser(user?: AuthResponse | UserProfileResponse | null): boolean {
  if (!user) {
    return false;
  }

  if (user.isPlus === true) {
    return true;
  }

  const planCode = String(user.planCode || '').trim().toLowerCase();
  if (planCode !== 'premiumplus' && planCode !== 'premium plus') {
    return false;
  }

  const expiresAt = user.planExpiresAt;
  return !expiresAt || new Date(expiresAt).getTime() > Date.now();
}

export type DisplayPlanCode = 'Free' | 'Premium' | 'PremiumPlus';

export function resolvePlanCode(user?: AuthResponse | UserProfileResponse | null): DisplayPlanCode {
  if (isPlusUser(user)) {
    return 'PremiumPlus';
  }

  if (isPaidUser(user)) {
    return 'Premium';
  }

  return 'Free';
}

export function planDisplayName(user?: AuthResponse | UserProfileResponse | null): string {
  const planCode = resolvePlanCode(user);
  if (planCode === 'PremiumPlus') {
    return 'Premium Plus';
  }

  return planCode === 'Premium' ? 'Premium' : 'Free';
}

export function planUpgradeLabel(user?: AuthResponse | UserProfileResponse | null): string {
  return resolvePlanCode(user) === 'PremiumPlus' ? 'Manage' : 'Upgrade';
}
