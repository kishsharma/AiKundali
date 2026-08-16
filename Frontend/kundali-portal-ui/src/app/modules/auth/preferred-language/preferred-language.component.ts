import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { AuthService } from '../../../core/auth/auth.service';
import { PreferredLanguage } from '../../../core/auth/auth.models';
import { NotificationService } from '../../../shared/services/notification.service';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';

@Component({
  selector: 'app-preferred-language',
  standalone: true,
  imports: [CommonModule, FormsModule, BrandLogoComponent],
  templateUrl: './preferred-language.component.html',
  styleUrls: ['../login/login.component.scss', './preferred-language.component.scss']
})
export class PreferredLanguageComponent implements OnInit {
  selectedLanguage: PreferredLanguage = 'en';
  isSaving = false;

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router,
    private translocoService: TranslocoService,
    private notifications: NotificationService
  ) {}

  ngOnInit(): void {
    const cachedUser = this.authService.getCurrentUser();
    if (cachedUser?.preferredLanguage) {
      this.selectedLanguage = cachedUser.preferredLanguage;
      this.translocoService.setActiveLang(cachedUser.preferredLanguage);
      this.navigateNext();
      return;
    }

    this.authService.getMe().subscribe({
      next: (user) => {
        if (user.preferredLanguage) {
          this.selectedLanguage = user.preferredLanguage;
          this.translocoService.setActiveLang(user.preferredLanguage);
          this.navigateNext();
        }
      },
      error: () => {
        this.notifications.error('Unable to load profile. Please try again.');
      }
    });
  }

  savePreferredLanguage(): void {
    if (this.isSaving) {
      return;
    }

    this.isSaving = true;
    this.authService.updatePreferredLanguage({ preferredLanguage: this.selectedLanguage }).subscribe({
      next: () => {
        this.isSaving = false;
        this.translocoService.setActiveLang(this.selectedLanguage);
        this.navigateNext();
      },
      error: (error) => {
        this.isSaving = false;
        this.notifications.error(error?.error?.message || 'Unable to save preferred language.');
      }
    });
  }

  private navigateNext(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') || '/app/dashboard';
    this.router.navigateByUrl(returnUrl);
  }
}

