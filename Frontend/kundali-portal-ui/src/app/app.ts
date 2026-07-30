import { Component, HostListener } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/services/theme.service';
import { BrandLogoComponent } from './shared/components/brand-logo/brand-logo.component';
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
  readonly languages = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'हिंदी' },
    { code: 'kn', label: 'ಕನ್ನಡ' }
  ];
  readonly themes = [
    { mode: 'dark', label: 'Dark', icon: '☾' },
    { mode: 'light', label: 'Light', icon: '☀' },
    { mode: 'hora', label: 'Hora', icon: 'H' }
  ] as const;
  activeLanguage: string;
  isPublicLanding = true;
 isUserMenuOpen = false;
 pageTitle = 'Kundali Portal';
  constructor(private translocoService: TranslocoService, private router: Router, public authService: AuthService, public themeService: ThemeService) {
    this.activeLanguage = this.translocoService.getActiveLang();
    this.updateShellMode(this.router.url);
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe((event) => this.updateShellMode(event.urlAfterRedirects));
  }

  changeLanguage(lang: string) {
    this.translocoService.setActiveLang(lang);
    this.activeLanguage = lang;
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

  private updateShellMode(url: string): void {
    const path = url.split('?')[0].split('#')[0];
    this.isPublicLanding = path === '' || path === '/';
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


