import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/auth/auth.service';
import { PracticeGroup, PreferredLanguage, UserProfileResponse } from '../../../core/auth/auth.models';
import { ThemeMode, ThemeService } from '../../../core/services/theme.service';
import { NotificationService } from '../../../shared/services/notification.service';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoPipe],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss',
})
export class ProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly notifications = inject(NotificationService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly translocoService = inject(TranslocoService);
  private readonly themeService = inject(ThemeService);

  readonly languages: ReadonlyArray<{ code: PreferredLanguage; label: string }> = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'हिन्दी' },
    { code: 'kn', label: 'ಕನ್ನಡ' },
  ];
  readonly themes: ReadonlyArray<{ mode: ThemeMode; label: string; icon: string }> = [
    { mode: 'dark', label: 'profile.themeDark', icon: '\u263E' },
    { mode: 'light', label: 'profile.themeLight', icon: '\u2600' },
  ];

  account: UserProfileResponse | null = null;
  groups: PracticeGroup[] = [];
  selected = new Set<string>();
  note = '';
  loading = true;
  saving = false;
  editingPhone = false;
  phoneDraft = '';
  phoneSaving = false;
  phoneError = '';
  errorMessage = '';
  practiceError = '';

  ngOnInit(): void {
    forkJoin({
      account: this.authService.getMe(),
      practice: this.authService.getPractice(),
    }).subscribe({
      next: ({ account, practice }) => {
        this.account = account;
        this.groups = practice.groups ?? [];
        this.selected = new Set(practice.selected ?? []);
        this.note = practice.note ?? '';
        this.phoneDraft = account.phoneNumber ?? '';
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.authService.getMe().subscribe({
          next: (account) => {
            this.account = account;
            this.phoneDraft = account.phoneNumber ?? '';
            this.loading = false;
            this.practiceError = 'Unable to load your practice details.';
            this.changeDetector.markForCheck();
          },
          error: () => {
            this.loading = false;
            this.errorMessage = 'Unable to load your profile.';
            this.changeDetector.markForCheck();
          },
        });
      },
    });
  }

  planLabel(): string {
    const code = this.account?.planCode;
    if (code === 'PremiumPlus') {
      return 'Plus';
    }
    if (code === 'Premium') {
      return 'Premium';
    }
    return 'Free';
  }

  initials(): string {
    const parts = (this.account?.fullName ?? '')
      .trim()
      .split(/\s+/)
      .filter(Boolean);
    const letters = parts.slice(0, 2).map((part) => part[0]?.toUpperCase() ?? '');
    return letters.join('') || 'HL';
  }

  selectedCount(): number {
    return this.selected.size;
  }

  startPhoneEdit(): void {
    this.phoneDraft = this.account?.phoneNumber ?? '';
    this.phoneError = '';
    this.editingPhone = true;
  }

  cancelPhoneEdit(): void {
    this.phoneDraft = this.account?.phoneNumber ?? '';
    this.phoneError = '';
    this.editingPhone = false;
  }

  savePhone(): void {
    const digits = this.phoneDraft.replace(/\D/g, '');
    if (digits.length < 10 || digits.length > 15) {
      this.phoneError = 'Enter a mobile number with 10 to 15 digits.';
      return;
    }

    this.phoneSaving = true;
    this.phoneError = '';
    this.authService.updatePhone({ phoneNumber: digits }).subscribe({
      next: (response) => {
        if (this.account) {
          this.account = { ...this.account, phoneNumber: response.phoneNumber };
        }
        this.phoneDraft = response.phoneNumber;
        this.editingPhone = false;
        this.phoneSaving = false;
        this.notifications.success('Mobile number updated.');
        this.changeDetector.markForCheck();
      },
      error: (error: { error?: { message?: string }; message?: string }) => {
        this.phoneSaving = false;
        this.phoneError = error?.error?.message || error?.message || 'Unable to update your mobile number.';
        this.changeDetector.markForCheck();
      },
    });
  }

  get activeLanguage(): string {
    return this.translocoService.getActiveLang();
  }

  changeLanguage(lang: PreferredLanguage): void {
    if (this.activeLanguage === lang) {
      return;
    }

    const previousLanguage = this.activeLanguage as PreferredLanguage;
    this.translocoService.setActiveLang(lang);
    if (this.account) {
      this.account = { ...this.account, preferredLanguage: lang };
    }

    this.authService.updatePreferredLanguage({ preferredLanguage: lang }).subscribe({
      error: (error: { error?: { message?: string } }) => {
        this.translocoService.setActiveLang(previousLanguage);
        if (this.account) {
          this.account = { ...this.account, preferredLanguage: previousLanguage };
        }
        this.notifications.error(error?.error?.message || 'Unable to update preferred language.');
        this.changeDetector.markForCheck();
      },
    });
  }

  themeMode(): ThemeMode {
    return this.themeService.mode();
  }

  setTheme(mode: ThemeMode): void {
    this.themeService.setTheme(mode);
  }

  isSelected(key: string): boolean {
    return this.selected.has(key);
  }

  toggle(key: string): void {
    if (this.selected.has(key)) {
      this.selected.delete(key);
      return;
    }

    this.selected.add(key);
  }

  savePractice(): void {
    this.saving = true;
    this.practiceError = '';
    this.authService.savePractice({
      services: [...this.selected],
      note: this.note.trim(),
    }).subscribe({
      next: (practice) => {
        this.selected = new Set(practice.selected ?? []);
        this.note = practice.note ?? '';
        this.saving = false;
        this.notifications.success('Practice details saved.');
        this.changeDetector.markForCheck();
      },
      error: (error: { error?: { message?: string }; message?: string }) => {
        this.saving = false;
        this.practiceError = error?.error?.message || error?.message || 'Unable to save your practice details.';
        this.changeDetector.markForCheck();
      },
    });
  }
}
