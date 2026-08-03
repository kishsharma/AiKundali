// Dashboard Page Component - meaningful authenticated landing page
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Subscription, of } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';

import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';
import {
  DashboardApiService,
  DashboardCurrentDasha,
  DashboardInsight,
  DashboardKundali,
  DashboardLearningLevel,
  DashboardMuhurthaPreview,
  DashboardPanchang,
  DashboardSummary,
} from '../../core/services/dashboard-api.service';
import { LatestKundaliStorageService } from '../../core/services/latest-kundali-storage.service';

interface QuickAction {
  label: string;
  caption: string;
  routerLink: string;
  icon: string;
}
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, PageHeaderComponent, SectionCardComponent],
  templateUrl: './dashboard.component.html',
  styles: [`
    :host {
      display: block;
      color: var(--theme-text);
    }

    .dashboard-shell {
      display: grid;
      gap: 1rem;
    }

    .dashboard-hero-grid {
      display: grid;
      grid-template-columns: minmax(0, 1.18fr) minmax(320px, 0.82fr);
      gap: 1rem;
      align-items: stretch;
    }

    .panel {
      border: 1px solid var(--theme-line);
      border-radius: var(--theme-radius-card);
      background: color-mix(in srgb, var(--theme-surface) 92%, transparent);
      box-shadow: 0 18px 44px var(--theme-shadow);
    }

    .daily-panel {
      position: relative;
      overflow: hidden;
      padding: 1.25rem;
      min-height: 294px;
    }

    .daily-panel::after {
      content: '';
      position: absolute;
      right: 1.25rem;
      top: 1.25rem;
      width: 8.5rem;
      height: 8.5rem;
      border: 1px solid color-mix(in srgb, var(--theme-accent) 30%, transparent);
      border-radius: 50%;
      opacity: 0.22;
      pointer-events: none;
    }

    .hero-copy {
      position: relative;
      z-index: 1;
      max-width: 42rem;
    }

    .kicker {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      margin-bottom: 0.55rem;
      color: var(--theme-accent);
      font-size: 0.72rem;
      font-weight: 800;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    .hero-title {
      margin: 0;
      color: var(--theme-text);
      font-size: 1.65rem;
      line-height: 1.15;
      font-weight: 700;
      letter-spacing: 0;
    }

    .hero-subtitle {
      margin: 0.55rem 0 0;
      color: var(--theme-text-muted);
      font-size: 0.95rem;
      line-height: 1.5;
    }

    .hero-meta {
      display: flex;
      flex-wrap: wrap;
      gap: 0.55rem;
      margin: 1rem 0 0;
      color: var(--theme-text-muted);
      font-size: 0.82rem;
    }

    .meta-chip {
      display: inline-flex;
      align-items: center;
      min-height: 2rem;
      padding: 0.35rem 0.65rem;
      border: 1px solid var(--theme-line-soft);
      border-radius: var(--theme-radius-control);
      background: color-mix(in srgb, var(--theme-input-bg) 58%, transparent);
    }

    .panchang-strip {
      position: relative;
      z-index: 1;
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 0.65rem;
      margin-top: 1.2rem;
    }

    .panchang-item {
      min-height: 5.4rem;
      padding: 0.75rem;
      border: 1px solid var(--theme-line-soft);
      border-radius: var(--theme-radius-control);
      background: color-mix(in srgb, var(--theme-surface-2) 74%, transparent);
    }

    .panchang-item span,
    .mini-label,
    .card-note,
    .recent-meta,
    .dasha-date,
    .progress-copy,
    .empty-copy {
      color: var(--theme-text-muted);
    }

    .panchang-item span,
    .mini-label {
      display: block;
      margin-bottom: 0.35rem;
      font-size: 0.7rem;
      font-weight: 800;
      letter-spacing: 0.06em;
      text-transform: uppercase;
    }

    .panchang-item strong {
      display: block;
      color: var(--theme-text);
      font-size: 0.95rem;
      line-height: 1.25;
      overflow-wrap: anywhere;
    }

    .quick-panel {
      padding: 1.25rem;
    }

    .section-heading {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .section-heading h2 {
      margin: 0;
      color: var(--theme-text);
      font-size: 0.95rem;
      font-weight: 800;
      letter-spacing: 0.06em;
      text-transform: uppercase;
    }

    .section-link,
    .text-link {
      color: var(--theme-accent);
      font-size: 0.82rem;
      font-weight: 800;
      text-decoration: none;
      white-space: nowrap;
    }

    .section-link:hover,
    .text-link:hover {
      text-decoration: underline;
    }

    .actions-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.75rem;
    }

    .action-card {
      min-height: 7rem;
      display: grid;
      align-content: center;
      justify-items: center;
      gap: 0.45rem;
      padding: 1rem;
      border: 1px solid var(--theme-line);
      border-radius: var(--theme-radius-control);
      background: color-mix(in srgb, var(--theme-input-bg) 52%, transparent);
      color: var(--theme-text);
      text-align: center;
      text-decoration: none;
      transition: border-color 180ms ease, background 180ms ease, transform 180ms ease;
    }

    .action-card:hover,
    .action-card:focus-visible {
      border-color: var(--theme-accent);
      background: color-mix(in srgb, var(--theme-accent) 10%, var(--theme-surface));
      transform: translateY(-1px);
      outline: none;
    }

    .action-mark {
      display: grid;
      width: 2.65rem;
      height: 2.65rem;
      place-items: center;
      border: 1px solid color-mix(in srgb, var(--theme-accent) 46%, transparent);
      border-radius: 50%;
      color: var(--theme-accent);
      font-size: 0.86rem;
      font-weight: 900;
    }

    .action-label {
      color: var(--theme-text);
      font-size: 0.9rem;
      font-weight: 800;
      line-height: 1.15;
    }

    .action-caption {
      color: var(--theme-text-muted);
      font-size: 0.76rem;
      line-height: 1.25;
    }

    .journey-grid {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 0.85rem;
    }

    .journey-card,
    .collection-card,
    .insight-card,
    .muhurtha-row {
      border: 1px solid var(--theme-line);
      border-radius: var(--theme-radius-control);
      background: color-mix(in srgb, var(--theme-surface-2) 72%, transparent);
    }

    .journey-card {
      display: grid;
      gap: 0.65rem;
      min-height: 10.5rem;
      padding: 1rem;
    }

    .journey-card h3,
    .insight-card h3 {
      margin: 0;
      color: var(--theme-text);
      font-size: 0.98rem;
      line-height: 1.25;
    }

    .card-note,
    .recent-meta,
    .dasha-date,
    .empty-copy {
      margin: 0;
      font-size: 0.82rem;
      line-height: 1.45;
    }

    .recent-name {
      margin: 0;
      color: var(--theme-text);
      font-size: 1.05rem;
      font-weight: 800;
      overflow-wrap: anywhere;
    }

    .status-pill {
      width: fit-content;
      padding: 0.28rem 0.55rem;
      border: 1px solid color-mix(in srgb, var(--theme-success) 42%, transparent);
      border-radius: 999px;
      color: var(--theme-success);
      background: color-mix(in srgb, var(--theme-success) 10%, transparent);
      font-size: 0.72rem;
      font-weight: 800;
    }

    .status-pill.pending {
      border-color: color-mix(in srgb, var(--theme-warning) 42%, transparent);
      color: var(--theme-warning);
      background: color-mix(in srgb, var(--theme-warning) 10%, transparent);
    }

    .collection-grid {
      display: grid;
      grid-template-columns: repeat(5, minmax(0, 1fr));
      gap: 0.75rem;
    }

    .collection-card {
      display: grid;
      gap: 0.35rem;
      min-height: 7rem;
      padding: 0.9rem;
      text-decoration: none;
      color: inherit;
    }

    .collection-card:hover {
      border-color: color-mix(in srgb, var(--theme-accent) 56%, transparent);
    }

    .collection-number {
      color: var(--theme-text);
      font-size: 1.75rem;
      font-weight: 800;
      line-height: 1;
    }

    .collection-label {
      color: var(--theme-text-muted);
      font-size: 0.78rem;
      font-weight: 700;
    }

    .today-grid {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.85rem;
    }

    .insight-card {
      display: grid;
      gap: 0.55rem;
      min-height: 11.5rem;
      padding: 1rem;
      overflow: hidden;
      position: relative;
    }

    .insight-card::after {
      content: '';
      position: absolute;
      right: -2.5rem;
      bottom: -2.5rem;
      width: 7rem;
      height: 7rem;
      border-radius: 50%;
      background: color-mix(in srgb, var(--tone-color, var(--theme-accent)) 14%, transparent);
      pointer-events: none;
    }

    .insight-card.moon { --tone-color: var(--theme-info); }
    .insight-card.study { --tone-color: var(--theme-accent); }
    .insight-card.muhurtha { --tone-color: var(--theme-warning); }

    .insight-eyebrow {
      color: var(--tone-color, var(--theme-accent));
      font-size: 0.7rem;
      font-weight: 900;
      letter-spacing: 0.06em;
      text-transform: uppercase;
    }

    .insight-card p {
      position: relative;
      z-index: 1;
      margin: 0;
      color: var(--theme-text-muted);
      font-size: 0.84rem;
      line-height: 1.45;
    }

    .progress-layout {
      display: grid;
      grid-template-columns: 8.5rem minmax(0, 1fr);
      gap: 1rem;
      align-items: center;
    }

    .progress-ring {
      display: grid;
      width: 7.2rem;
      height: 7.2rem;
      place-items: center;
      border-radius: 50%;
      background: conic-gradient(var(--theme-accent) var(--progress, 0%), color-mix(in srgb, var(--theme-line) 58%, transparent) 0);
      position: relative;
    }

    .progress-ring::before {
      content: '';
      position: absolute;
      inset: 0.7rem;
      border-radius: 50%;
      background: var(--theme-surface);
      border: 1px solid var(--theme-line-soft);
    }

    .progress-value {
      position: relative;
      color: var(--theme-text);
      font-size: 1.2rem;
      font-weight: 900;
    }

    .progress-copy {
      margin: 0.35rem 0 0;
      font-size: 0.86rem;
      line-height: 1.45;
    }

    .muhurtha-list {
      display: grid;
      gap: 0.65rem;
    }

    .muhurtha-row {
      display: grid;
      grid-template-columns: auto minmax(0, 1fr) auto;
      align-items: center;
      gap: 0.75rem;
      padding: 0.75rem;
    }

    .date-badge {
      display: grid;
      min-width: 3.6rem;
      place-items: center;
      color: var(--theme-accent);
      font-weight: 900;
      line-height: 1.1;
    }

    .date-badge span {
      color: var(--theme-text-muted);
      font-size: 0.65rem;
      letter-spacing: 0.08em;
      text-transform: uppercase;
    }

    .muhurtha-title {
      margin: 0;
      color: var(--theme-text);
      font-size: 0.9rem;
      font-weight: 800;
    }

    .muhurtha-time {
      margin: 0.15rem 0 0;
      color: var(--theme-text-muted);
      font-size: 0.78rem;
    }

    .quality-pill {
      padding: 0.28rem 0.55rem;
      border: 1px solid color-mix(in srgb, var(--theme-success) 38%, transparent);
      border-radius: 999px;
      color: var(--theme-success);
      font-size: 0.72rem;
      font-weight: 800;
      white-space: nowrap;
    }

    .wide-grid {
      display: grid;
      grid-template-columns: minmax(0, 1.1fr) minmax(320px, 0.9fr);
      gap: 1rem;
    }

    .empty-copy {
      padding: 0.75rem 0;
    }

    .loading-line {
      color: var(--theme-text-muted);
      font-size: 0.82rem;
    }

    @media (max-width: 1180px) {
      .dashboard-hero-grid,
      .wide-grid {
        grid-template-columns: 1fr;
      }

      .journey-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }

    @media (max-width: 900px) {
      .panchang-strip,
      .collection-grid,
      .today-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }
    }

    @media (max-width: 640px) {
      .daily-panel,
      .quick-panel {
        padding: 1rem;
      }

      .hero-title {
        font-size: 1.35rem;
      }

      .panchang-strip,
      .actions-grid,
      .journey-grid,
      .collection-grid,
      .today-grid,
      .progress-layout {
        grid-template-columns: 1fr;
      }

      .muhurtha-row {
        grid-template-columns: auto minmax(0, 1fr);
      }

      .quality-pill {
        grid-column: 2;
        width: fit-content;
      }
    }
  `],
})
export class DashboardComponent implements OnInit, OnDestroy {
  private readonly dashboardApi = inject(DashboardApiService);
  private readonly latestKundaliStorage = inject(LatestKundaliStorageService);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly subscriptions = new Subscription();

