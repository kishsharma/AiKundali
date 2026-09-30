import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NotificationService } from '../../shared/services/notification.service';
import { AuthService } from '../../core/auth/auth.service';
import { isPlusUser } from '../../core/auth/plan.util';
import { SubscriptionModalService } from '../../core/services/subscription-modal.service';
import {
  AstrologerBrandingSetting,
  AstrologerBrandingSettingsService
} from '../../core/services/astrologer-branding-settings.service';

@Component({
  selector: 'app-white-label-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent],
  templateUrl: './white-label-settings.component.html',
  styleUrl: './white-label-settings.component.scss'
})
export class WhiteLabelSettingsComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly brandingSettingsService = inject(AstrologerBrandingSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly authService = inject(AuthService);
  private readonly subscriptionModal = inject(SubscriptionModalService);

  loading = false;
  saving = false;
  saveMessage = '';
  errorMessage = '';

  get canEditBranding(): boolean {
    return isPlusUser(this.authService.getCurrentUser());
  }

  openPlans(): void {
    this.subscriptionModal.open();
  }

  readonly settingsForm = this.fb.group({
    astrologerName: ['', [Validators.required, Validators.maxLength(150)]],
    designation: ['', Validators.maxLength(120)],
    address: ['', Validators.maxLength(500)],
    contactNumber: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(150)]],
    logoUrl: ['', Validators.maxLength(500)],
    showOnChartPage: [true],
    showOnCompatibilityPage: [true],
    showOnPdfReport: [true],
    isActive: [true]
  });

  ngOnInit(): void {
    this.loadSettings();
  }

  get settings() {
    return this.settingsForm.getRawValue();
  }

  loadSettings(): void {
    this.loading = true;
    this.errorMessage = '';

    this.brandingSettingsService.getMine().subscribe({
      next: (setting) => {
        if (setting) {
          this.settingsForm.patchValue({
            astrologerName: setting.astrologerName,
            designation: setting.designation ?? '',
            address: setting.address ?? '',
            contactNumber: setting.contactNumber ?? '',
            email: setting.email ?? '',
            logoUrl: setting.logoUrl ?? '',
            showOnChartPage: setting.showOnChartPage,
            showOnCompatibilityPage: setting.showOnCompatibilityPage,
            showOnPdfReport: setting.showOnPdfReport,
            isActive: setting.isActive
          });
        }

        this.loading = false;
      },
      error: (error) => {
        this.errorMessage = error.message || 'Unable to load branding settings.';
        this.notifications.error(this.errorMessage);
        this.loading = false;
      }
    });
  }

  resetForm(): void {
    this.settingsForm.reset({
      astrologerName: '',
      designation: '',
      address: '',
      contactNumber: '',
      email: '',
      logoUrl: '',
      showOnChartPage: true,
      showOnCompatibilityPage: true,
      showOnPdfReport: true,
      isActive: true
    });
  }

  saveSettings(): void {
    this.saveMessage = '';
    this.errorMessage = '';

    if (!this.canEditBranding) {
      this.errorMessage = 'White-label branding is available on Premium Plus.';
      this.notifications.error(this.errorMessage);
      return;
    }

    if (this.settingsForm.invalid) {
      this.settingsForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.brandingSettingsService.saveMine(this.settings as AstrologerBrandingSetting).subscribe({
      next: () => {
        this.saveMessage = 'Branding settings saved.';
        this.notifications.success(this.saveMessage);
        this.saving = false;
      },
      error: (error) => {
        this.errorMessage = error.message || 'Unable to save branding settings.';
        this.notifications.error(this.errorMessage);
        this.saving = false;
      }
    });
  }
}
