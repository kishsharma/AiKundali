// Left Sidebar Component
import { Component, HostListener, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { AstrologyIconComponent } from '../shared/components/astrology-icon.component';
import { KundaliService } from '../modules/kundali/kundali.service';
import { AuthService } from '../core/auth/auth.service';
import { CompatibilityApiService, SavedCompatibilityListItem } from '../core/services/compatibility-api.service';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';

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
        <button class="collapse-btn" type="button" (click)="toggleCompact()" [attr.aria-label]="compact ? 'Expand menu bar' : 'Collapse menu bar'" [title]="compact ? 'Expand menu bar' : 'Collapse menu bar'">
          <svg *ngIf="!compact" class="collapse-icon" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
            <path d="M11 17l-5-5 5-5"></path>
            <path d="M18 17l-5-5 5-5"></path>
          </svg>
          <svg *ngIf="compact" class="collapse-icon" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
            <path d="M13 17l5-5-5-5"></path>
            <path d="M6 17l5-5-5-5"></path>
          </svg>
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
              <a routerLink="/app/dashboard" routerLinkActive="active" data-dhruva-tour="dashboard" data-tour-id="portal-dashboard" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <rect x="3" y="3" width="7" height="8" rx="1.5"></rect>
                    <rect x="14" y="3" width="7" height="5" rx="1.5"></rect>
                    <rect x="14" y="12" width="7" height="9" rx="1.5"></rect>
                    <rect x="3" y="15" width="7" height="6" rx="1.5"></rect>
                  </svg>
                </span>
                <span class="nav-label">Dashboard</span>
              </a>
            </li>
            <li class="nav-item nav-parent">
              <a routerLink="/app/kundali" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" data-dhruva-tour="create-kundali" data-tour-id="portal-create-kundali">
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
                  <button
                    type="button"
                    class="saved-toggle"
                    (click)="toggleSavedKundalis($event)"
                    [attr.aria-expanded]="savedKundalisOpen"
                    aria-controls="saved-kundali-list" data-dhruva-tour="saved-kundalis" data-tour-id="portal-saved-kundalis">
                    <span>Saved Kundali</span>
                    <span class="saved-count">{{ savedKundalis.length }}</span>
                    <svg class="saved-chevron" [class.open]="savedKundalisOpen" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="m6 9 6 6 6-6"></path>
                    </svg>
                  </button>
                  <div id="saved-kundali-list" class="saved-list" *ngIf="savedKundalisOpen">
                    <div class="saved-row" *ngFor="let item of savedKundalis" [class.deleting]="deletingKundaliIds.has(item.id)" [attr.aria-busy]="deletingKundaliIds.has(item.id)">
                      <a [routerLink]="['/app/kundali/saved', item.id]" routerLinkActive="active" (click)="onClose.emit()" [title]="item.displayName">
                        <span class="nav-dot saved"></span>
                        <span class="nav-label">{{ deletingKundaliIds.has(item.id) ? 'Deleting...' : item.displayName }}</span>
                      </a>
                      <button
                        type="button"
                        class="saved-delete-btn"
                        (click)="deleteSavedKundali(item, $event)"
                        [disabled]="deletingKundaliIds.has(item.id)"
                        [attr.aria-label]="'Delete saved Kundali ' + item.displayName"
                        [title]="deletingKundaliIds.has(item.id) ? 'Deleting...' : 'Delete saved Kundali'">
                        {{ deletingKundaliIds.has(item.id) ? 'Deleting...' : 'X' }}
                      </button>
                    </div>
                    <span class="saved-status" *ngIf="deleteStatusMessage">{{ deleteStatusMessage }}</span>
                    <span class="saved-empty" *ngIf="!savedKundalis.length">No saved kundali</span>
                  </div>
                </li>
              </ul>
            </li>
            <li class="nav-item">
              <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M3 10.8 12 3l9 7.8"></path>
                    <path d="M5.5 9.5V21h13V9.5"></path>
                    <path d="M9.5 21v-6h5v6"></path>
                  </svg>
                </span>
                <span class="nav-label">Website Home</span>
              </a>
            </li>
            <li class="nav-item nav-parent">
              <a routerLink="/app/compatibility" routerLinkActive="active" data-dhruva-tour="compatibility" data-tour-id="portal-compatibility" [routerLinkActiveOptions]="{ exact: true }">
                <app-astrology-icon name="compatibility" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">Compatibility</span>
              </a>
              <ul class="nav-sublist" *ngIf="!compact">
                <li>
                  <a routerLink="/app/compatibility" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                    <span class="nav-dot"></span>
                    <span class="nav-label">Create New</span>
                  </a>
                </li>
                <li class="saved-group">
                  <button
                    type="button"
                    class="saved-toggle"
                    (click)="toggleSavedMatches($event)"
                    [attr.aria-expanded]="savedMatchesOpen"
                    aria-controls="saved-match-list">
                    <span>Saved Matches</span>
                    <span class="saved-count">{{ savedMatches.length }}</span>
                    <svg class="saved-chevron" [class.open]="savedMatchesOpen" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="m6 9 6 6 6-6"></path>
                    </svg>
                  </button>
                  <div id="saved-match-list" class="saved-list" *ngIf="savedMatchesOpen">
                    <div class="saved-row" *ngFor="let item of savedMatches" [class.deleting]="deletingCompatibilityIds.has(item.id)" [attr.aria-busy]="deletingCompatibilityIds.has(item.id)">
                      <a [routerLink]="['/app/compatibility/saved', item.id]" routerLinkActive="active" (click)="onClose.emit()" [title]="item.displayName || (item.personAName + ' & ' + item.personBName)">
                        <span class="nav-dot saved match"></span>
                        <span class="nav-label">{{ deletingCompatibilityIds.has(item.id) ? 'Deleting...' : (item.displayName || (item.personAName + ' & ' + item.personBName)) }}</span>
                      </a>
                      <button
                        type="button"
                        class="saved-delete-btn"
                        (click)="deleteSavedCompatibility(item, $event)"
                        [disabled]="deletingCompatibilityIds.has(item.id)"
                        [attr.aria-label]="'Delete saved match ' + (item.displayName || item.personAName + ' and ' + item.personBName)"
                        [title]="deletingCompatibilityIds.has(item.id) ? 'Deleting...' : 'Delete saved match'">
                        {{ deletingCompatibilityIds.has(item.id) ? 'Deleting...' : 'X' }}
                      </button>
                    </div>
                    <span class="saved-status" *ngIf="compatibilityDeleteStatusMessage">{{ compatibilityDeleteStatusMessage }}</span>
                    <span class="saved-empty" *ngIf="!savedMatches.length">No saved matches</span>
                  </div>
                </li>
              </ul>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">Astrology</h3>
          <ul class="nav-list">
            <li class="nav-item">
              <a routerLink="/app/panchang" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                <app-astrology-icon name="panchang" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">Panchang</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/panchang/muhurtha" routerLinkActive="active" data-dhruva-tour="muhurtha" data-tour-id="portal-muhurtha" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <circle cx="12" cy="12" r="8.5"></circle>
                    <path d="M12 7v5l3.2 2"></path>
                    <path d="M12 1.8v2"></path>
                    <path d="M12 20.2v2"></path>
                    <path d="M1.8 12h2"></path>
                    <path d="M20.2 12h2"></path>
                  </svg>
                </span>
                <span class="nav-label">Muhurtha</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/learning" routerLinkActive="active" data-dhruva-tour="learning" data-tour-id="portal-learning" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M4 19.5A2.5 2.5 0 0 1 6.5 17H20"></path>
                    <path d="M6.5 2H20v20H6.5A2.5 2.5 0 0 1 4 19.5v-15A2.5 2.5 0 0 1 6.5 2z"></path>
                    <path d="M8 6h8"></path>
                    <path d="M8 10h6"></path>
                  </svg>
                </span>
                <span class="nav-label">Jyotish Study</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/dasha" routerLinkActive="active" (click)="onClose.emit()">
                <app-astrology-icon name="dasha" class="nav-icon icon-md"></app-astrology-icon>
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
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M4 6.5A2.5 2.5 0 0 1 6.5 4h11A2.5 2.5 0 0 1 20 6.5v11a2.5 2.5 0 0 1-2.5 2.5h-11A2.5 2.5 0 0 1 4 17.5z"></path>
                    <path d="M8 9.2h8"></path>
                    <path d="M8 14.8h4.8"></path>
                    <path d="M15 13.2l1.8 1.8L15 16.8"></path>
                  </svg>
                </span>
                <span class="nav-label">White-label</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/user-login-control" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2"></path>
                    <circle cx="9.5" cy="7" r="4"></circle>
                    <path d="M17 11l2 2 4-4"></path>
                  </svg>
                </span>
                <span class="nav-label">User Login</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/audit-logs" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M8 6h11"></path>
                    <path d="M8 12h11"></path>
                    <path d="M8 18h11"></path>
                    <path d="M4 6h.01"></path>
                    <path d="M4 12h.01"></path>
                    <path d="M4 18h.01"></path>
                  </svg>
                </span>
                <span class="nav-label">Audit Logs</span>
              </a>
            </li>
            <li class="nav-item">
              <a href="#help">
                <app-astrology-icon name="info" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">Help & Support</span>
              </a>
            </li>
          </ul>
        </div>
      </nav>

      <div class="sidebar-footer">
        <button class="logout-btn" type="button" (click)="logout()">
          <span class="nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 21H6.5A2.5 2.5 0 0 1 4 18.5v-13A2.5 2.5 0 0 1 6.5 3H9"></path>
              <path d="M15 17l5-5-5-5"></path>
              <path d="M20 12H9"></path>
            </svg>
          </span>
          <span class="nav-label">Logout</span>
        </button>
      </div>
    </aside>

    <!-- Mobile Overlay -->
    <div *ngIf="isOpen && mobileView" class="sidebar-overlay" (click)="onClose.emit()"></div>
  `,
  styles: [`
    .sidebar {
      width: 240px;
      background: #FFFFFF;
      border-right: 1px solid #E0E0E0;
      height: calc(100dvh - 76px);
      position: fixed;
      left: 0;
      top: 76px;
      display: flex;
      flex-direction: column;
      z-index: 110;
      overflow: hidden;
      transition: width 260ms ease, box-shadow 260ms ease, transform 260ms ease;

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

    .collapse-btn {
      width: 2rem;
      height: 2rem;
      padding: 0;
      border: 1px solid #E0E0E0;
      background: #FFFFFF;
      box-shadow: 0 6px 18px rgba(30, 20, 40, 0.08);

      &:hover,
      &:focus-visible {
        border-color: #C9A7E2;
        color: #7E3BA8;
        background: #F8F1FC;
        box-shadow: 0 8px 22px rgba(126, 59, 168, 0.14);
      }
    }

    .collapse-icon {
      display: block;
      flex: 0 0 auto;
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
    .saved-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto;
      align-items: center;
      gap: 0.15rem;
    }

    .saved-row a {
      min-width: 0;
    }

    .saved-row.deleting a {
      pointer-events: none;
      opacity: 0.72;
    }

    .saved-delete-btn {
      min-width: 26px;
      height: 26px;
      padding: 0 0.45rem;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      border: 1px solid transparent;
      border-radius: 0.35rem;
      background: transparent;
      color: var(--theme-text-muted);
      cursor: pointer;
      font-size: 0.68rem;
      font-weight: 700;
      line-height: 1;
    }

    .saved-delete-btn:hover:not(:disabled) {
      border-color: color-mix(in srgb, var(--theme-danger) 26%, transparent);
      background: color-mix(in srgb, var(--theme-danger) 10%, transparent);
      color: var(--theme-danger);
    }

    .saved-delete-btn:disabled {
      cursor: default;
      opacity: 0.72;
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

    .nav-dot.saved.match {
      background: var(--theme-accent, #D85F36);
    }

    .saved-group {
      display: grid;
      gap: 0.1rem;
      margin-top: 0.2rem;
    }

    .saved-toggle {
      display: grid;
      grid-template-columns: minmax(0, 1fr) auto auto;
      align-items: center;
      gap: 0.4rem;
      width: calc(100% - 0.5rem);
      min-height: 32px;
      margin: 0.15rem 0.5rem 0.1rem 0;
      border: 1px solid transparent;
      border-radius: 0.45rem;
      padding: 0.35rem 0.5rem;
      color: var(--theme-text-muted, #8A8A8A);
      background: transparent;
      font-size: 0.7rem;
      font-weight: 800;
      text-align: left;
      text-transform: uppercase;
      cursor: pointer;
    }

    .saved-toggle:hover,
    .saved-toggle:focus-visible {
      color: var(--theme-accent, #7E3BA8);
      background: color-mix(in srgb, var(--theme-accent, #7E3BA8) 9%, transparent);
      outline: none;
    }

    .saved-count {
      display: inline-grid;
      min-width: 1.35rem;
      height: 1.2rem;
      place-items: center;
      border-radius: 999px;
      padding: 0 0.35rem;
      color: var(--theme-accent, #7E3BA8);
      background: color-mix(in srgb, var(--theme-accent, #7E3BA8) 12%, transparent);
      font-size: 0.65rem;
      font-weight: 900;
    }

    .saved-chevron {
      width: 14px;
      height: 14px;
      transition: transform 180ms ease;
    }

    .saved-chevron.open {
      transform: rotate(180deg);
    }

    .saved-list {
      display: grid;
      gap: 0.1rem;
      max-height: 210px;
      overflow-y: auto;
      overflow-x: hidden;
      padding-right: 0.25rem;
      -webkit-overflow-scrolling: touch;
    }

    .saved-title,
    .saved-empty,
    .saved-status {
      display: block;
      padding: 0.35rem 1rem 0.25rem 0.5rem;
      color: #8A8A8A;
      font-size: 0.7rem;
      font-weight: 700;
      text-transform: uppercase;
    }

    .saved-empty,
    .saved-status {
      text-transform: none;
      font-weight: 500;
    }

    .saved-status {
      color: var(--theme-danger);
    }

    .nav-icon {
      display: inline-flex;
      width: 20px;
      height: 20px;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      margin-right: 0.5rem;
      font-size: 1.25rem;
      line-height: 1;
      color: currentColor;
    }

    .nav-icon svg {
      display: block;
      width: 1em;
      height: 1em;
      flex: 0 0 auto;
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
      box-shadow: 14px 0 36px var(--theme-shadow);
    }

    .sidebar:not(.compact) {
      box-shadow: 18px 0 44px color-mix(in srgb, var(--theme-shadow) 78%, transparent);
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
    .saved-empty,
    .saved-status {
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
        width: min(86vw, 320px);
        max-width: 320px;
        height: 100dvh;
        left: -100%;
        top: 0;
        z-index: 2200;
        box-shadow: 18px 0 48px rgba(0, 0, 0, 0.28);

        &.open {
          left: 0;
        }
      }

      .sidebar-header {
        padding: 0.85rem 1rem 0.35rem;
      }

      .collapse-btn {
        display: none;
      }

      .sidebar-nav {
        overflow-y: auto;
        -webkit-overflow-scrolling: touch;
      }

      .close-btn {
        display: flex;
        margin-left: auto;
      }

      .sidebar-overlay {
        display: block;
        z-index: 2190;
      }
    }
  `],
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() compact: boolean = false;
  @Input() mobileView: boolean = false;
  @Output() onClose = new EventEmitter<void>();
  @Output() onToggleCompact = new EventEmitter<void>();

  savedKundalisOpen = false;
  savedMatchesOpen = false;
  navItems: NavItem[] = [];
  savedKundalis: any[] = [];
  savedMatches: SavedCompatibilityListItem[] = [];
  deletingKundaliIds = new Set<string>();
  deletingCompatibilityIds = new Set<string>();
  deleteStatusMessage = '';
  compatibilityDeleteStatusMessage = '';
  private savedKundaliRefreshSub?: Subscription;

  constructor(private kundaliService: KundaliService, private compatibilityApi: CompatibilityApiService, private router: Router, private authService: AuthService) {}

  ngOnInit(): void {
    this.loadSavedKundalis();
    this.loadSavedCompatibilities();
    this.savedKundaliRefreshSub = this.kundaliService.savedKundaliChanged$.subscribe(() => {
      this.loadSavedKundalis();
    });
  }

  ngOnDestroy(): void {
    this.savedKundaliRefreshSub?.unsubscribe();
  }

  @HostListener('window:kundali-saved')
  onKundaliSaved(): void {
    this.loadSavedKundalis();
  }

  @HostListener('window:compatibility-saved')
  onCompatibilitySaved(): void {
    this.loadSavedCompatibilities();
  }

  toggleSavedKundalis(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.savedKundalisOpen = !this.savedKundalisOpen;
  }

  toggleSavedMatches(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.savedMatchesOpen = !this.savedMatchesOpen;
  }

  deleteSavedKundali(item: any, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    const id = item?.id;
    if (!id || this.deletingKundaliIds.has(id)) {
      return;
    }

    const label = item?.displayName || item?.fullName || 'this Kundali';
    if (!window.confirm(`Delete saved Kundali "${label}"?`)) {
      return;
    }

    this.deleteStatusMessage = 'Deleting...';
    this.deletingKundaliIds.add(id);
    this.kundaliService.deleteSavedKundali(id).pipe(
      finalize(() => this.deletingKundaliIds.delete(id))
    ).subscribe({
      next: () => {
        this.deleteStatusMessage = '';
        this.savedKundalis = this.savedKundalis.filter(saved => saved.id !== id);
        this.loadSavedKundalis();

        if (this.router.url.includes(`/app/kundali/saved/${id}`)) {
          this.router.navigate(['/app/kundali']);
        }
      },
      error: (error) => {
        console.error('Saved kundali delete error:', error);
        this.deleteStatusMessage = error?.name === 'TimeoutError'
          ? 'Delete failed: backend did not respond.'
          : 'Delete failed. Please try again.';
        this.loadSavedKundalis();
      }
    });
  }
  private loadSavedKundalis(): void {
    this.kundaliService.getSavedKundalis().subscribe({
      next: (items) => {
        this.savedKundalis = Array.isArray(items) ? items : [];
        if (this.router.url.includes('/app/kundali/saved/')) {
          this.savedKundalisOpen = true;
        }
      },
      error: (error) => {
        console.error('Saved kundali menu load error:', error);
        this.savedKundalis = [];
      }
    });
  }

  deleteSavedCompatibility(item: SavedCompatibilityListItem, event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();

    const id = item?.id;
    if (!id || this.deletingCompatibilityIds.has(id)) {
      return;
    }

    const label = item.displayName || `${item.personAName} & ${item.personBName}`;
    if (!window.confirm(`Delete saved match "${label}"?`)) {
      return;
    }

    this.compatibilityDeleteStatusMessage = 'Deleting...';
    this.deletingCompatibilityIds.add(id);
    this.compatibilityApi.deleteCompatibilityAnalysis(id).pipe(
      finalize(() => this.deletingCompatibilityIds.delete(id))
    ).subscribe({
      next: () => {
        this.compatibilityDeleteStatusMessage = '';
        this.savedMatches = this.savedMatches.filter(saved => saved.id !== id);
        this.loadSavedCompatibilities();

        if (this.router.url.includes(`/app/compatibility/saved/${id}`)) {
          this.router.navigate(['/app/compatibility']);
        }
      },
      error: (error) => {
        console.error('Saved compatibility delete error:', error);
        this.compatibilityDeleteStatusMessage = error?.name === 'TimeoutError'
          ? 'Delete failed: backend did not respond.'
          : 'Delete failed. Please try again.';
        this.loadSavedCompatibilities();
      }
    });
  }

  private loadSavedCompatibilities(): void {
    this.compatibilityApi.getCompatibilityHistory().subscribe({
      next: (items) => {
        this.savedMatches = Array.isArray(items) ? items : [];
        if (this.router.url.includes('/app/compatibility/saved/')) {
          this.savedMatchesOpen = true;
        }
      },
      error: (error) => {
        console.error('Saved compatibility menu load error:', error);
        this.savedMatches = [];
      }
    });
  }

  toggleCompact(): void {
    this.onToggleCompact.emit();
  }

  logout(): void {
    this.authService.logout();
    this.onClose.emit();
    this.router.navigate(['/login']);
  }
}






