  userName = 'Seeker';
  today = new Date();
  savedKundalisCount = 0;
  latestBrowserKundali = this.latestKundaliStorage.getLatest();
  latestKundali: DashboardKundali | null = null;
  panchang: DashboardPanchang | null = null;
  currentDasha: DashboardCurrentDasha | null = null;
  learningLevels: DashboardLearningLevel[] = [];
  recentReport: DashboardSummary['recentReport'] = null;
  collectionStats: DashboardSummary['collectionStats'] = {
    savedKundalis: 0,
    savedMatches: 0,
    savedMuhurthas: 0,
    reports: 0,
    studyLevels: 0,
  };
  insightCards: DashboardInsight[] = [];
  upcomingMuhurthas: DashboardMuhurthaPreview[] = [];
  loading = {
    dashboard: true,
  };

  readonly quickActions: QuickAction[] = [
    { label: 'Generate Kundali', caption: 'Create a birth chart', routerLink: '/app/kundali', icon: 'K' },
    { label: 'Match Kundali', caption: 'Compare compatibility', routerLink: '/app/compatibility', icon: 'M' },
    { label: 'Panchang', caption: 'See today and dates', routerLink: '/app/panchang', icon: 'P' },
    { label: 'Muhurtha', caption: 'Find auspicious time', routerLink: '/app/panchang/muhurtha', icon: 'T' },
    { label: 'Dasha', caption: 'Current period flow', routerLink: '/app/dasha', icon: 'D' },
    { label: 'Jyotish Study', caption: 'Continue learning', routerLink: '/app/learning', icon: 'J' },
  ];

