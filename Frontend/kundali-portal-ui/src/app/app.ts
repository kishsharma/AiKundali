import { ChangeDetectorRef, Component, HostListener, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { DisplayPlanCode, isPlusUser as userHasPlusPlan, resolvePlanCode } from './core/auth/plan.util';
import { BillingService } from './core/services/billing.service';
import { PreferredLanguage } from './core/auth/auth.models';
import { ThemeService } from './core/services/theme.service';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';
import { PlanChipComponent } from './shared/components/plan-chip/plan-chip.component';
import { NotificationService } from './shared/services/notification.service';
import { NotificationToastComponent } from './shared/components/notification-toast/notification-toast.component';
import { SubscriptionModalComponent } from './shared/components/subscription-modal/subscription-modal.component';
import { ConfirmDialogComponent } from './shared/components/confirm-dialog/confirm-dialog.component';
import { SubscriptionModalService } from './core/services/subscription-modal.service';
import { GlobalSketchOverlayComponent } from './shared/components/global-sketch-overlay/global-sketch-overlay.component';
import { UserActivityTrackingService } from './core/services/user-activity-tracking.service';
import { NotificationCenterComponent } from './shared/components/notification-center/notification-center.component';

@Component({
  selector: 'app-root',
  imports: [NgFor, NgIf, RouterOutlet,
  RouterLink,
  BrandLogoComponent,
  PlanChipComponent,
  NotificationToastComponent,
  SubscriptionModalComponent,
  ConfirmDialogComponent,
  GlobalSketchOverlayComponent,
  NotificationCenterComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})

export class App {
  private readonly userActivityTracking = inject(UserActivityTrackingService);
  readonly languages: ReadonlyArray<{ code: PreferredLanguage; label: string }> = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'Hindi' },
    { code: 'kn', label: 'Kannada' }
  ];
  activeLanguage: string;
  isPublicLanding = true;
  isAuthPage = false;
 isUserMenuOpen = false;
  isGlobalSketchOpen = false;
 pageTitle = 'Kundali Portal';
  constructor(
    private translocoService: TranslocoService,
    private router: Router,
    public authService: AuthService,
    public themeService: ThemeService,
    private notifications: NotificationService,
    private subscriptionModal: SubscriptionModalService,
    private billingService: BillingService,
    private cdr: ChangeDetectorRef
  ) {
    this.activeLanguage = this.translocoService.getActiveLang();
    this.translocoService.langChanges$.subscribe((lang) => {
      this.activeLanguage = lang;
      document.documentElement.lang = lang;
    });
    document.documentElement.lang = this.translocoService.getActiveLang();
    this.applyCachedPreferredLanguage();
    this.syncAuthenticatedUserLanguage();
    this.loadPlanPricing();
    this.subscriptionModal.isOpen$.subscribe((open) => {
      if (open) {
        this.closeUserMenu();
      }
    });
    this.updateShellMode(this.router.url);
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.updateShellMode(event.urlAfterRedirects));
  }

  changeLanguage(lang: PreferredLanguage) {
    if (lang === this.activeLanguage) {
      return;
    }

    const previousLanguage = this.activeLanguage as PreferredLanguage;
    this.translocoService.setActiveLang(lang);
    this.activeLanguage = lang;

    if (!this.authService.isLoggedIn()) {
      return;
    }

    this.authService.updatePreferredLanguage({ preferredLanguage: lang }).subscribe({
      error: (error) => {
        this.translocoService.setActiveLang(previousLanguage);
        this.activeLanguage = previousLanguage;
        this.notifications.error(error?.error?.message || 'Unable to update preferred language.');
        this.cdr.markForCheck();
      }
    });
  }
  get currentUser() {

  return this.authService.getCurrentUser();
}

get userName(): string {
  return this.formatDisplayName(this.currentUser?.fullName || 'User');
}

get userEmail(): string {
  return this.currentUser?.email || '';
}

get userInitial(): string {
  return this.userName.charAt(0).toUpperCase();
}

profileImageFailedUrl = '';

get profileImageUrl(): string {
  return this.currentUser?.profileImageUrl?.trim() || '';
}

get showAppTopbar(): boolean {
  return !this.isPublicLanding;
}

get showPublicPreferenceControls(): boolean {
  return this.showAppTopbar && !this.isAuthPage;
}

get showThemeSelector(): boolean {
  return this.showPublicPreferenceControls;
}

get showAppMenuToggle(): boolean {
  return this.showAppTopbar && !this.isAuthPage && this.authService.isLoggedIn();
}

get showUserMenu(): boolean {
  return this.showAppTopbar && !this.isAuthPage && this.authService.isLoggedIn() && !!this.currentUser;
}

get isPlusUser(): boolean {
  return userHasPlusPlan(this.authService.getCurrentUser());
}

get isAdminUser(): boolean {
  return (this.currentUser?.role ?? '').toLowerCase() === 'admin';
}

