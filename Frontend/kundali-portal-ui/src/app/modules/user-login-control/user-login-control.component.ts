import { CommonModule } from '@angular/common';
import { Component, OnInit, inject,ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
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

  users: UserLoginControlUser[] = [];
  searchTerm = '';
  loading = false;
  errorMessage = '';
  successMessage = '';
  savingUserIds = new Set<string>();
  savingLimitUserIds = new Set<string>();
  constructor(private readonly cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  get filteredUsers(): UserLoginControlUser[] {
    const term = this.searchTerm.trim().toLowerCase();

    if (!term) {
      return this.users;
    }

    return this.users.filter(user =>
      user.fullName.toLowerCase().includes(term)
      || user.email.toLowerCase().includes(term)
      || (user.phoneNumber ?? '').toLowerCase().includes(term)
    );
  }

  loadUsers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.userLoginControlService.getUsers().subscribe({
      next: (users) => {
        this.users = (Array.isArray(users) ? users : []).map(user => ({
          ...user,
          kundaliSaveLimit: Number.isFinite(Number(user.kundaliSaveLimit)) ? Number(user.kundaliSaveLimit) : 5
        }));
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
        this.users = this.users.map(item => item.id === updatedUser.id ? updatedUser : item);
        this.successMessage = `${updatedUser.fullName || updatedUser.email} login ${updatedUser.isActive ? 'enabled' : 'disabled'}.`;
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
        this.users = this.users.map(item => item.id === updatedUser.id ? updatedUser : item);
        this.successMessage = `${updatedUser.fullName || updatedUser.email} can save up to ${updatedUser.kundaliSaveLimit} Kundalis.`;
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

  private getHttpErrorMessage(error: any, fallback: string): string {
    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (typeof error?.error?.message === 'string' && error.error.message.trim()) {
      return error.error.message;
    }

    return fallback;
  }
  isSavingLimit(userId: string): boolean {
    return this.savingLimitUserIds.has(userId);
  }
  isSaving(userId: string): boolean {
    return this.savingUserIds.has(userId);
  }

  trackByUserId(_: number, user: UserLoginControlUser): string {
    return user.id;
  }
}

