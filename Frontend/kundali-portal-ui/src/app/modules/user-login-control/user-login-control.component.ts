import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { AuthService } from '../../core/auth/auth.service';
import { NotificationService } from '../../shared/services/notification.service';
import { UserLoginControlService, UserLoginControlUser } from '../../core/services/user-login-control.service';

@Component({
  selector: 'app-user-login-control',
  standalone: true,
  imports: [CommonModule, FormsModule, PageHeaderComponent],
  templateUrl: './user-login-control.component.html',
  styleUrl: './user-login-control.component.scss'
})
export class UserLoginControlComponent implements OnInit {
  private readonly userLoginControlService = inject(UserLoginControlService);
  private readonly notifications = inject(NotificationService);
  private readonly authService = inject(AuthService);

  users: UserLoginControlUser[] = [];
  searchTerm = '';
  onlineOnly = false;
  loading = false;
  errorMessage = '';
  successMessage = '';
  savingUserIds = new Set<string>();
  savingLimitUserIds = new Set<string>();
  savingPlanUserIds = new Set<string>();
  planExpiryInputs: Record<string, string> = {};

  constructor(private readonly cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  get onlineCount(): number {
    return this.users.filter(user => user.isOnline).length;
  }

  get filteredUsers(): UserLoginControlUser[] {
    const term = this.searchTerm.trim().toLowerCase();

    return this.users.filter(user => {
      if (this.onlineOnly && !user.isOnline) {
        return false;
      }

      if (!term) {
        return true;
      }

      return (user.fullName ?? '').toLowerCase().includes(term)
        || (user.email ?? '').toLowerCase().includes(term)
        || (user.phoneNumber ?? '').toLowerCase().includes(term);
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userLoginControlService.getUsers().subscribe({
      next: (users) => {
        this.users = (Array.isArray(users) ? users : []).map(user => this.normalizeUser(user));
        this.loading = false;
        this.cdr.markForCheck();
      },
      error: (error) => {
        this.errorMessage = this.getHttpErrorMessage(error, 'Unable to load users.');
        this.notifications.error(this.errorMessage);
        this.loading = false;
      }
    });
  }

  toggleLoginAccess(user: UserLoginControlUser): void {
    if (this.savingUserIds.has(user.id)) {
      return;
    }

    const previousValue = user.isActive;
    const nextValue = !previousValue;

    user.isActive = nextValue;
    this.errorMessage = '';
    this.successMessage = '';
    this.savingUserIds.add(user.id);

    this.userLoginControlService.updateActiveStatus(user.id, nextValue).subscribe({
      next: (updatedUser) => {
        const normalizedUser = this.normalizeUser(updatedUser);
        this.users = this.users.map(item => item.id === normalizedUser.id ? normalizedUser : item);
        this.successMessage = `${normalizedUser.fullName || normalizedUser.email} login ${normalizedUser.isActive ? 'enabled' : 'disabled'}.`;
        this.notifications.success(this.successMessage);
        this.savingUserIds.delete(user.id);
      },
      error: (error) => {
        user.isActive = previousValue;
        this.errorMessage = this.getHttpErrorMessage(error, 'Unable to update login access.');
        this.notifications.error(this.errorMessage);
        this.savingUserIds.delete(user.id);
      }
    });
  }

  updateKundaliSaveLimit(user: UserLoginControlUser): void {
    if (this.savingLimitUserIds.has(user.id)) {
      return;
    }

    const nextLimit = Math.trunc(Number(user.kundaliSaveLimit));
    if (!Number.isFinite(nextLimit) || nextLimit < 0 || nextLimit > 1000) {
      this.errorMessage = 'Kundali save limit must be between 0 and 1000.';
      this.notifications.error(this.errorMessage);
      return;
    }

    user.kundaliSaveLimit = nextLimit;
    this.errorMessage = '';
    this.successMessage = '';
    this.savingLimitUserIds.add(user.id);

    this.userLoginControlService.updateKundaliSaveLimit(user.id, nextLimit).subscribe({
      next: (updatedUser) => {
        const normalizedUser = this.normalizeUser(updatedUser);
        this.users = this.users.map(item => item.id === normalizedUser.id ? normalizedUser : item);
        this.successMessage = `${normalizedUser.fullName || normalizedUser.email} can save up to ${normalizedUser.kundaliSaveLimit} Kundalis.`;
        this.notifications.success(this.successMessage);
        this.savingLimitUserIds.delete(user.id);
      },
      error: (error) => {
        this.errorMessage = this.getHttpErrorMessage(error, 'Unable to update Kundali save limit.');
        this.notifications.error(this.errorMessage);
        this.savingLimitUserIds.delete(user.id);
        this.loadUsers();
      }
    });
  }

  onPlanChange(user: UserLoginControlUser, planCode: string): void {
    user.planCode = planCode;
    user.isPremium = planCode !== 'Free';
    user.isPaid = planCode !== 'Free';
    user.isPlus = planCode === 'PremiumPlus';

    if (planCode !== 'Free' && !this.planExpiryInputs[user.id]) {
      this.planExpiryInputs[user.id] = this.getDefaultPremiumExpiryDate();
    }

    this.updatePremiumPlan(user);
  }

  updatePremiumPlan(user: UserLoginControlUser): void {
    if (this.savingPlanUserIds.has(user.id)) {
      return;
    }

    const planExpiresAt = user.planCode !== 'Free'
      ? this.toPlanExpiryIso(this.planExpiryInputs[user.id] || this.getDefaultPremiumExpiryDate())
      : null;

    this.errorMessage = '';
    this.successMessage = '';
    this.savingPlanUserIds.add(user.id);

    this.userLoginControlService.updatePremiumPlan(user.id, user.planCode, planExpiresAt).subscribe({
      next: (updatedUser) => {
        const normalizedUser = this.normalizeUser(updatedUser);
        this.users = this.users.map(item => item.id === normalizedUser.id ? normalizedUser : item);
        this.successMessage = `${normalizedUser.fullName || normalizedUser.email} is now ${this.getPlanLabel(normalizedUser)}.`;
        this.notifications.success(this.successMessage);
        this.savingPlanUserIds.delete(user.id);
        this.refreshSessionIfCurrentUser(normalizedUser);
      },
      error: (error) => {
        this.errorMessage = this.getHttpErrorMessage(error, 'Unable to update plan.');
        this.notifications.error(this.errorMessage);
        this.savingPlanUserIds.delete(user.id);
        this.loadUsers();
      }
    });
  }

  lastSeenLabel(user: UserLoginControlUser): string {
    if (!user.lastSeenAt) {
      return 'Not seen yet';
    }

    const seenAt = new Date(user.lastSeenAt);
    if (Number.isNaN(seenAt.getTime())) {
      return 'Not seen yet';
    }

    return seenAt.toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    });
  }

  getPlanLabel(user: UserLoginControlUser): string {
    if (user.planCode === 'PremiumPlus') {
      return 'Premium Plus';
    }

    if (user.planCode === 'Premium') {
      return 'Premium';
    }

    return 'Free';
  }

  private normalizeUser(user: UserLoginControlUser): UserLoginControlUser {
    const normalizedUser: UserLoginControlUser = {
      ...user,
      email: user.email ?? '',
      planCode: user.planCode || (user.isPremium ? 'Premium' : 'Free'),
      isPremium: Boolean(user.isPaid ?? user.isPremium),
      isPaid: Boolean(user.isPaid ?? user.isPremium),
      isPlus: Boolean(user.isPlus || user.planCode === 'PremiumPlus'),
      kundaliSaveLimit: Number.isFinite(Number(user.kundaliSaveLimit)) ? Number(user.kundaliSaveLimit) : 5
    };

    this.planExpiryInputs[normalizedUser.id] = this.toDateInputValue(normalizedUser.planExpiresAt);
    return normalizedUser;
  }

  private refreshSessionIfCurrentUser(user: UserLoginControlUser): void {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser?.userId || currentUser.userId !== user.id) {
      return;
    }

    this.authService.getMe().subscribe();
  }

