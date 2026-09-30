import { Component, HostListener, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { AuthResponse } from '../core/auth/auth.models';
import { ThemeService } from '../core/theme/theme.service';
import { CartStateService } from '../core/services/cart-state.service';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <div class="store-mandala" aria-hidden="true"></div>
      <header class="topbar">
        <div class="topbar-main">
        <a class="app-brand" routerLink="/" aria-label="HoraLife Store">
          <img class="brand-logo__mark" [src]="logoSrc" alt="">
          <span class="brand-logo__copy">
            <span class="brand-logo__name"><span class="brand-logo__hora">Hora</span><span class="brand-logo__life">Life</span></span>
            <small>Store</small>
          </span>
        </a>
        <nav class="nav">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Catalogue</a>
          @if (signedIn) {
            <a class="cart-link" routerLink="/cart">Cart ({{ cartState.count() }})</a>
            @if (isAdminUser) {
              <a routerLink="/admin/inventory" routerLinkActive="active">Inventory</a>
            }
            <div class="topbar-right" [class.menu-open]="accountOpen">
              <div
                class="user-menu"
                role="button"
                tabindex="0"
                (click)="toggleAccount($event)"
                (keydown.enter)="toggleAccount($event)"
                (keydown.space)="toggleAccount($event)"
                [class.open]="accountOpen"
                [attr.aria-expanded]="accountOpen"
                aria-haspopup="menu"
                aria-controls="store-account-menu"
                aria-label="Open user menu">
                <div class="user-avatar" aria-hidden="true">{{ userInitial }}</div>
                <div class="user-info">
                  <div class="user-name-row">
                    <span class="user-name" [attr.data-plan]="planCode">{{ userName }}</span>
                    @if (isAdminUser) {
                      <span class="user-admin-tag">Admin</span>
                    }
                    @if (planLabel) {
                      <span class="user-plan-mark" [attr.data-plan]="planCode">
                        <span class="user-plan-star" aria-hidden="true"></span>
                        {{ planLabel }}
                      </span>
                    }
                  </div>
                </div>
                <span class="chevron" aria-hidden="true"></span>
              </div>
              @if (accountOpen) {
                <div class="user-dropdown" id="store-account-menu" role="menu">
                  <div class="dropdown-account">
                    <strong class="dropdown-account-name" [attr.data-plan]="planCode">{{ userName }}</strong>
                    @if (isAdminUser) {
                      <span class="user-admin-tag">Admin</span>
                    }
                    <small>{{ userEmail }}</small>
                  </div>
                  <button type="button" role="menuitem" class="logout" (click)="signOut()">
                    <svg class="menu-icon" viewBox="0 0 24 24" aria-hidden="true">
                      <path d="M9 6H6a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h3"></path>
                      <path d="M15 16l4-4-4-4"></path>
                      <path d="M19 12H10"></path>
                    </svg>
                    <span>Sign out</span>
                  </button>
                </div>
              }
            </div>
          } @else {
            <a class="btn" routerLink="/login">Sign in</a>
          }
        </nav>
        </div>
        <p class="demo-notice" role="status">This store is still in demo and is not ready for purchase.</p>
      </header>
      <router-outlet />
    </div>
  `
})
export class ShellComponent implements OnInit {
  accountOpen = false;

  constructor(
    readonly auth: AuthService,
    readonly themes: ThemeService,
    readonly cartState: CartStateService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('handoff');
    if (!code) {
      this.cartState.refresh();
      return;
    }

    this.auth.redeemStoreHandoff(code).subscribe({
      next: () => {
        this.clearHandoffParam();
        this.cartState.refresh();
      },
      error: () => this.clearHandoffParam()
    });
  }

  get signedIn(): boolean {
    this.auth.session();
    return this.auth.isLoggedIn();
  }

  get currentUser(): AuthResponse | null {
    this.auth.session();
    return this.auth.getCurrentUser();
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

  get planCode(): string {
    return this.currentUser?.planCode || 'Free';
  }

  get planLabel(): string {
    if (this.planCode === 'PremiumPlus') {
      return 'Plus';
    }
    if (this.planCode === 'Premium') {
      return 'Premium';
    }
    return '';
  }

  get isAdminUser(): boolean {
    return (this.currentUser?.role ?? '').toLowerCase() === 'admin';
  }

  get logoSrc(): string {
    return this.themes.mode() === 'light'
      ? 'assets/brand/horalife-icon-color.svg'
      : 'assets/brand/horalife-icon-white.svg';
  }

  toggleAccount(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.accountOpen = !this.accountOpen;
  }

  @HostListener('document:click')
  closeAccount(): void {
    this.accountOpen = false;
  }

  signOut(): void {
    this.accountOpen = false;
    this.auth.clearSession();
    this.cartState.clearLocal();
    void this.router.navigateByUrl('/');
  }

  private clearHandoffParam(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { handoff: null },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }
}
