import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
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

  users: UserLoginControlUser[] = [];
  searchTerm = '';
  loading = false;
  errorMessage = '';
  successMessage = '';
  savingUserIds = new Set<string>();

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
        this.users = Array.isArray(users) ? users : [];
        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = error?.error?.message || error?.message || 'Unable to load users.';
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
        this.savingUserIds.delete(user.id);
      },
      error: (error) => {
        user.isActive = previousValue;
        this.errorMessage = error?.error?.message || error?.message || 'Unable to update login access.';
        this.savingUserIds.delete(user.id);
      }
    });
  }

  isSaving(userId: string): boolean {
    return this.savingUserIds.has(userId);
  }

  trackByUserId(_: number, user: UserLoginControlUser): string {
    return user.id;
  }
}
