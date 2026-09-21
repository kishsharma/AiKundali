// Main Layout Shell Component
import { Component, HostListener, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterOutlet, RouterModule } from '@angular/router';
import { filter, map } from 'rxjs/operators';
import { NavbarComponent } from './navbar.component';
import { SidebarComponent } from './sidebar.component';
import { DhruvaAssistantComponent } from '../shared/components/dhruva-assistant/dhruva-assistant.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule, SidebarComponent, DhruvaAssistantComponent],
  template: `
    <div class="app-shell">
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
        <main class="content" [class.sidebar-open]="toggleSidebar() && isMobileView()">
          <div class="content-wrapper" [class.content-wrapper--wide]="wideContent()">
            <router-outlet></router-outlet>
          </div>
        </main>
      </div>

      <app-dhruva-assistant></app-dhruva-assistant>
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
    return url.includes('/app/kundali');
  }

  @HostListener('window:app-menu-toggle')
  toggleMobileSidebar(): void {
    if (this.isMobileView()) {
      this.toggleSidebar.set(!this.toggleSidebar());
    }
  }
}



