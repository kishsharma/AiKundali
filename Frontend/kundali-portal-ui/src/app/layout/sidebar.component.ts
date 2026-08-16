// Left Sidebar Component
import { ChangeDetectorRef, Component, HostListener, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AstrologyIconComponent } from '../shared/components/astrology-icon.component';
import { KundaliService } from '../modules/kundali/kundali.service';
import { AuthService } from '../core/auth/auth.service';
import { PreferredLanguage } from '../core/auth/auth.models';
import { CompatibilityApiService, SavedCompatibilityListItem } from '../core/services/compatibility-api.service';
import { ThemeService } from '../core/services/theme.service';
import { NotificationService } from '../shared/services/notification.service';
import { Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { APP_VERSION } from '../config/app-version';

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
  imports: [CommonModule, RouterModule, AstrologyIconComponent, TranslocoPipe],
  template: `
    <aside class="sidebar" [class.open]="isOpen" [class.compact]="compact && !mobileView">
      <div class="sidebar-header">
        <button class="collapse-btn" type="button" (click)="toggleCompact()" [attr.aria-label]="compact ? ('sidebar.expandMenu' | transloco) : ('sidebar.collapseMenu' | transloco)" [title]="compact ? ('sidebar.expandMenu' | transloco) : ('sidebar.collapseMenu' | transloco)">
          <svg *ngIf="!compact" class="collapse-icon" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
            <path d="M11 17l-5-5 5-5"></path>
            <path d="M18 17l-5-5 5-5"></path>
          </svg>
          <svg *ngIf="compact" class="collapse-icon" width="18" height="18" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round">
            <path d="M13 17l5-5-5-5"></path>
            <path d="M6 17l5-5-5-5"></path>
          </svg>
        </button>

        <button class="close-btn" (click)="onClose.emit()" *ngIf="mobileView" [attr.aria-label]="'common.close' | transloco" [title]="'common.close' | transloco">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="18" y1="6" x2="6" y2="18"></line>
            <line x1="6" y1="6" x2="18" y2="18"></line>
          </svg>
        </button>
      </div>

      <nav class="sidebar-nav">
        <div class="nav-section">
          
          <h3 class="nav-section-title">{{ 'sidebar.main' | transloco }}</h3>
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
                <span class="nav-label">{{ 'sidebar.dashboard' | transloco }}</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/notes" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M5 4h14v16H5z"></path>
                    <path d="M8 8h8"></path>
                    <path d="M8 12h8"></path>
                    <path d="M8 16h5"></path>
                  </svg>
                </span>
                <span class="nav-label">{{ 'sidebar.notes' | transloco }}</span>
              </a>
            </li>

            <li class="nav-item nav-parent">
              <div class="nav-parent-row" data-dhruva-tour="create-kundali" data-tour-id="portal-create-kundali">
                <a class="nav-parent-link" routerLink="/app/kundali" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                  <app-astrology-icon name="kundali" class="nav-icon icon-md"></app-astrology-icon>
                  <span class="nav-label">{{ 'sidebar.kundali' | transloco }}</span>
                </a>
                <a class="nav-create-btn" routerLink="/app/kundali" aria-label="Create Kundali" title="Create Kundali" (click)="onClose.emit()">+</a>
              </div>
              <ul class="nav-sublist" *ngIf="!compact || mobileView">
                <li class="saved-group">
                  <button
                    type="button"
                    class="saved-toggle"
                    (click)="toggleSavedKundalis($event)"
                    [attr.aria-expanded]="savedKundalisOpen"
                    aria-controls="saved-kundali-list" data-dhruva-tour="saved-kundalis" data-tour-id="portal-saved-kundalis">
                    <span>{{ 'sidebar.savedKundali' | transloco }}</span>
                    <span class="saved-count">{{ savedKundalis.length }}</span>
                    <svg class="saved-chevron" [class.open]="savedKundalisOpen" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="m6 9 6 6 6-6"></path>
                    </svg>
                  </button>
                  <div id="saved-kundali-list" class="saved-list" *ngIf="savedKundalisOpen">
                    <div class="saved-row" *ngFor="let item of savedKundalis" [class.deleting]="deletingKundaliIds.has(item.id)" [attr.aria-busy]="deletingKundaliIds.has(item.id)">
                      <a [routerLink]="['/app/kundali/saved', item.id]" routerLinkActive="active" (click)="onClose.emit()" [title]="item.displayName">
                        <span class="nav-dot saved"></span>
                        <span class="nav-label">{{ deletingKundaliIds.has(item.id) ? ('sidebar.deleting' | transloco) : item.displayName }}</span>
                      </a>
                      <button
                        type="button"
                        class="saved-delete-btn"
                        (click)="deleteSavedKundali(item, $event)"
                        [disabled]="deletingKundaliIds.has(item.id)"
                        [attr.aria-label]="'sidebar.deleteSavedKundaliNamed' | transloco: { name: item.displayName }"
                        [title]="deletingKundaliIds.has(item.id) ? ('sidebar.deleting' | transloco) : ('sidebar.deleteSavedKundali' | transloco)">
                        {{ deletingKundaliIds.has(item.id) ? ('sidebar.deleting' | transloco) : 'X' }}
                      </button>
                    </div>
                    <span class="saved-status" *ngIf="deleteStatusMessage">{{ deleteStatusMessage }}</span>
                    <span class="saved-empty" *ngIf="!savedKundalis.length">{{ 'sidebar.noSavedKundali' | transloco }}</span>
                  </div>
                </li>
              </ul>
            </li>

            <li class="nav-item nav-parent">
              <div class="nav-parent-row" data-dhruva-tour="compatibility" data-tour-id="portal-compatibility">
                <a class="nav-parent-link" routerLink="/app/compatibility" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" (click)="onClose.emit()">
                  <app-astrology-icon name="compatibility" class="nav-icon icon-md"></app-astrology-icon>
                  <span class="nav-label">{{ 'sidebar.compatibility' | transloco }}</span>
                </a>
                <a class="nav-create-btn" routerLink="/app/compatibility" aria-label="New Match" title="New Match" (click)="onClose.emit()">+</a>
              </div>
              <ul class="nav-sublist" *ngIf="!compact || mobileView">
                <li class="saved-group">
                  <button
                    type="button"
                    class="saved-toggle"
                    (click)="toggleSavedMatches($event)"
                    [attr.aria-expanded]="savedMatchesOpen"
                    aria-controls="saved-match-list">
                    <span>{{ 'sidebar.savedMatches' | transloco }}</span>
                    <span class="saved-count">{{ savedMatches.length }}</span>
                    <svg class="saved-chevron" [class.open]="savedMatchesOpen" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
                      <path d="m6 9 6 6 6-6"></path>
                    </svg>
                  </button>
                  <div id="saved-match-list" class="saved-list" *ngIf="savedMatchesOpen">
                    <div class="saved-row" *ngFor="let item of savedMatches" [class.deleting]="deletingCompatibilityIds.has(item.id)" [attr.aria-busy]="deletingCompatibilityIds.has(item.id)">
                      <a [routerLink]="['/app/compatibility/saved', item.id]" routerLinkActive="active" (click)="onClose.emit()" [title]="item.displayName || (item.personAName + ' & ' + item.personBName)">
                        <span class="nav-dot saved match"></span>
                        <span class="nav-label">{{ deletingCompatibilityIds.has(item.id) ? ('sidebar.deleting' | transloco) : (item.displayName || (item.personAName + ' & ' + item.personBName)) }}</span>
                      </a>
                      <button
                        type="button"
                        class="saved-delete-btn"
                        (click)="deleteSavedCompatibility(item, $event)"
                        [disabled]="deletingCompatibilityIds.has(item.id)"
                        [attr.aria-label]="'sidebar.deleteSavedMatchNamed' | transloco: { name: (item.displayName || item.personAName + ' and ' + item.personBName) }"
                        [title]="deletingCompatibilityIds.has(item.id) ? ('sidebar.deleting' | transloco) : ('sidebar.deleteSavedMatch' | transloco)">
                        {{ deletingCompatibilityIds.has(item.id) ? ('sidebar.deleting' | transloco) : 'X' }}
                      </button>
                    </div>
                    <span class="saved-status" *ngIf="compatibilityDeleteStatusMessage">{{ compatibilityDeleteStatusMessage }}</span>
                    <span class="saved-empty" *ngIf="!savedMatches.length">{{ 'sidebar.noSavedMatches' | transloco }}</span>
                  </div>
                </li>
              </ul>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">{{ 'sidebar.astrology' | transloco }}</h3>
          <ul class="nav-list">


            <li class="nav-item">
              <a routerLink="/app/panchang" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }" data-dhruva-tour="panchang" data-tour-id="portal-panchang" (click)="onClose.emit()">
                <app-astrology-icon name="panchang" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">{{ 'sidebar.panchang' | transloco }}</span>
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
                <span class="nav-label">{{ 'sidebar.muhurtha' | transloco }}</span>
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
                <span class="nav-label">{{ 'sidebar.jyotishStudy' | transloco }}</span>
              </a>
            </li>
            <li class="nav-item">
              <a routerLink="/app/dasha" routerLinkActive="active" (click)="onClose.emit()">
                <app-astrology-icon name="dasha" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">{{ 'sidebar.dasha' | transloco }}</span>
              </a>
            </li>
          </ul>
        </div>

        <div class="nav-section">
          <h3 class="nav-section-title">{{ 'sidebar.settings' | transloco }}</h3>
          <ul class="nav-list">

            <li class="nav-item">
              <a routerLink="/app/user-login-control" routerLinkActive="active" (click)="onClose.emit()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2"></path>
                    <circle cx="9.5" cy="7" r="4"></circle>
                    <path d="M17 11l2 2 4-4"></path>
                  </svg>
                </span>
                <span class="nav-label">{{ 'sidebar.userLogin' | transloco }}</span>
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
                <span class="nav-label">{{ 'sidebar.auditLogs' | transloco }}</span>
              </a>
            </li>
            <li class="nav-item about-popover-host">
              <button
                type="button"
                class="nav-popover-trigger"
                [class.active]="aboutPopoverOpen"
                (click)="toggleAboutPopover($event)"
                [attr.aria-expanded]="aboutPopoverOpen"
                aria-controls="about-version-popover"
              >
                <app-astrology-icon name="info" class="nav-icon icon-md"></app-astrology-icon>
                <span class="nav-label">{{ 'sidebar.about' | transloco }}</span>
              </button>
              <div
                id="about-version-popover"
                class="about-popover"
                *ngIf="aboutPopoverOpen"
                role="dialog"
                [attr.aria-label]="'sidebar.aboutHoraLife' | transloco"
                (click)="$event.stopPropagation()"
              >
                <div class="about-popover-header">
                  <strong>{{ appVersion.productName }}</strong>
                </div>
          <p class="about-popover-section-title">{{ 'sidebar.aboutSection' | transloco }}</p>
          <dl class="about-popover-details">
            <div>
              <dt>{{ 'sidebar.version' | transloco }}</dt>
              <dd>{{ appVersion.version }}</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.release' | transloco }}</dt>
              <dd>{{ appVersion.releaseName }}</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.status' | transloco }}</dt>
              <dd>{{ appVersion.status }}</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.started' | transloco }}</dt>
              <dd>{{ appVersion.formalVersioningStart }}</dd>
            </div>
          </dl>

          <p class="about-popover-section-title about-popover-section-title--separated">{{ 'sidebar.systemDetails' | transloco }}</p>
          <dl class="about-popover-details about-popover-details--system">
            <div>
              <dt>{{ 'sidebar.system' | transloco }}</dt>
              <dd>Classical Parāśari Jyotiṣa</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.zodiac' | transloco }}</dt>
              <dd>Sidereal</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.ayanamsha' | transloco }}</dt>
              <dd>Lahiri / Chitrapaksha</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.houses' | transloco }}</dt>
              <dd>Whole Sign</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.dashaSystem' | transloco }}</dt>
              <dd>Vimshottari</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.references' | transloco }}</dt>
              <dd>BPHS, Phaladeepika</dd>
            </div>
            <div>
              <dt>{{ 'sidebar.calculations' | transloco }}</dt>
              <dd>Swiss Ephemeris</dd>
            </div>
          </dl>
              </div>
            </li>

          </ul>
        </div>
      </nav>

      <div class="mobile-account-panel" *ngIf="mobileView">
        <div class="mobile-account-card">
          <button
            class="mobile-account-toggle"
            type="button"
            (click)="toggleMobileAccountMenu()"
            [attr.aria-expanded]="mobileAccountMenuOpen"
          >
            <div class="mobile-account-header">
              <div class="user-avatar" aria-hidden="true">{{ userInitial }}</div>
              <div class="mobile-account-meta">
                <strong>{{ userName }}</strong>
                <small>{{ userEmail }}</small>
              </div>
            </div>
            <span class="mobile-account-chevron" [class.open]="mobileAccountMenuOpen"></span>
          </button>

          <div class="mobile-account-panel-body" [class.open]="mobileAccountMenuOpen">
            <div class="mobile-preferences">
              <div class="mobile-preference-group">
                <span class="mobile-preference-title">{{ 'sidebar.language' | transloco }}</span>
                <div class="mobile-option-list mobile-option-list--language">
                  <button
                    type="button"
                    *ngFor="let language of languages"
                    [class.active]="activeLanguage === language.code"
                    (click)="changeLanguage(language.code)"
                  >{{ language.label }}</button>
                </div>
              </div>

              <div class="mobile-preference-group">
                <span class="mobile-preference-title">{{ 'sidebar.theme' | transloco }}</span>
                <div class="mobile-option-list mobile-option-list--theme">
                  <button
                    type="button"
                    (click)="toggleTheme()"
                    [title]="themeToggleTitle"
                    [attr.aria-label]="themeToggleTitle"
                  >
                    <span>{{ themeToggleIcon }}</span>
                    <small>{{ themeAvailableModes }}</small>
                  </button>
                </div>
              </div>
            </div>

            <div class="mobile-account-actions">
              <button class="logout-btn logout-btn--menu" type="button" (click)="logout()">
                <span class="nav-icon" aria-hidden="true">
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M9 21H6.5A2.5 2.5 0 0 1 4 18.5v-13A2.5 2.5 0 0 1 6.5 3H9"></path>
                    <path d="M15 17l5-5-5-5"></path>
                    <path d="M20 12H9"></path>
                  </svg>
                </span>
                <span class="nav-label">{{ 'sidebar.logout' | transloco }}</span>
              </button>
            </div>
          </div>
        </div>
      </div>
      <div class="sidebar-footer" *ngIf="!mobileView">
        <button class="logout-btn" type="button" (click)="logout()">
          <span class="nav-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.1" stroke-linecap="round" stroke-linejoin="round">
              <path d="M9 21H6.5A2.5 2.5 0 0 1 4 18.5v-13A2.5 2.5 0 0 1 6.5 3H9"></path>
              <path d="M15 17l5-5-5-5"></path>
              <path d="M20 12H9"></path>
            </svg>
          </span>
          <span class="nav-label">{{ 'sidebar.logout' | transloco }}</span>
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
      overflow: visible;
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
      a,
      .nav-popover-trigger {
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
    .nav-parent-row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 28px;
      align-items: center;
      gap: 0.2rem;
      padding-right: 0.45rem;
    }

    .nav-parent-link {
      min-width: 0;
    }

    .nav-item .nav-create-btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      min-height: 28px;
      justify-self: center;
      margin-right: 0;
      padding: 0;
      border: 0;
      border-radius: 0.45rem;
      background: transparent;
      color: var(--theme-text-muted);
      font-size: 1.18rem;
      font-weight: 500;
      line-height: 1;
      text-decoration: none;
      transition: opacity 160ms ease, background-color 160ms ease, color 160ms ease;
    }

    .nav-item .nav-create-btn:hover,
    .nav-item .nav-create-btn:focus-visible {
      background: color-mix(in srgb, var(--theme-accent, #7E3BA8) 9%, transparent);
      color: var(--theme-accent, #7E3BA8);
      outline: none;
    }


    .sidebar.compact .nav-parent-row {
      grid-template-columns: 1fr;
      justify-items: center;
    }

    .sidebar.compact .nav-item .nav-create-btn {
      display: none;
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

    .sidebar.compact .nav-item a,
    .sidebar.compact .nav-item .nav-popover-trigger {
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

    .nav-popover-trigger {
      width: 100%;
      border: 0;
      background: transparent;
      font: inherit;
      text-align: left;
      cursor: pointer;
    }

    .about-popover-host {
      position: relative;
    }

    .about-popover-host .nav-popover-trigger::after {
      content: '›';
      margin-left: auto;
      color: var(--theme-text-muted);
      font-size: 0.9rem;
      font-weight: 700;
      line-height: 1;
      transition: transform 160ms ease, color 160ms ease;
    }

    .about-popover-host .nav-popover-trigger.active::after {
      color: var(--theme-primary);
      transform: translateX(2px);
    }


    .about-popover {
      position: absolute;
      left: calc(100% + 0.5rem);
      top: auto;
      bottom: 0;
      z-index: 420;
      width: min(258px, calc(100vw - 245px));
      max-height: min(52vh, 300px);
      overflow-y: auto;
      overflow-x: hidden;
      padding: 0.5rem 0.55rem;
      border: 1px solid color-mix(in srgb, var(--theme-line) 80%, transparent);
      border-radius: 8px;
      background: color-mix(in srgb, var(--theme-surface) 96%, white);
      box-shadow: 0 10px 28px color-mix(in srgb, var(--theme-shadow) 12%, transparent);
      color: var(--theme-text);
      transform: none;
      animation: aboutPopoverIn 160ms ease-out;
      scrollbar-width: thin;
      scrollbar-color: color-mix(in srgb, var(--theme-line) 72%, var(--theme-muted)) transparent;
    }

    .about-popover::before {
      content: '';
      position: absolute;
      left: -5px;
      top: auto;
      bottom: 1rem;
      width: 8px;
      height: 8px;
      border-left: 1px solid color-mix(in srgb, var(--theme-line) 80%, transparent);
      border-bottom: 1px solid color-mix(in srgb, var(--theme-line) 80%, transparent);
      background: color-mix(in srgb, var(--theme-surface) 96%, white);
      transform: rotate(45deg);
    }

    .about-popover::-webkit-scrollbar {
      width: 4px;
    }

    .about-popover::-webkit-scrollbar-track {
      background: transparent;
    }

    .about-popover::-webkit-scrollbar-thumb {
      background: color-mix(in srgb, var(--theme-line) 72%, var(--theme-muted));
      border-radius: 999px;
    }

    .sidebar.compact .about-popover {
      left: calc(100% + 0.62rem);
      width: min(258px, calc(100vw - 105px));
    }

    .about-popover-header {
      display: grid;
      gap: 0.08rem;
      margin-bottom: 0.38rem;
      padding-bottom: 0.34rem;
      border-bottom: 1px solid color-mix(in srgb, var(--theme-line) 72%, transparent);
    }

    .about-popover-header strong {
      font-size: 0.74rem;
      font-weight: 800;
      line-height: 1.2;
    }

    .about-popover-section-title {
      margin: 0 0 0.22rem;
      color: var(--theme-text-muted);
      font-size: 0.5rem;
      font-weight: 800;
      text-transform: uppercase;
      letter-spacing: 0;
    }

    .about-popover-section-title--separated {
      margin-top: 0.34rem;
      padding-top: 0.32rem;
      border-top: 1px solid color-mix(in srgb, var(--theme-line) 72%, transparent);
    }

    .about-popover-details {
      display: grid;
      gap: 0.12rem;
      margin: 0;
    }

    .about-popover-details div {
      display: grid;
      grid-template-columns: minmax(58px, 0.38fr) minmax(0, 1fr);
      align-items: start;
      gap: 0.42rem;
      padding: 0.1rem 0;
    }

    .about-popover-details dt {
      color: var(--theme-text-muted);
      font-size: 0.49rem;
      font-weight: 750;
      line-height: 1.18;
      text-transform: uppercase;
      letter-spacing: 0;
    }

    .about-popover-details dd {
      margin: 0;
      color: var(--theme-text);
      font-size: 0.63rem;
      font-weight: 650;
      line-height: 1.18;
      overflow-wrap: anywhere;
    }

    .about-popover-details--system div {
      padding-block: 0.08rem;
    }

    .about-popover-details--system dd {
      font-weight: 650;
    }

    @keyframes aboutPopoverIn {
      from {
        opacity: 0;
        transform: translateX(-6px);
      }
      to {
        opacity: 1;
        transform: translateX(0);
      }
    }

    @media (max-width: 768px) {
      .about-popover {
        position: absolute;
        left: 0;
        right: auto;
        top: calc(100% + 0.4rem);
        bottom: auto;
        width: min(292px, calc(100vw - 2rem));
        max-height: min(52vh, 300px);
        transform: none;
      }

      .about-popover::before {
        left: 1.1rem;
        top: -6px;
        transform: rotate(135deg);
      }
    }

    .mobile-account-panel {
      padding: 0 1rem 0.75rem;
    }

    .mobile-account-card {
      border: 1px solid color-mix(in srgb, var(--theme-line) 78%, transparent);
      border-radius: 0.7rem;
      background: color-mix(in srgb, var(--theme-surface-2) 76%, transparent);
      padding: 0.7rem 0.8rem;
      box-shadow: none;
      overflow: hidden;
    }

    .mobile-account-toggle {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      border: 0;
      background: transparent;
      padding: 0;
      cursor: pointer;
      color: inherit;
      font: inherit;
      text-align: left;
    }

    .mobile-account-header {
      display: flex;
      align-items: center;
      gap: 0.7rem;
    }

    .mobile-account-meta {
      display: flex;
      flex-direction: column;
      min-width: 0;
      gap: 0.1rem;
    }

    .mobile-account-meta strong {
      color: var(--theme-text);
      font-size: 0.875rem;
      font-weight: 600;
      line-height: 1.2;
    }

    .mobile-account-meta small {
      color: var(--theme-text-muted);
      font-size: 0.75rem;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
      line-height: 1.2;
    }

    .mobile-account-chevron {
      position: relative;
      width: 16px;
      height: 16px;
      flex: 0 0 16px;
      color: var(--theme-text-muted);
      transition: transform 180ms ease;
    }

    .mobile-account-chevron::before {
      content: "";
      position: absolute;
      left: 50%;
      top: 45%;
      width: 6px;
      height: 6px;
      border-right: 1.5px solid currentColor;
      border-bottom: 1.5px solid currentColor;
      transform: translate(-50%, -50%) rotate(45deg);
    }

    .mobile-account-chevron.open {
      transform: rotate(180deg);
    }

    .mobile-account-panel-body {
      max-height: 0;
      opacity: 0;
      overflow: hidden;
      transition: max-height 240ms ease, opacity 220ms ease, margin-top 240ms ease;
    }

    .mobile-account-panel-body.open {
      max-height: 220px;
      opacity: 1;
      margin-top: 0.8rem;
    }

    .mobile-preferences {
      display: grid;
      gap: 0.7rem;
    }

    .mobile-account-actions {
      margin-top: 0.85rem;
      padding-top: 0.75rem;
      border-top: 1px solid color-mix(in srgb, var(--theme-line) 78%, transparent);
    }

    .logout-btn--menu {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: flex-start;
      gap: 0.65rem;
      padding: 0.72rem 0.8rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.6rem;
      background: color-mix(in srgb, var(--theme-surface-2) 78%, transparent);
      color: var(--theme-text-muted);
      font-size: 0.8rem;
      font-weight: 600;
    }

    .logout-btn--menu:hover {
      background: color-mix(in srgb, var(--theme-danger) 12%, transparent);
      border-color: color-mix(in srgb, var(--theme-danger) 30%, var(--theme-line));
      color: var(--theme-danger);
    }

    .mobile-preference-title {
      display: block;
      margin-bottom: 0.35rem;
      color: var(--theme-text-muted);
      font-size: 0.72rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.04em;
    }

    .mobile-option-list {
      display: grid;
      gap: 0.45rem;
    }

    .mobile-option-list button {
      display: flex;
      justify-content: center;
      align-items: center;
      gap: 0.35rem;
      min-height: 2.1rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.55rem;
      background: var(--theme-surface);
      color: var(--theme-text-muted);
      font-size: 0.78rem;
      font-weight: 600;
      padding: 0.4rem 0.6rem;
      cursor: pointer;
    }

    .mobile-option-list button.active {
      border-color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 12%, transparent);
      color: var(--theme-accent);
    }

    .mobile-option-list--language {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }

    .mobile-option-list--theme {
      grid-template-columns: repeat(2, minmax(0, 1fr));
    }
    .sidebar-footer {
      display: grid;
      gap: 0.65rem;
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
    .nav-popover-trigger,
    .logout-btn {
      color: var(--theme-text-muted);
    }

    .collapse-btn:hover,
    .close-btn:hover,
    .nav-item a:hover,
    .nav-popover-trigger:hover {
      color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 10%, transparent);
    }

    .nav-item a.active,
    .nav-popover-trigger.active {
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
  private _isOpen = false;
  private _mobileView = false;

  @Input()
  get isOpen(): boolean {
    return this._isOpen;
  }
  set isOpen(value: boolean) {
    this._isOpen = value;
    if (value && this.mobileView) {
      this.mobileAccountMenuOpen = false;
    }
  }

  @Input()
  get mobileView(): boolean {
    return this._mobileView;
  }
  set mobileView(value: boolean) {
    this._mobileView = value;
    if (!value) {
      this.mobileAccountMenuOpen = false;
    }
  }

  @Input() compact: boolean = false;
  @Output() onClose = new EventEmitter<void>();
  @Output() onToggleCompact = new EventEmitter<void>();
  readonly appVersion = APP_VERSION;
  readonly languages: ReadonlyArray<{ code: PreferredLanguage; label: string }> = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'हिंदी' },
    { code: 'kn', label: 'ಕನ್ನಡ' }
  ];
  readonly themes = [
    { mode: 'dark', label: 'Dark', icon: '☾' },
    { mode: 'light', label: 'Light', icon: '☼' }
  ] as const;

  savedKundalisOpen = false;
  savedMatchesOpen = false;
  navItems: NavItem[] = [];
  savedKundalis: any[] = [];
  savedMatches: SavedCompatibilityListItem[] = [];
  deletingKundaliIds = new Set<string>();
  deletingCompatibilityIds = new Set<string>();
  deleteStatusMessage = '';
  compatibilityDeleteStatusMessage = '';
  aboutPopoverOpen = false;
  mobileAccountMenuOpen = false;
  private savedKundaliRefreshSub?: Subscription;

  constructor(
    private kundaliService: KundaliService,
    private compatibilityApi: CompatibilityApiService,
    private router: Router,
    private authService: AuthService,
    private translocoService: TranslocoService,
    private notifications: NotificationService,
    private cdr: ChangeDetectorRef,
    public themeService: ThemeService
  ) {}

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

  get currentUser() {
    return this.authService.getCurrentUser();
  }

  get userName(): string {
    return this.formatDisplayName(this.currentUser?.fullName || this.t('sidebar.userFallback'));
  }

  get userEmail(): string {
    return this.currentUser?.email || '';
  }

  get userInitial(): string {
    return this.userName.charAt(0).toUpperCase();
  }

  get activeLanguage(): string {
    return this.translocoService.getActiveLang();
  }

  changeLanguage(lang: PreferredLanguage): void {
    if (this.activeLanguage === lang) {
      return;
    }

    const previousLanguage = this.activeLanguage as PreferredLanguage;
    this.translocoService.setActiveLang(lang);

    if (!this.authService.isLoggedIn()) {
      return;
    }

    this.authService.updatePreferredLanguage({ preferredLanguage: lang }).subscribe({
      error: (error) => {
        this.translocoService.setActiveLang(previousLanguage);
        this.notifications.error(error?.error?.message || 'Unable to update preferred language.');
      }
    });
  }

  get themeToggleIcon(): string {
    return this.themeService.mode() === 'dark' ? '☀' : '☾';
  }

  get themeToggleLabel(): string {
    return this.themeService.mode() === 'dark' ? this.t('sidebar.dark') : this.t('sidebar.light');
  }

  get themeAvailableModes(): string {
    return this.t('sidebar.darkLight');
  }

  get themeToggleTitle(): string {
    return this.themeService.mode() === 'dark' ? this.t('sidebar.switchToLight') : this.t('sidebar.switchToDark');
  }

  private t(key: string, params?: Record<string, unknown>): string {
    return this.translocoService.translate(key, params);
  }

  toggleTheme(): void {
    this.themeService.setTheme(this.themeService.mode() === 'dark' ? 'light' : 'dark');
  }

  toggleMobileAccountMenu(): void {
    this.mobileAccountMenuOpen = !this.mobileAccountMenuOpen;
  }

  toggleAboutPopover(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.aboutPopoverOpen = !this.aboutPopoverOpen;
  }

  closeAboutPopover(): void {
    this.aboutPopoverOpen = false;
  }

  private formatDisplayName(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .filter(Boolean)
      .map((part) => part.charAt(0).toUpperCase() + part.slice(1).toLowerCase())
      .join(' ') || this.t('sidebar.userFallback');
  }

  @HostListener('document:click')
  closeAboutPopoverFromDocument(): void {
    this.closeAboutPopover();
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
    if (!window.confirm(this.t('sidebar.confirmDeleteKundali', { name: label }))) {
      return;
    }

    this.deleteStatusMessage = this.t('sidebar.deleting');
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
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Saved kundali menu load error:', error);
        this.savedKundalis = [];
        this.cdr.markForCheck();
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
    if (!window.confirm(this.t('sidebar.confirmDeleteMatch', { name: label }))) {
      return;
    }

    this.compatibilityDeleteStatusMessage = this.t('sidebar.deleting');
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
        this.cdr.markForCheck();
      },
      error: (error) => {
        console.error('Saved compatibility menu load error:', error);
        this.savedMatches = [];
        this.cdr.markForCheck();
      }
    });
  }

  toggleCompact(): void {
    this.onToggleCompact.emit();
  }

  logout(): void {
    if (!window.confirm(this.t('sidebar.confirmLogout'))) {
      return;
    }

    this.authService.logout();
    this.onClose.emit();
    this.router.navigate(['/login']);
  }
}








