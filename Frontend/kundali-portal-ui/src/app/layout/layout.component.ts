// Main Layout Shell Component
import { Component, HostListener, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet, RouterModule } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { NavbarComponent } from './navbar.component';
import { SidebarComponent } from './sidebar.component';
import { DhruvaAssistantComponent } from '../shared/components/dhruva-assistant/dhruva-assistant.component';
import { FeedbackBadgeComponent } from '../shared/components/feedback-badge/feedback-badge.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule, SidebarComponent, DhruvaAssistantComponent, FeedbackBadgeComponent],
  template: `
    <div class="app-shell" [class.app-shell--fixed]="fixedViewport()">
      <!-- Top Navbar -->
      <!-- <app-navbar (onToggleSidebar)="toggleSidebar.set(!toggleSidebar())" [userInitials]="userInitials"></app-navbar> -->

      <!-- Main Container -->
      <div class="app-container">
        <!-- Sidebar -->
        <app-sidebar
          [isOpen]="toggleSidebar()"
          [compact]="compactSidebar()"
          [mobileView]="isMobileView()"
          (onClose)="toggleSidebar.set(false)"
          (onToggleCompact)="toggleCompactSidebar()"
        ></app-sidebar>

        <!-- Content Area -->
        <main
          class="content"
          [class.sidebar-open]="toggleSidebar() && isMobileView()"
          [class.content--fixed]="fixedViewport()">
          <div
            class="content-wrapper"
            [class.content-wrapper--wide]="wideContent()"
            [class.content-wrapper--fixed]="fixedViewport()">
            <router-outlet></router-outlet>
          </div>
        </main>
      </div>

      <app-dhruva-assistant></app-dhruva-assistant>
      <app-feedback-badge></app-feedback-badge>
    </div>
  `,
  styles: [`
    .app-shell {
      display: flex;
      flex-direction: column;
      height: 100vh;
      background: #FAFAFA;
    }

    .app-container {
      display: flex;
      flex: 1;
      overflow: hidden;
    }

    .content {
      flex: 1;
      overflow-y: auto;
      overflow-x: hidden;
      padding: 0;
      margin-left: 80px;
      transition: opacity 300ms ease;
    }

    .content-wrapper {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
      width: 100%;
      min-width: 0;
    }

    .content-wrapper--wide {
      max-width: 1680px;
      padding: 1.15rem 1.5rem 2rem;
    }

    @media (max-width: 768px) {
      .content {
        margin-left: 0;
        width: 100%;

        &.sidebar-open {
          pointer-events: none;
          opacity: 0.7;
        }
      }

      .content-wrapper {
        padding: 1rem;
      }
    }

    /* Runtime theme shell */
    :host {
      display: block;
      min-height: 100dvh;
      background: var(--theme-bg);
      color: var(--theme-text);
    }

    .app-shell {
      min-height: 100dvh;
      height: auto;
      background: var(--theme-bg);
      color: var(--theme-text);
    }

    /* Consultation: lock page scroll; only the board workspace scrolls. */
    .app-shell.app-shell--fixed {
      height: 100dvh;
      min-height: 100dvh;
      max-height: 100dvh;
      overflow: hidden;
    }

    .app-shell.app-shell--fixed .app-container {
      flex: 1 1 auto;
      min-height: 0;
      overflow: hidden;
    }

    .content.content--fixed {
      display: flex;
      flex-direction: column;
      flex: 1 1 auto;
      min-height: 0;
      overflow: hidden;
    }

    .content-wrapper.content-wrapper--fixed {
      display: flex;
      flex-direction: column;
      flex: 1 1 auto;
      width: 100%;
      max-width: 1680px;
      min-height: 0;
      height: 100%;
      overflow: hidden;
      padding-top: 1rem;
      padding-bottom: 1rem;
    }

    .content-wrapper.content-wrapper--fixed > *:not(router-outlet) {
      display: flex;
      flex-direction: column;
      flex: 1 1 auto;
      min-height: 0;
      height: 100%;
      overflow: hidden;
    }

    @media (max-width: 768px) {
      .content-wrapper.content-wrapper--fixed {
        padding: 0.75rem 1rem;
      }
    }

    .app-container,
    .content,
    .content-wrapper {
      background: transparent;
      color: var(--theme-text);
    }

    .content {
      scrollbar-color: color-mix(in srgb, var(--theme-accent) 42%, transparent) transparent;
    }
    /* Scrollbar Styling */
    .content::-webkit-scrollbar {
      width: 8px;
    }

    .content::-webkit-scrollbar-track {
      background: transparent;
    }

    .content::-webkit-scrollbar-thumb {
      background: color-mix(in srgb, var(--theme-accent) 42%, transparent);
      border-radius: 4px;

      &:hover {
        background: color-mix(in srgb, var(--theme-accent) 62%, transparent);
      }
    }

    @media print {
      :host,
      .app-shell,
      .app-container,
      .content,
      .content-wrapper {
        position: static !important;
        height: auto !important;
        min-height: 0 !important;
        max-height: none !important;
        overflow: visible !important;
        background: #fff !important;
        transform: none !important;
      }

      app-sidebar {
        display: none !important;
      }

      .content {
        margin-left: 0 !important;
      }

      .content-wrapper {
        max-width: none !important;
        padding: 0 !important;
      }
    }
  `],
})
export class LayoutComponent {
  private readonly router = inject(Router);
  toggleSidebar = signal(false);
  compactSidebar = signal(false);
  isMobileView = signal(window.innerWidth < 768);
  userInitials = 'US';
  readonly wideContent = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => this.isWideContentUrl(event.urlAfterRedirects))
    ),
    { initialValue: this.isWideContentUrl(this.router.url) }
  );
  readonly fixedViewport = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map((event) => this.isFixedViewportUrl(event.urlAfterRedirects))
    ),
    { initialValue: this.isFixedViewportUrl(this.router.url) }
  );

  constructor() {
    // Handle window resize
    window.addEventListener('resize', () => {
      const isMobile = window.innerWidth < 768;
      this.isMobileView.set(isMobile);

      // Close sidebar on mobile when resizing to desktop
      if (!isMobile && this.toggleSidebar()) {
        this.toggleSidebar.set(false);
      }
    });
  }

  toggleCompactSidebar(): void {
    if (this.isMobileView()) {
      this.compactSidebar.set(false);
      return;
    }

    this.compactSidebar.set(!this.compactSidebar());
  }

  private isWideContentUrl(url: string): boolean {
    return url.includes('/app/kundali') || url.includes('/app/user-login-control');
  }

  private isFixedViewportUrl(url: string): boolean {
    return url.includes('/app/consultation');
  }

  @HostListener('window:app-menu-toggle')
  toggleMobileSidebar(): void {
    if (this.isMobileView()) {
      this.toggleSidebar.set(!this.toggleSidebar());
    }
  }
}



