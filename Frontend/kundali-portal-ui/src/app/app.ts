import { Component } from '@angular/core';
import { NgIf } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { filter } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  imports: [NgIf, RouterOutlet,
  RouterLink],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})

export class App {
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
  return this.currentUser?.fullName || 'User';
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
  console.log('user drodpwnclicked',true);
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
}

