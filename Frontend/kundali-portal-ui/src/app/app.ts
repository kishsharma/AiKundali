import { Component, HostListener } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { PreferredLanguage } from './core/auth/auth.models';
import { ThemeService } from './core/services/theme.service';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';
import { NotificationService } from './shared/services/notification.service';
import { NotificationToastComponent } from './shared/components/notification-toast/notification-toast.component';

@Component({
  selector: 'app-root',
  imports: [NgFor, NgIf, RouterOutlet,
  RouterLink,
  BrandLogoComponent,
  NotificationToastComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})

export class App {
  readonly languages: ReadonlyArray<{ code: PreferredLanguage; label: string }> = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'Hindi' },
    { code: 'kn', label: 'Kannada' }
  ];
  readonly themes = [
    { mode: 'dark', label: 'Dark', icon: '\u263E' },
    { mode: 'light', label: 'Light', icon: '\u263C' }
  ] as const;
  activeLanguage: string;
  isPublicLanding = true;
  isAuthPage = false;
 isUserMenuOpen = false;
 pageTitle = 'Kundali Portal';
  constructor(
    private translocoService: TranslocoService,
    private router: Router,
    public authService: AuthService,
    public themeService: ThemeService,
    private notifications: NotificationService
  ) {
    this.activeLanguage = this.translocoService.getActiveLang();
    this.translocoService.langChanges$.subscribe((lang) => {
      this.activeLanguage = lang;
    });
    this.applyCachedPreferredLanguage();
    this.syncAuthenticatedUserLanguage();
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
  return this.showAppTopbar && this.authService.isLoggedIn();
}

get showUserMenu(): boolean {
  return this.showAppTopbar && this.authService.isLoggedIn();
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

@HostListener('document:keydown.escape')
closeUserMenuOnEscape(): void {
  this.closeUserMenu();
}

toggleAppMenu(): void {
  window.dispatchEvent(new CustomEvent('app-menu-toggle'));
}

logout(): void {
  this.isUserMenuOpen = false;
  this.authService.logout();
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
      }
    });
  }

  private updateShellMode(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    this.isPublicLanding = path === '' || path === '/' || path === '/select-language';
    this.isAuthPage = path === '/login' || path === '/register';
    this.isUserMenuOpen = false;
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