  ngOnInit(): void {
    this.loadDashboard();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get displayDate(): string {
    return new Intl.DateTimeFormat('en-IN', {
      weekday: 'long',
      day: '2-digit',
      month: 'long',
      year: 'numeric',
    }).format(this.today);
  }

  get greeting(): string {
    const hour = this.today.getHours();
    if (hour < 12) {
      return 'Good Morning';
    }
    if (hour < 17) {
      return 'Good Afternoon';
    }
    return 'Good Evening';
  }

  get latestName(): string {
    return this.latestKundali?.fullName || this.latestBrowserKundali?.name || 'No chart yet';
  }

  get latestBirthPlace(): string {
    return this.latestKundali?.birthPlace || this.latestBrowserKundali?.birthPlace || this.latestBrowserKundali?.birthplace || 'Add birth details to begin';
  }
  get latestKundaliRouterLink(): string | unknown[] {
    return this.latestKundali?.id ? ['/app/kundali/saved', this.latestKundali.id] : '/app/kundali';
  }

  get learningSummary(): DashboardLearningLevel | null {
    return this.learningLevels[0] ?? null;
  }

  get learningProgress(): number {
    if (!this.learningSummary) {
      return 0;
    }

    return this.learningSummary.status?.toLowerCase() === 'completed' ? 100 : 20;
  }

  formatDate(value: string | Date | undefined): string {
    const date = this.toDate(value);
    if (!date) {
      return value ? String(value) : 'Not available';
    }

    return new Intl.DateTimeFormat('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }

  formatDay(value: string | Date | undefined): string {
    const date = this.toDate(value);
    return date ? new Intl.DateTimeFormat('en-IN', { day: '2-digit' }).format(date) : '--';
  }

  formatMonth(value: string | Date | undefined): string {
    const date = this.toDate(value);
    return date ? new Intl.DateTimeFormat('en-IN', { month: 'short' }).format(date) : '';
  }

  private loadDashboard(): void {
    const sub = this.dashboardApi.getSummary().pipe(
      catchError((error) => {
        console.error('Dashboard summary load error:', error);
        return of(null);
      }),
      finalize(() => {
        this.loading.dashboard = false;
        this.cdr.markForCheck();
      })
    ).subscribe((summary) => {
      if (!summary) {
        this.insightCards = this.buildFallbackInsights();
        this.cdr.markForCheck();
        return;
      }

      this.userName = summary.user?.fullName?.trim() || 'Seeker';
      this.panchang = summary.todayPanchang ?? null;
      this.latestKundali = summary.latestKundali ?? null;
      this.currentDasha = summary.currentDasha ?? null;
      this.learningLevels = Array.isArray(summary.learningLevels) ? summary.learningLevels : [];
      this.recentReport = summary.recentReport ?? null;
      this.collectionStats = summary.collectionStats ?? this.collectionStats;
      this.savedKundalisCount = this.collectionStats.savedKundalis;
      this.insightCards = summary.dailyInsights?.length ? summary.dailyInsights : this.buildFallbackInsights();
      this.upcomingMuhurthas = summary.upcomingMuhurthas ?? [];
      this.cdr.markForCheck();
    });

    this.subscriptions.add(sub);
  }

  private buildFallbackInsights(): DashboardInsight[] {
    return [
      {
        eyebrow: 'Panchang Insight',
        title: 'Today\'s Panchang Insight',
        text: 'The dashboard summary API will show tithi, nakshatra, and auspicious context here.',
        routerLink: '/app/panchang',
        action: 'View Panchang',
        tone: 'moon',
      },
      {
        eyebrow: 'Learning Tip',
        title: 'Jyotish foundations',
        text: 'Open structured lessons to build a steady Jyotish study habit.',
        routerLink: '/app/learning',
        action: 'Open Study',
        tone: 'study',
      },
      {
        eyebrow: 'Muhurtha Focus',
        title: 'Plan with cleaner timing',
        text: 'Use Muhurtha before major starts: home, vehicle, ceremonies, business work, and travel.',
        routerLink: '/app/panchang/muhurtha',
        action: 'Find Muhurtha',
        tone: 'muhurtha',
      },
    ];
  }

  private toDate(value: string | Date | undefined): Date | null {
    if (!value) {
      return null;
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  }
}