  private toDateInputValue(value?: string | null): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toISOString().slice(0, 10);
  }

  private toPlanExpiryIso(value: string | undefined): string | null {
    if (!value) {
      return null;
    }

    const expiryDate = new Date(`${value}T23:59:59.999Z`);
    return Number.isNaN(expiryDate.getTime()) ? null : expiryDate.toISOString();
  }

  private getDefaultPremiumExpiryDate(): string {
    const date = new Date();
    date.setFullYear(date.getFullYear() + 1);
    return date.toISOString().slice(0, 10);
  }

  private getHttpErrorMessage(error: any, fallback: string): string {
    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (typeof error?.error?.message === 'string' && error.error.message.trim()) {
      return error.error.message;
    }

    if (error?.status === 403) {
      return 'Only Admin users can update premium plans. Please sign in with an Admin account.';
    }

    if (error?.status === 404) {
      return 'Premium plan API was not found. Restart the backend after deploying the latest code.';
    }

    if (error?.status === 500) {
      return 'Premium plan update failed on the server. Check that the latest database migrations are applied.';
    }

    return fallback;
  }

  isSavingLimit(userId: string): boolean {
    return this.savingLimitUserIds.has(userId);
  }

  isSavingPlan(userId: string): boolean {
    return this.savingPlanUserIds.has(userId);
  }

  isSaving(userId: string): boolean {
    return this.savingUserIds.has(userId);
  }

  trackByUserId(_: number, user: UserLoginControlUser): string {
    return user.id;
  }
}