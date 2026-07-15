// Left Sidebar Component
import { Component, HostListener, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AstrologyIconComponent } from '../shared/components/astrology-icon.component';
import { KundaliService } from '../modules/kundali/kundali.service';

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
  imports: [CommonModule, RouterModule, AstrologyIconComponent],
  template: `
    <aside class="sidebar" [class.open]="isOpen" [class.compact]="compact">
      <div class="sidebar-header">
        <button class="collapse-btn" type="button" (click)="toggleCompact()" [attr.aria-label]="compact ? 'Expand sidebar' : 'Collapse sidebar'" [title]="compact ? 'Expand sidebar' : 'Collapse sidebar'">
          <span *ngIf="!compact">&lt;</span>
          <span *ngIf="compact">&gt;</span>
        </button>

        <button class="close-btn" (click)="onClose.emit()" *ngIf="mobileView">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>

      <nav class="sidebar-nav">
        <div class="nav-section">
          
          <h3 class="nav-section-title">Main</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/app/dashboard" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">DB</span>
                <span class="nav-label">Dashboard</span>
              </a>
            </li>
            <li class="nav-item nav-parent">
              <a routerLink="/app/kundali" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
                <app-astrology-icon name="kundali" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">Kundali</span>
              </a>
              <ul class="nav-sublist" *ngIf="!compact">
                <li>
                  <a routerLink="/app/kundali" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                    <span class="nav-dot"></span>
                    <span class="nav-label">Create New</span>
                  </a>
                </li>
                <li class="saved-group">
                  <span class="saved-title">Saved Kundali</span>
                  <a *ngFor="let item of savedKundalis" [routerLink]="['/app/kundali/saved', item.id]" routerLinkActive="active" (click)="onClose.emit()" [title]="item.displayName">
                    <span class="nav-dot saved"></span>
                    <span class="nav-label">{{ item.displayName }}</span>
                  </a>
                  <span class="saved-empty" *ngIf="!savedKundalis.length">No saved kundali</span>
                </li>
              </ul>
            </li>
            <li class="nav-item">
              <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                <app-astrology-icon name="reports" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">Website Home</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/compatibility" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">C</span>
                <span class="nav-label">Compatibility</span>
              </a>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">Astrology</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/app/panchang" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                <span class="nav-icon">P</span>
                <span class="nav-label">Panchang</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/panchang/muhurtha" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">M</span>
                <span class="nav-label">Muhurtha</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/dasha" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">D</span>
                <span class="nav-label">Dasha</span>
              </a>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">Settings</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/app/white-label-settings" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon">W</span>
                <span class="nav-label">White-label</span>
              </a>
            </li>
            <li class="nav-item">
              <a href="#help">
                <span class="nav-icon">?</span>
                <span class="nav-label">Help & Support</span>
              </a>
            </li>
          </ul>
        </div>
      </nav>

      <div class="sidebar-footer">
        <button class="logout-btn">
          <span class="nav-icon">O</span>
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
      height: calc(100dvh - 76px);
      position: fixed;
      left: 0;
      top: 76px;
      display: flex;
      flex-direction: column;
      z-index: 99;
      overflow: hidden;
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

    .sidebar-header {
      display: flex;
      align-items: center;
      justify-content: flex-end;
      gap: 0.5rem;
      padding: 0.85rem 1rem 0.25rem;
    }

    .collapse-btn,
    .close-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      background: transparent;
      border: none;
      cursor: pointer;
      padding: 0.45rem;
      color: #424242;
      border-radius: 0.5rem;
      transition: all 200ms ease;

      &:hover {
        color: #7E3BA8;
        background: #F5F0FA;
      }
    }

    .close-btn {
      display: none;
      padding: 0.5rem;
      margin-bottom: 0;
    }

    .sidebar-nav {
      flex: 1;
      overflow: visible;
      padding: 0.35rem 0 0.75rem;
    }

    .nav-section {
      margin-bottom: 1.2rem;

      &:first-child {
        margin-top: 0.5rem;
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

    .sidebar.compact .nav-section-title {
      display: none;
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
        padding: 0.62rem 1.5rem;
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

    .nav-sublist {
      list-style: none;
      margin: 0.15rem 0 0.4rem 0;
      padding: 0 0 0 2.4rem;
    }

    .nav-sublist a {
      min-height: 34px;
      padding: 0.35rem 1rem 0.35rem 0.5rem;
      border-left: none;
      font-size: 0.78rem;
    }

    .nav-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: #BDBDBD;
      flex: 0 0 6px;
    }

    .nav-dot.saved {
      background: #7E3BA8;
    }

    .saved-group {
      display: grid;
      gap: 0.1rem;
      margin-top: 0.2rem;
    }

    .saved-title,
    .saved-empty {
      display: block;
      padding: 0.35rem 1rem 0.25rem 0.5rem;
      color: #8A8A8A;
      font-size: 0.7rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .saved-empty {
      text-transform: none;
      font-weight: 500;
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

    .sidebar.compact .nav-item a {
      justify-content: center;
      padding: 0.75rem 0;
      gap: 0;
    }

    .sidebar.compact .nav-icon {
      margin-right: 0;
    }

    .nav-label {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .sidebar-footer {
      margin-top: auto;
      padding: 0.75rem 1rem;
      border-top: 1px solid #E0E0E0;
    }

    .sidebar.compact .sidebar-footer {
      padding: 0.75rem 0.5rem;
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

    .sidebar.compact .logout-btn {
      justify-content: center;
      padding: 0.75rem 0.25rem;
    }

    .sidebar.compact .logout-btn .nav-label {
      display: none;
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

    /* Runtime theme sidebar */
    :host {
      color: var(--theme-text);
    }

    .sidebar {
      border-right-color: var(--theme-line);
      background: var(--theme-surface);
      color: var(--theme-text);
      box-shadow: 10px 0 34px var(--theme-shadow);
    }

    .collapse-btn,
    .close-btn,
    .nav-item a,
    .logout-btn {
      color: var(--theme-text-muted);
    }

    .collapse-btn:hover,
    .close-btn:hover,
    .nav-item a:hover {
      color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 10%, transparent);
    }

    .nav-item a.active {
      border-left-color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 12%, var(--theme-surface));
      color: var(--theme-accent);
    }

    .nav-section-title,
    .saved-title,
    .saved-empty {
      color: var(--theme-text-muted);
    }

    .nav-dot {
      background: color-mix(in srgb, var(--theme-text-muted) 52%, transparent);
    }

    .nav-dot.saved {
      background: var(--theme-accent);
    }

    .sidebar-footer,
    .logout-btn {
      border-color: var(--theme-line);
    }

    .logout-btn {
      background: color-mix(in srgb, var(--theme-surface-2) 74%, transparent);
    }

    .logout-btn:hover {
      background: color-mix(in srgb, var(--theme-danger) 12%, transparent);
      color: var(--theme-danger);
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
export class SidebarComponent implements OnInit {
  @Input() isOpen: boolean = false;
  @Input() compact: boolean = false;
  @Input() mobileView: boolean = false;
  @Output() onClose = new EventEmitter<void>();
  @Output() onToggleCompact = new EventEmitter<void>();

  navItems: NavItem[] = [];
  savedKundalis: any[] = [];

  constructor(private kundaliService: KundaliService) {}

  ngOnInit(): void {
    this.loadSavedKundalis();
  }

  @HostListener('window:kundali-saved')
  onKundaliSaved(): void {
    this.loadSavedKundalis();
  }

  private loadSavedKundalis(): void {
    this.kundaliService.getSavedKundalis().subscribe({
      next: (items) => {
        this.savedKundalis = Array.isArray(items) ? items : [];
      },
      error: (error) => {
        console.error('Saved kundali menu load error:', error);
        this.savedKundalis = [];
      }
    });
  }

  toggleCompact(): void {
    this.onToggleCompact.emit();
  }
}






