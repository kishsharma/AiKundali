// Top Navbar Component
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <header class="navbar" [class.compact]="compact">
      <div class="navbar-container">
        <!-- Logo / Brand -->
        <div class="navbar-brand">
          <button class="menu-toggle" (click)="onToggleSidebar.emit()" title="Toggle Sidebar">
            <span class="hamburger"></span>
          </button>
          <a routerLink="/" class="brand-text">
            <span class="brand-icon">♈</span>
            <span class="brand-name">Kundali Portal</span>
          </a>
        </div>

        <!-- Nav Links (Center) -->
        <nav class="navbar-nav">
          <a routerLink="/app/dashboard" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
            Dashboard
          </a>
          <a routerLink="/app/kundali" routerLinkActive="active">
            Kundali
          </a>
          <a routerLink="/app/compatibility" routerLinkActive="active">
            Compatibility
          </a>
          <a routerLink="/app/panchang" routerLinkActive="active">
            Panchang
          </a>
          <a routerLink="/app/dasha" routerLinkActive="active">
            Dasha
          </a>
        </nav>

        <!-- Right Actions -->
        <div class="navbar-actions">
          <button class="action-btn" title="Notifications">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
              <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
            </svg>
          </button>
          <button class="action-btn" title="Theme Toggle">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor">
              <circle cx="12" cy="12" r="5"></circle>
              <line x1="12" y1="1" x2="12" y2="3"></line>
              <line x1="12" y1="21" x2="12" y2="23"></line>
              <line x1="4.22" y1="4.22" x2="5.64" y2="5.64"></line>
              <line x1="18.36" y1="18.36" x2="19.78" y2="19.78"></line>
              <line x1="1" y1="12" x2="3" y2="12"></line>
              <line x1="21" y1="12" x2="23" y2="12"></line>
              <line x1="4.22" y1="19.78" x2="5.64" y2="18.36"></line>
              <line x1="18.36" y1="5.64" x2="19.78" y2="4.22"></line>
            </svg>
          </button>
          <div class="user-menu">
            <button class="user-avatar" title="User Menu">
              <span>{{ userInitials }}</span>
            </button>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .navbar {
      background: #FFFFFF;
      border-bottom: 1px solid #E0E0E0;
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
      position: sticky;
      top: 0;
      z-index: 100;
      transition: all 300ms ease;

      &.compact {
        padding: 0.5rem 1rem;
      }
    }

    .navbar-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      max-width: 100%;
    }

    .navbar-brand {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-shrink: 0;
    }

    .menu-toggle {
      display: none;
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 0.5rem;
      color: #7E3BA8;
      transition: color 300ms ease;

      &:hover {
        color: #FF1E64;
      }

      .hamburger {
        display: flex;
        flex-direction: column;
        gap: 4px;
        width: 24px;
        height: 24px;

        &::before,
        &::after {
          content: '';
          display: block;
          width: 100%;
          height: 2px;
          background: currentColor;
          border-radius: 1px;
          transition: all 300ms ease;
        }

        &::before {
          box-shadow: 0 8px 0 currentColor;
        }
      }
    }

    .brand-text {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      text-decoration: none;
    }

    .brand-icon {
      font-size: 1.5rem;
    }

    .brand-name {
      font-weight: 600;
      font-size: 1.125rem;
      background: linear-gradient(135deg, #7E3BA8, #FF1E64);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .navbar-nav {
      display: flex;
      align-items: center;
      gap: 0;
      flex: 1;
      justify-content: center;
      margin: 0 2rem;

      a {
        padding: 0.5rem 1rem;
        color: #424242;
        text-decoration: none;
        font-size: 0.875rem;
        font-weight: 500;
        border-bottom: 2px solid transparent;
        transition: all 300ms ease;
        white-space: nowrap;

        &:hover {
          color: #7E3BA8;
        }

        &.active {
          color: #7E3BA8;
          border-bottom-color: #7E3BA8;
        }
      }
    }

    .navbar-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      flex-shrink: 0;
    }

    .action-btn {
      background: transparent;
      border: none;
      cursor: pointer;
      color: #424242;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 0.5rem;
      border-radius: 0.5rem;
      transition: all 300ms ease;

      &:hover {
        background: #F0F0F0;
        color: #7E3BA8;
      }
    }

    .user-menu {
      display: flex;
      align-items: center;
    }

    .user-avatar {
      background: linear-gradient(135deg, #7E3BA8, #FF1E64);
      color: white;
      width: 36px;
      height: 36px;
      border-radius: 50%;
      border: none;
      cursor: pointer;
      font-weight: 600;
      font-size: 0.875rem;
      display: flex;
      align-items: center;
      justify-content: center;
      transition: transform 300ms ease;

      &:hover {
        transform: scale(1.05);
      }
    }

    @media (max-width: 768px) {
      .navbar-container {
        padding: 0.75rem 1rem;
      }

      .menu-toggle {
        display: flex;
      }

      .navbar-nav {
        display: none;
      }

      .brand-name {
        display: none;
      }
    }
  `],
})
export class NavbarComponent {
  @Input() compact: boolean = false;
  @Input() userInitials: string = 'US';
  @Output() onToggleSidebar = new EventEmitter<void>();
}
