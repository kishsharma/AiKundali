// Left Sidebar Component
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

export interface NavItem {
  label: string;
  routerLink: string;
  icon?: string;
  badge?: number;
  children?: NavItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <aside class="sidebar" [class.open]="isOpen" [class.compact]="compact">
      <button class="close-btn" (click)="onClose.emit()" *ngIf="mobileView">
        <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
          <line x1="18" y1="6" x2="6" y2="18"></line>
          <line x1="6" y1="6" x2="18" y2="18"></line>
        </svg>
      </button>

      <nav class="sidebar-nav">
        <div class="nav-section">
          
          <h3 class="nav-section-title">Main</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/dashboard" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">📊</span>
                <span class="nav-label">Dashboard</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/kundali" routerLinkActive="active" >
                <span class="nav-icon">♈</span>
                <span class="nav-label">Kundali</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/compatibility" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">💑</span>
                <span class="nav-label">Compatibility</span>
              </a>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">Astrology</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/panchang" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">📅</span>
                <span class="nav-label">Panchang</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/dasha" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">⏰</span>
                <span class="nav-label">Dasha</span>
              </a>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">Settings</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a href="#settings">
                <span class="nav-icon">⚙️</span>
                <span class="nav-label">Preferences</span>
              </a>
            </li>
            <li class="nav-item">
              <a href="#help">
                <span class="nav-icon">❓</span>
                <span class="nav-label">Help & Support</span>
              </a>
            </li>
          </ul>
        </div>
      </nav>

      <div class="sidebar-footer">
        <button class="logout-btn">
          <span class="nav-icon">🚪</span>
          <span class="nav-label">Logout</span>
        </button>
      </div>
    </aside>

    <!-- Mobile Overlay -->
    <div *ngIf="isOpen && mobileView" class="sidebar-overlay" (click)="onClose.emit()"></div>
  `,
  styles: [`
    .sidebar {
      width: 250px;
      background: #FFFFFF;
      border-right: 1px solid #E0E0E0;
      height: calc(100vh - 60px);
      position: fixed;
      left: 0;
      top: 60px;
      display: flex;
      flex-direction: column;
      z-index: 99;
      overflow-y: auto;
      transition: all 300ms ease;

      &.compact {
        width: 80px;

        .nav-label {
          display: none;
        }

        .nav-icon {
          margin-right: 0;
        }
      }
    }

    .close-btn {
      display: none;
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 1rem;
      color: #424242;
      margin-bottom: 1rem;

      &:hover {
        color: #7E3BA8;
      }
    }

    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: 1rem 0;
    }

    .nav-section {
      margin-bottom: 2rem;

      &:first-child {
        margin-top: 1rem;
      }
    }

    .nav-section-title {
      margin: 0 0 0.75rem 1.5rem;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
      color: #BDBDBD;
      letter-spacing: 0.5px;
    }

    .nav-list {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .nav-item {
      a {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.75rem 1.5rem;
        color: #424242;
        text-decoration: none;
        font-size: 0.875rem;
        font-weight: 500;
        border-left: 3px solid transparent;
        transition: all 300ms ease;
        cursor: pointer;

        &:hover {
          background: #F0F0F0;
          color: #7E3BA8;
        }

        &.active {
          background: linear-gradient(90deg, rgba(126, 59, 168, 0.1), transparent);
          color: #7E3BA8;
          border-left-color: #7E3BA8;
        }
      }
    }

    .nav-icon {
      display: inline-flex;
      width: 20px;
      height: 20px;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      margin-right: 0.5rem;
      font-size: 1rem;
    }

    .nav-label {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sidebar-footer {
      padding: 1rem;
      border-top: 1px solid #E0E0E0;
    }

    .logout-btn {
      width: 100%;
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem 1rem;
      background: #F0F0F0;
      border: 1px solid #E0E0E0;
      border-radius: 0.5rem;
      color: #424242;
      font-size: 0.875rem;
      font-weight: 500;
      cursor: pointer;
      transition: all 300ms ease;

      &:hover {
        background: #E0E0E0;
        color: #F44336;
      }
    }

    .sidebar-overlay {
      display: none;
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      z-index: 98;
    }

    @media (max-width: 768px) {
      .sidebar {
        width: 100%;
        max-width: 280px;
        left: -100%;

        &.open {
          left: 0;
        }
      }

      .close-btn {
        display: flex;
      }

      .sidebar-overlay {
        display: block;
      }
    }
  `],
})
export class SidebarComponent {
  @Input() isOpen: boolean = false;
  @Input() compact: boolean = false;
  @Input() mobileView: boolean = false;
  @Output() onClose = new EventEmitter<void>();

  navItems: NavItem[] = [];
}
