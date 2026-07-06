// Main Layout Shell Component
import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterModule } from '@angular/router';
import { NavbarComponent } from './navbar.component';
import { SidebarComponent } from './sidebar.component';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule, SidebarComponent],
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
        <main class="content" [class.sidebar-open]="toggleSidebar() && isMobileView()" [class.sidebar-collapsed]="compactSidebar() && !isMobileView()">
          <div class="content-wrapper">
            <router-outlet></router-outlet>
          </div>
        </main>
      </div>
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
      margin-left: 250px;
      transition: all 300ms ease;
    }

    .content.sidebar-collapsed {
      margin-left: 80px;
    }

    .content-wrapper {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
      width: 100%;
    }

    @media (max-width: 768px) {
      .content {
        &.sidebar-open {
          pointer-events: none;
          opacity: 0.7;
        }
      }

      .content-wrapper {
        padding: 1rem;
      }
    }

    /* Scrollbar Styling */
    .content::-webkit-scrollbar {
      width: 8px;
    }

    .content::-webkit-scrollbar-track {
      background: transparent;
    }

    .content::-webkit-scrollbar-thumb {
      background: #BDBDBD;
      border-radius: 4px;

      &:hover {
        background: #757575;
      }
    }
  `],
})
export class LayoutComponent {
  toggleSidebar = signal(false);
  compactSidebar = signal(false);
  isMobileView = signal(window.innerWidth < 768);
  userInitials = 'US';

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
    this.compactSidebar.set(!this.compactSidebar());
  }
}