get planCode(): DisplayPlanCode {
  return resolvePlanCode(this.currentUser);
}

get planFee(): string {
  const amounts = this.planAmounts[this.planCode];
  const paise = this.billingCycle === 'yearly' ? amounts.yearly : amounts.monthly;
  if (!paise) {
    return 'Free';
  }

  const amount = `₹${(paise / 100).toLocaleString('en-IN')}`;
  return this.billingCycle === 'yearly' ? `${amount}/yr` : `${amount}/mo`;
}

toggleUserMenu(): void {
  if (!this.showUserMenu) {
    return;
  }

  this.isUserMenuOpen = !this.isUserMenuOpen;
}

handleUserMenuSpace(event: Event): void {
  event.preventDefault();
  this.toggleUserMenu();
}

get themeToggleIcon(): string {
  return this.themeService.mode() === 'dark' ? '\u2600' : '\u263E';
}

get themeToggleLabel(): string {
  return this.themeService.mode() === 'dark' ? 'Dark' : 'Light';
}

get themeToggleTitle(): string {
  return this.themeService.mode() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
}

toggleTheme(): void {
  this.themeService.setTheme(this.themeService.mode() === 'dark' ? 'light' : 'dark');
}

closeUserMenu(): void {
  this.isUserMenuOpen = false;
}

openPlans(): void {
  this.closeUserMenu();
  this.subscriptionModal.open();
}

@HostListener('document:keydown.escape')
closeUserMenuOnEscape(): void {
  this.closeUserMenu();
  this.isGlobalSketchOpen = false;
}

  toggleGlobalSketch(): void {
    if (!this.showUserMenu) {
      return;
    }
    this.isGlobalSketchOpen = !this.isGlobalSketchOpen;
  }

toggleAppMenu(): void {
  window.dispatchEvent(new CustomEvent('app-menu-toggle'));
}

logout(): void {
  this.isUserMenuOpen = false;
  this.authService.logout();
  this.cdr.markForCheck();
  this.router.navigate(['/login']);
}

  private applyCachedPreferredLanguage(): void {
    const preferredLanguage = this.authService.getCurrentUser()?.preferredLanguage;
    if (!preferredLanguage) {
      return;
    }

    this.translocoService.setActiveLang(preferredLanguage);
    this.activeLanguage = preferredLanguage;
  }

  private syncAuthenticatedUserLanguage(): void {
    if (!this.authService.isLoggedIn()) {
      return;
    }

    this.authService.getMe().subscribe({
      next: (user) => {
        if (!user.preferredLanguage) {
          return;
        }

        this.translocoService.setActiveLang(user.preferredLanguage);
        this.activeLanguage = user.preferredLanguage;
        this.cdr.markForCheck();
      }
    });
  }

  private billingCycle: 'monthly' | 'yearly' = 'monthly';
  private planPricingLoaded = false;
  private planAmounts: Record<DisplayPlanCode, { monthly: number; yearly: number }> = {
    Free: { monthly: 0, yearly: 0 },
    Premium: { monthly: 69900, yearly: 699900 },
    PremiumPlus: { monthly: 99900, yearly: 999900 }
  };

  private loadPlanPricing(): void {
    if (!this.authService.isLoggedIn() || this.planPricingLoaded) {
      return;
    }

    this.planPricingLoaded = true;
    this.billingService.getStatus().subscribe({
      next: (status) => {
        const subscriptionStatus = String(status.subscriptionStatus || '').trim().toLowerCase();
        const paid = subscriptionStatus === 'active' || subscriptionStatus === 'authenticated' || subscriptionStatus === 'charged' || subscriptionStatus === 'cancelling';
        const cycle = String(status.billingCycle || '').trim().toLowerCase();
        if (paid && (cycle === 'yearly' || cycle === 'monthly')) {
          this.billingCycle = cycle;
        }
        this.cdr.markForCheck();
      }
    });
    this.billingService.getPlans().subscribe({
      next: (plans) => {
        for (const plan of plans) {
          const code = String(plan.code || '').trim().toLowerCase();
          const key: DisplayPlanCode | null = code === 'premiumplus' || code === 'premium plus'
            ? 'PremiumPlus'
            : code === 'premium'
              ? 'Premium'
              : code === 'free'
                ? 'Free'
                : null;
          if (!key) {
            continue;
          }

          this.planAmounts[key] = {
            monthly: plan.monthlyAmountPaise || 0,
            yearly: plan.yearlyAmountPaise || 0
          };
        }
        this.cdr.markForCheck();
      }
    });
  }

  private updateShellMode(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    this.isPublicLanding = path === '' || path === '/' || path === '/select-language' || path === '/for-astrologers';
    this.isAuthPage = path === '/login' || path === '/register';
    this.isUserMenuOpen = false;
    this.isGlobalSketchOpen = false;
    this.cdr.markForCheck();
  }
  private formatDisplayName(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ') || 'User';
  }
}



