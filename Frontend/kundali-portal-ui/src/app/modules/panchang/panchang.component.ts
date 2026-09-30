// Panchang Module - Panchang Viewer Page
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';
import { Panchang, PanchangApiService, PanchangCalendarMode, PanchangEclipse } from '../../core/services/panchang-api.service';
import { GocharApiService, GocharDailyResponse, GocharPlanet } from '../../core/services/gochar-api.service';
import { LocationService } from '../../core/services/location.service';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { TranslocoService } from '@jsverse/transloco';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';

@Component({
  selector: 'app-panchang',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PageHeaderComponent, SectionCardComponent, NorthIndianChartComponent],
  templateUrl: './panchang.component.html',
  styles: [`
    .panchang-layout {
      display: grid;
      grid-template-columns: minmax(0, 1.65fr) minmax(320px, 0.8fr);
      gap: 1.25rem;
      align-items: start;
    }

    .calendar-shell {
      display: flex;
      flex-direction: column;
      gap: 0.2rem;
    }

    .calendar-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.32rem;
      flex-wrap: wrap;
    }

    .calendar-title-stack {
      display: grid;
      gap: 0.08rem;
      min-width: 12rem;
    }

    .month-title {
      margin: 0;
      color: var(--theme-text);
      font-size: 0.92rem;
      font-weight: 550;
    }

    .calendar-actions,
    .mode-switcher {
      display: inline-flex;
      align-items: center;
      gap: 0.16rem;
    }

    .mode-switcher {
      padding: 0.12rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
    }

    .mode-btn {
      min-height: 1.35rem;
      padding: 0 0.42rem;
      border: 0;
      border-radius: 0.38rem;
      background: transparent;
      color: var(--theme-text-muted);
      font-size: 0.62rem;
      font-weight: 500;
      cursor: pointer;
    }

    .calendar-btn {
      font-size: 0.68rem;
    }

    .mode-btn.active {
      background: var(--theme-accent);
      color: #fff;
    }

    .calendar-btn {
      min-width: 1.55rem;
      min-height: 1.55rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.5rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font-weight: 600;
      cursor: pointer;
    }

    .calendar-btn.today {
      padding: 0 0.5rem;
      color: var(--theme-accent);
    }

    .location-selector {
      position: relative;
      display: grid;
      grid-template-columns: minmax(8.5rem, 12rem) auto;
      gap: 0.28rem;
      align-items: center;
    }

    .location-input {
      width: 100%;
      min-height: 1.55rem;
      padding: 0 0.5rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font: inherit;
      font-size: 0.66rem;
      font-weight: 400;
    }

    .location-current {
      color: var(--theme-text-muted);
      font-size: 0.62rem;
      font-weight: 500;
      white-space: nowrap;
    }

    .location-results {
      position: absolute;
      z-index: 10;
      top: calc(100% + 0.25rem);
      left: 0;
      right: 0;
      max-height: 12rem;
      overflow: auto;
      border: 1px solid var(--theme-line);
      border-radius: 0.55rem;
      background: var(--theme-surface);
      box-shadow: 0 0.75rem 1.5rem rgba(15, 23, 42, 0.12);
    }

    .location-result {
      display: block;
      width: 100%;
      padding: 0.5rem 0.6rem;
      border: 0;
      background: transparent;
      color: var(--theme-text);
      text-align: left;
      font: inherit;
      font-size: 0.66rem;
      font-weight: 400;
      cursor: pointer;
    }

    .location-result:hover {
      background: color-mix(in srgb, var(--theme-accent) 8%, var(--theme-surface));
    }
    .calendar-status {
      color: var(--theme-text-muted);
      font-size: 0.68rem;
      line-height: 1.2;
    }

    .calendar-status.error {
      color: #b42318;
    }

    .eclipse-details-section {
      display: grid;
      gap: 0.5rem;
      margin-top: 0.75rem;
    }

    .eclipse-details-toggle {
      width: 100%;
      min-height: 2.4rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.75rem;
      padding: 0.55rem 0.75rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font: inherit;
      font-size: 0.76rem;
      font-weight: 600;
      cursor: pointer;
    }

    .eclipse-details-toggle:hover,
    .eclipse-details-toggle[aria-expanded="true"] {
      border-color: color-mix(in srgb, var(--theme-accent) 45%, var(--theme-line));
      background: color-mix(in srgb, var(--theme-accent) 5%, var(--theme-surface));
    }

    .eclipse-panel {
      display: grid;
      gap: 0.42rem;
      padding: 0.52rem 0.58rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.58rem;
      background: color-mix(in srgb, var(--theme-accent) 5%, var(--theme-surface));
    }

    .eclipse-header {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 0.5rem;
      flex-wrap: wrap;
    }

    .eclipse-header strong {
      color: var(--theme-text);
      font-size: 0.74rem;
      font-weight: 600;
    }

    .eclipse-header small,
    .eclipse-status,
    .eclipse-empty,
    .eclipse-meta {
      color: var(--theme-text-muted);
      font-size: 0.6rem;
      font-weight: 400;
      line-height: 1.25;
    }

    .eclipse-status.error {
      color: #b42318;
    }

    .eclipse-list {
      display: grid;
      gap: 0.32rem;
    }

    .eclipse-row {
      display: grid;
      gap: 0.16rem;
      padding: 0.42rem 0.48rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.48rem;
      background: var(--theme-surface);
    }

    .eclipse-row-head {
      display: flex;
      align-items: baseline;
      justify-content: space-between;
      gap: 0.42rem;
      flex-wrap: wrap;
    }

    .eclipse-name {
      color: var(--theme-text);
      font-size: 0.66rem;
      font-weight: 600;
      line-height: 1.2;
    }

    .eclipse-date {
      color: var(--theme-text-muted);
      font-size: 0.58rem;
      font-weight: 400;
    }


    .weekday-row,
    .calendar-grid {
      display: grid;
      grid-template-columns: repeat(7, minmax(0, 1fr));
    }

    .weekday-row {
      border: 1px solid var(--theme-line);
      border-bottom: 0;
      border-radius: 0.65rem 0.65rem 0 0;
      overflow: hidden;
      background: color-mix(in srgb, var(--theme-accent) 8%, var(--theme-surface));
    }

    .weekday-row.single-day {
      display: none;
    }

    .weekday {
      padding: 0.26rem 0.24rem;
      text-align: center;
      color: var(--theme-text);
      font-size: 0.62rem;
      font-weight: 550;
    }

    .calendar-grid {
      border: 1px solid var(--theme-line);
      border-radius: 0 0 0.65rem 0.65rem;
      overflow: hidden;
      background: var(--theme-line-soft);
      gap: 1px;
    }

    .calendar-grid.single-day {
      grid-template-columns: minmax(16rem, 1fr);
      border-radius: 0.65rem;
    }

    .date-cell {
      position: relative;
      display: flex;
      flex-direction: column;
      gap: 0.46rem;
      min-height: 7.7rem;
      padding: 0.62rem 0.58rem;
      border: 0;
      background: var(--theme-surface);
      color: var(--theme-text);
      text-align: left;
      font: inherit;
      font-weight: 400;
      cursor: pointer;
    }

    .date-cell.outside-month {
      background: color-mix(in srgb, var(--theme-surface) 74%, var(--theme-line));
      color: var(--theme-text-muted);
    }

    .date-cell.today {
      box-shadow: inset 0 0 0 2px var(--theme-accent);
    }

    .date-cell.selected {
      background: color-mix(in srgb, var(--theme-accent) 10%, var(--theme-surface));
    }

    .date-topline {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 0.25rem;
      min-height: 1.35rem;
      margin-bottom: 0.08rem;
    }

    .date-number {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 1.35rem;
      height: 1.35rem;
      border-radius: 999px;
      color: var(--theme-text);
      font-size: 0.62rem;
      font-weight: 550;
      line-height: 1;
    }

    .today .date-number {
      background: var(--theme-accent);
      color: #fff;
    }

    .day-name {
      color: color-mix(in srgb, var(--theme-text-muted) 78%, transparent);
      font-size: 0.54rem;
      font-weight: 400;
      line-height: 1;
    }

    .special-tithi-strip {
      display: flex;
      flex-wrap: wrap;
      gap: 0.18rem;
      min-height: 1rem;
      margin-top: -0.12rem;
      overflow: hidden;
    }

    .special-tithi-badge {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 0.92rem;
      height: 0.92rem;
      color: var(--theme-text);
      line-height: 1;
      white-space: nowrap;
    }
    .special-tithi-badge.amavasya .special-tithi-icon {
      background: #111827;
      color: #111827;
      box-shadow: inset 0 0 0 1px #020617;
    }

    .special-tithi-badge.purnima .special-tithi-icon {
      background: #fff7c2;
      color: #facc15;
      box-shadow: inset 0 0 0 1px #facc15, 0 0 0.28rem rgba(250, 204, 21, 0.45);
    }

    .special-tithi-badge.ekadashi .special-tithi-icon {
      background: transparent;
      color: #166534;
      width: auto;
      min-width: 0.82rem;
      font-size: 0.7rem;
      box-shadow: none;
    }

    .special-tithi-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 0.58rem;
      height: 0.58rem;
      font-size: 0.48rem;
      line-height: 1;
    }


    .panchang-mini {
      display: grid;
      gap: 0.18rem;
      min-width: 0;
      color: var(--theme-text);
      font-weight: 400;
    }

    .panchang-line {
      display: grid;
      grid-template-columns: 0.8rem minmax(0, 1fr);
      align-items: baseline;
      gap: 0.24rem;
      min-width: 0;
      min-height: 0.86rem;
      font-size: 0.55rem;
      line-height: 1.25;
    }

    .panchang-label {
      color: color-mix(in srgb, var(--theme-accent) 70%, var(--theme-text-muted));
      font-size: 0.52rem;
      font-weight: 500;
      letter-spacing: 0;
    }

    .panchang-value {
      overflow: hidden;
      color: color-mix(in srgb, var(--theme-text) 92%, var(--theme-text-muted));
      font-weight: 400;
      line-height: 1.22;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .panchang-line:first-child .panchang-value {
      color: var(--theme-text);
      font-weight: 600;
    }

    .empty-panchang {
      margin-top: 0.2rem;
      color: color-mix(in srgb, var(--theme-text-muted) 76%, transparent);
      font-size: 0.54rem;
      font-weight: 400;
      line-height: 1.2;
    }

    .detail-panel {
      display: grid;
      gap: 0.38rem;
    }

    .detail-date {
      display: grid;
      gap: 0.18rem;
      padding: 0.6rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.65rem;
      background: color-mix(in srgb, var(--theme-accent) 8%, var(--theme-surface));
    }

    .detail-date strong {
      color: var(--theme-text);
      font-size: 0.95rem;
      font-weight: 600;
      line-height: 1.25;
    }

    .detail-date span {
      color: var(--theme-text-muted);
      font-size: 0.68rem;
      font-weight: 400;
    }

    .detail-grid {
      display: grid;
      gap: 0.28rem;
    }

    .detail-row {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 0.28rem;
    }

    .detail-item {
      display: grid;
      grid-template-columns: minmax(3.45rem, 0.75fr) minmax(0, 1fr) minmax(3.55rem, auto);
      align-items: center;
      column-gap: 0.42rem;
      min-height: 1.72rem;
      padding: 0.28rem 0.42rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
    }

    .detail-label {
      display: block;
      min-height: 1.35rem;
      margin-bottom: 0.08rem;
      color: var(--theme-text-muted);
      font-size: 0.52rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .detail-value {
      color: var(--theme-text);
      font-size: 0.7rem;
      font-weight: 500;
      line-height: 1.18;
    }

    .calendar-era {
      display: flex;
      align-items: center;
      gap: 0.24rem;
      min-width: 0;
      color: var(--theme-text);
      font-size: 0.62rem;
      font-weight: 500;
    }

    .calendar-era-top {
      justify-content: flex-start;
      padding: 0;
      border: 0;
      border-radius: 0;
      background: transparent;
    }

    .calendar-era span {
      display: inline-flex;
      align-items: baseline;
      gap: 0.22rem;
      min-width: 0;
      white-space: nowrap;
    }

    .calendar-era small {
      color: var(--theme-text-muted);
      font-size: 0.5rem;
      font-weight: 500;
      text-transform: uppercase;
    }

    .era-month {
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .era-samvat {
      margin-left: 0;
    }

    .detail-end-time {
      display: block;
      margin-top: 0;
      color: var(--theme-text-muted);
      font-size: 0.5rem;
      font-weight: 400;
      line-height: 1.15;
      text-align: right;
      white-space: nowrap;
    }
    .pada-strip {
      display: grid;
      grid-template-columns: repeat(4, minmax(0, 1fr));
      gap: 0.24rem;
    }

    .pada-card {
      display: flex;
      align-items: center;
      gap: 0.22rem;
      min-height: 1.7rem;
      padding: 0.24rem 0.32rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      line-height: 1;
      min-width: 0;
      white-space: nowrap;
    }

    .pada-card.active {
      border-color: color-mix(in srgb, var(--theme-accent) 58%, var(--theme-line));
      background: color-mix(in srgb, var(--theme-accent) 8%, var(--theme-surface));
    }

    .pada-card span,
    .pada-card small {
      overflow: hidden;
      color: var(--theme-text-muted);
      font-size: 0.52rem;
      font-weight: 400;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .pada-card strong {
      flex: 0 0 auto;
      color: var(--theme-text);
      font-size: 0.66rem;
      font-weight: 500;
      white-space: nowrap;
    }

    .timing-list {
      display: grid;
      gap: 0.55rem;
    }

    .timing-row {
      display: flex;
      justify-content: space-between;
      gap: 0.2rem;
      padding: 0.48rem 0.58rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.55rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font-weight: 500;
    }

    .muted {
      color: var(--theme-text-muted);
    }

    .muhurtha-link {
      display: inline-flex;
      justify-content: center;
      margin-top: 0.25rem;
      padding: 0.55rem 0.75rem;
      border-radius: 0.5rem;
      background: var(--theme-accent, #7E3BA8);
      color: #fff;
      font-size: 0.76rem;
      font-weight: 600;
      text-decoration: none;
    }



    .gochar-panel {
      display: grid;
      gap: 0.48rem;
      padding-top: 0.2rem;
    }

    .gochar-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.45rem;
      color: var(--theme-text);
      font-size: 0.8rem;
      font-weight: 600;
    }

    .gochar-header small,
    .gochar-status {
      color: var(--theme-text-muted);
      font-size: 0.64rem;
      font-weight: 400;
    }

    .gochar-chart {
      display: grid;
      justify-items: center;
      padding: 0.35rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.55rem;
      background: var(--theme-surface);
    }

    .gochar-chart app-north-indian-chart {
      width: min(100%, 17rem);
    }

    .gochar-planets {
      display: grid;
      grid-template-columns: repeat(3, minmax(0, 1fr));
      gap: 0.24rem;
    }

    .transition-list {
      display: grid;
      gap: 0.24rem;
    }

    .gochar-planet-row {
      position: relative;
      display: grid;
      grid-template-rows: auto auto auto;
      gap: 0.08rem;
      min-height: 3.15rem;
      padding: 0.32rem 0.42rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font-size: 0.62rem;
      line-height: 1.12;
      min-width: 0;
    }

    .transition-row {
      display: grid;
      grid-template-columns: minmax(3.4rem, 0.8fr) minmax(0, 1.35fr) auto;
      align-items: center;
      gap: 0.35rem;
      padding: 0.3rem 0.45rem;
      border: 1px solid var(--theme-line);
      border-radius: 0.45rem;
      background: var(--theme-surface);
      color: var(--theme-text);
      font-size: 0.66rem;
      line-height: 1.2;
    }

    .gochar-planet-row strong,
    .transition-row strong {
      font-weight: 600;
    }

    .gochar-planet-head {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.2rem;
      min-width: 0;
    }

    .gochar-planet-sign,
    .gochar-planet-degree {
      color: var(--theme-text-muted);
      font-size: 0.58rem;
      font-weight: 400;
    }

    .gochar-planet-degree {
      color: var(--theme-text);
      font-weight: 500;
    }

    .gochar-planet-row span,
    .transition-row span {
      min-width: 0;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .retrograde-chip {
      color: var(--theme-accent);
      font-size: 0.6rem;
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .panchang-layout {
        grid-template-columns: 1fr;
      }

      .weekday {
      padding: 0.26rem 0.24rem;
      text-align: center;
      color: var(--theme-text);
      font-size: 0.62rem;
      font-weight: 550;
    }

      .date-cell {
        min-height: 6.7rem;
        padding: 0.48rem;
      }

      .day-name,
      .panchang-label {
        display: none;
      }

      .panchang-line {
        grid-template-columns: 1fr;
        font-size: 0.54rem;
      }

      .detail-row,
      .transition-row {
        grid-template-columns: 1fr;
      }

      .gochar-planets,

      .pada-strip {

        grid-template-columns: repeat(2, minmax(0, 1fr));

      }

      .detail-end-time {
        text-align: left;
      }
    }
  `],
})
export class PanchangComponent implements OnInit, OnDestroy {
  private readonly weekdayKeys = [
    'panchang.weekday.sun',
    'panchang.weekday.mon',
    'panchang.weekday.tue',
    'panchang.weekday.wed',
    'panchang.weekday.thu',
    'panchang.weekday.fri',
    'panchang.weekday.sat'
  ];
  readonly calendarModes: PanchangCalendarMode[] = ['daily', 'weekly', 'monthly'];

  calendarDays: CalendarDay[] = [];
  calendarMode: PanchangCalendarMode = 'monthly';
  currentDate = new Date();
  selectedDateKey = this.toDateKey(new Date());
  panchangByDate = new Map<string, Panchang>();
  selectedPanchangForView?: Panchang;
  selectedGochar?: GocharDailyResponse;
  isGocharLoading = false;
  gocharError = "";
  isLoading = false;
  errorMessage = '';
  yearlyEclipses: PanchangEclipse[] = [];
  yearlyEclipseYear = new Date().getFullYear();
  isEclipsesLoading = false;
  eclipseError = '';
  showEclipseDetails = false;
  locationQuery = 'Bangalore, Karnataka, India';
  locationResults: PanchangLocationResult[] = [];
  selectedLocation: PanchangLocation = {
    name: 'Bangalore, Karnataka, India',
    latitude: 12.9716,
    longitude: 77.5946,
    timezone: 'Asia/Kolkata',
  };
  private locationSearchTimer?: ReturnType<typeof setTimeout>;
  private gocharRequestId = 0;
  private eclipseRequestId = 0;
  private eclipseSubscription?: Subscription;
  private loadedEclipseKey = '';

  constructor(
    private readonly panchangApi: PanchangApiService,
    private readonly gocharApi: GocharApiService,
    private readonly locationService: LocationService,
    private readonly apiTerms: ApiTermTranslationService,
    private readonly translocoService: TranslocoService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  private languageSubscription?: Subscription;

  ngOnInit(): void {
    this.languageSubscription = this.translocoService.langChanges$.subscribe(() => {
      this.buildCalendar();
      this.cdr.markForCheck();
    });
    this.buildCalendar();
    this.loadCalendarPanchang();
  }

  ngOnDestroy(): void {
    this.languageSubscription?.unsubscribe();
  }

  get weekdays(): string[] {
    return this.weekdayKeys.map((key) => this.translocoService.translate(key));
  }

  get calendarTitle(): string {
    if (this.calendarMode === 'daily') {
      return this.formatFullDate(this.currentDate);
    }

    if (this.calendarMode === 'weekly') {
      const weekStart = this.startOfWeek(this.currentDate);
      const weekEnd = new Date(weekStart);
      weekEnd.setDate(weekStart.getDate() + 6);
      return `${this.formatShortDate(weekStart)} - ${this.formatShortDate(weekEnd)}`;
    }

    return this.currentDate.toLocaleDateString(this.activeLocale, {
      month: 'long',
      year: 'numeric',
    });
  }

  get selectedDay(): CalendarDay | undefined {
    return this.calendarDays.find((day) => day.dateKey === this.selectedDateKey);
  }

  get selectedPanchang(): Panchang | undefined {
    return this.selectedPanchangForView || this.panchangByDate.get(this.selectedDateKey);
  }

  get calendarHeaderPanchang(): Panchang | undefined {
    const currentMonthDay = this.calendarDays.find((day) => day.isCurrentMonth && this.panchangByDate.has(day.dateKey));
    const firstLoadedDay = this.calendarDays.find((day) => this.panchangByDate.has(day.dateKey));
    const selectedPanchang = this.panchangByDate.get(this.selectedDateKey);
    return currentMonthDay
      ? this.panchangByDate.get(currentMonthDay.dateKey)
      : selectedPanchang || (firstLoadedDay ? this.panchangByDate.get(firstLoadedDay.dateKey) : undefined);
  }

  setCalendarMode(mode: PanchangCalendarMode): void {
    this.calendarMode = mode;
    const selectedDay = this.selectedDay;
    this.currentDate = selectedDay ? new Date(selectedDay.date) : new Date();
    this.selectedDateKey = this.toDateKey(this.currentDate);
    this.buildCalendar();
    this.loadCalendarPanchang();
  }

  previousPeriod(): void {
    this.changePeriod(-1);
  }

  nextPeriod(): void {
    this.changePeriod(1);
  }

  goToToday(): void {
    this.currentDate = new Date();
    this.selectedDateKey = this.toDateKey(this.currentDate);
    this.buildCalendar();
    this.loadCalendarPanchang();
  }

  selectDay(day: CalendarDay): void {
    this.selectedDateKey = day.dateKey;
    this.currentDate = new Date(day.date);
    this.applySelectedPanchang();
    this.loadSelectedGochar();
    this.cdr.markForCheck();
  }

  trackByDate(_: number, day: CalendarDay): string {
    return day.dateKey;
  }

  onLocationQueryChange(value: string): void {
    this.locationQuery = value;
    this.locationResults = [];

    if (this.locationSearchTimer) {
      clearTimeout(this.locationSearchTimer);
    }

    const query = value.trim();
    if (query.length < 3) {
      return;
    }

    this.locationSearchTimer = setTimeout(() => {
      this.locationService.searchLocation(query).subscribe({
        next: (results) => {
          this.locationResults = results || [];
          this.cdr.detectChanges();
        },
        error: () => {
          this.locationResults = [];
          this.cdr.detectChanges();
        },
      });
    }, 500);
  }

  selectLocation(place: PanchangLocationResult): void {
    this.selectedLocation = {
      name: place.displayName,
      latitude: Number(place.latitude),
      longitude: Number(place.longitude),
      timezone: 'Asia/Kolkata',
    };
    this.locationQuery = place.displayName;
    this.locationResults = [];
    this.loadCalendarPanchang();
  }

  currentPadaSyllable(panchang: Panchang): string {
    return panchang.nakshatraPadas?.find((pada) => pada.isCurrent)?.syllable || '';
  }


  specialTithiMarkers(panchang: Panchang): SpecialTithiMarker[] {
    const tithi = (panchang.tithi || '').toLowerCase();
    const markers: SpecialTithiMarker[] = [];

    if (tithi.includes('amavasya') || tithi.includes('amavas')) {
      markers.push({ kind: 'amavasya', icon: '?', label: 'Amavasya' });
    }

    if (tithi.includes('purnima') || tithi.includes('poornima') || tithi.includes('paurnima')) {
      markers.push({ kind: 'purnima', icon: '?', label: 'Purnima' });
    }

    if (tithi.includes('ekadashi')) {
      markers.push({ kind: 'ekadashi', icon: '?', label: 'Ekadashi' });
    }

    if (tithi.includes('chaturthi')) {
      markers.push({ kind: 'chaturthi', icon: '4', label: 'Chaturthi' });
    }

    return markers;
  }
  formatMode(mode: PanchangCalendarMode): string {
    return this.t('panchang.mode.' + mode);
  }

  t(key: string): string {
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : this.fallbackLabels[key] || key;
  }

  translatePanchangValue(field: 'maas' | 'paksha' | 'tithi' | 'nakshatra' | 'yoga' | 'karana' | 'vaar', value: unknown): string {
    return this.apiTerms.translatePanchangValue(field, value);
  }

  translatePlanet(value: unknown): string {
    return this.apiTerms.translatePlanet(value);
  }

  translateSign(value: unknown): string {
    return this.apiTerms.translateSign(value);
  }

  formatDegree(planet: GocharPlanet): string {
    return planet.degree + ' deg ' + planet.minute + '\'';
  }

  formatGocharDate(value: string | Date): string {
    return this.formatShortDate(this.parseDate(String(value)));
  }
  formatEclipseName(eclipse: PanchangEclipse): string {
    return `${eclipse.type} ${this.t(`panchang.eclipse.${eclipse.kind}`)}`.trim();
  }

  formatEclipseDate(value: string): string {
    return new Date(value).toLocaleDateString(this.activeLocale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  formatEclipseClock(value: string): string {
    return new Date(value).toLocaleTimeString(this.activeLocale, {
      hour: '2-digit',
      minute: '2-digit',
      hour12: false,
    });
  }

  formatEclipseRange(startValue: string | null | undefined, endValue: string | null | undefined): string {
    if (!startValue || !endValue) {
      return '-';
    }

    return `${this.formatEclipseClock(startValue)} - ${this.formatEclipseClock(endValue)}`;
  }

  formatFullDate(date: Date): string {
    return date.toLocaleDateString(this.activeLocale, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  }

  private changePeriod(offset: number): void {
    if (this.calendarMode === 'daily') {
      this.currentDate = this.addDays(this.currentDate, offset);
    } else if (this.calendarMode === 'weekly') {
      this.currentDate = this.addDays(this.currentDate, offset * 7);
    } else {
      this.currentDate = new Date(this.currentDate.getFullYear(), this.currentDate.getMonth() + offset, 1);
    }

    this.selectedDateKey = this.toDateKey(this.currentDate);
    this.buildCalendar();
    this.loadCalendarPanchang();
  }

  private buildCalendar(): void {
    const todayKey = this.toDateKey(new Date());
    const dates = this.getDisplayDates();

    this.calendarDays = dates.map((date) => {
      const dateKey = this.toDateKey(date);
      return {
        date,
        dateKey,
        dayNumber: date.getDate(),
        dayName: this.weekdays[date.getDay()],
        isCurrentMonth: date.getMonth() === this.currentDate.getMonth(),
        isToday: dateKey === todayKey,
      };
    });
  }

  private getDisplayDates(): Date[] {
    if (this.calendarMode === 'daily') {
      return [new Date(this.currentDate)];
    }

    const start = this.calendarMode === 'weekly'
      ? this.startOfWeek(this.currentDate)
      : this.startOfCalendarMonth(this.currentDate);
    const dayCount = this.calendarMode === 'weekly' ? 7 : 42;
    const dates: Date[] = [];

    for (let index = 0; index < dayCount; index += 1) {
      dates.push(this.addDays(start, index));
    }

    return dates;
  }

  private loadCalendarPanchang(): void {
    this.isLoading = true;
    if (this.showEclipseDetails) {
      this.loadYearlyEclipses(this.currentDate.getFullYear());
    }
    this.errorMessage = '';
    this.selectedPanchangForView = undefined;

    this.panchangApi.getPanchangCalendar({
      mode: this.calendarMode,
      date: this.currentDate,
      latitude: this.selectedLocation.latitude,
      longitude: this.selectedLocation.longitude,
      timezone: this.selectedLocation.timezone,
    }).subscribe({
      next: (response) => {
        const panchangList = response?.data?.days || [];
        this.panchangByDate = new Map(
          panchangList.map((panchang) => [this.toDateKey(this.parseDate(panchang.date)), panchang])
        );
        this.syncSelectedDateAfterLoad(panchangList);
        this.applySelectedPanchang();
        this.loadSelectedGochar();
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (error: Error) => {
        this.errorMessage = error.message || 'Unable to load Panchang calendar.';
        this.panchangByDate = new Map();
        this.selectedPanchangForView = undefined;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
    });
  }



  toggleEclipseDetails(): void {
    this.showEclipseDetails = !this.showEclipseDetails;

    if (!this.showEclipseDetails) {
      this.eclipseSubscription?.unsubscribe();
      this.eclipseSubscription = undefined;
      this.isEclipsesLoading = false;
      return;
    }

    this.loadYearlyEclipses(this.currentDate.getFullYear());
  }

  private loadYearlyEclipses(year: number): void {
    const requestKey = [
      year,
      this.selectedLocation.latitude.toFixed(4),
      this.selectedLocation.longitude.toFixed(4),
      this.selectedLocation.timezone,
    ].join('|');

    this.yearlyEclipseYear = year;
    if (requestKey === this.loadedEclipseKey && !this.eclipseError) {
      return;
    }

    this.eclipseSubscription?.unsubscribe();
    const requestId = ++this.eclipseRequestId;
    this.isEclipsesLoading = true;
    this.eclipseError = '';
    this.yearlyEclipses = [];

    this.eclipseSubscription = this.panchangApi.getYearlyEclipses(
      year,
      this.selectedLocation.latitude,
      this.selectedLocation.longitude,
      this.selectedLocation.timezone,
    ).subscribe({
      next: (response) => {
        if (requestId !== this.eclipseRequestId || !this.showEclipseDetails) {
          return;
        }

        const payload = response?.data ?? response;
        this.yearlyEclipses = Array.isArray(payload?.eclipses) ? payload.eclipses : [];
        this.loadedEclipseKey = requestKey;
        this.isEclipsesLoading = false;
        this.eclipseSubscription = undefined;
        this.cdr.detectChanges();
      },
      error: (error: Error) => {
        if (requestId !== this.eclipseRequestId || !this.showEclipseDetails) {
          return;
        }

        this.yearlyEclipses = [];
        this.loadedEclipseKey = '';
        this.eclipseError = error.message || 'Unable to load yearly eclipses.';
        this.isEclipsesLoading = false;
        this.eclipseSubscription = undefined;
        this.cdr.detectChanges();
      },
    });
  }
  private loadSelectedGochar(date: Date = this.currentDate): void {
    const requestId = ++this.gocharRequestId;
    const selectedDate = new Date(date);

    this.isGocharLoading = true;
    this.gocharError = '';
    this.selectedGochar = undefined;

    this.gocharApi.getDailyGochar({
      date: selectedDate,
      latitude: this.selectedLocation.latitude,
      longitude: this.selectedLocation.longitude,
      timezone: this.selectedLocation.timezone,
    }).subscribe({
      next: (response) => {
        if (requestId !== this.gocharRequestId) {
          return;
        }

        this.selectedGochar = response;
        this.isGocharLoading = false;
        this.cdr.detectChanges();
      },
      error: (error: Error) => {
        if (requestId !== this.gocharRequestId) {
          return;
        }

        this.selectedGochar = undefined;
        this.gocharError = error.message || 'Unable to load Gochar.';
        this.isGocharLoading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private applySelectedPanchang(): void {
    this.selectedPanchangForView = this.panchangByDate.get(this.selectedDateKey);
  }

  private syncSelectedDateAfterLoad(panchangList: Panchang[]): void {
    if (this.panchangByDate.has(this.selectedDateKey)) {
      return;
    }

    const currentMonthDay = this.calendarDays.find((day) => day.isCurrentMonth && this.panchangByDate.has(day.dateKey));
    const firstLoadedDay = this.calendarDays.find((day) => this.panchangByDate.has(day.dateKey));
    const firstApiDay = panchangList[0]?.date ? this.parseDate(panchangList[0].date) : undefined;
    const fallbackDate = currentMonthDay?.date || firstLoadedDay?.date || firstApiDay;

    if (fallbackDate) {
      this.currentDate = new Date(fallbackDate);
      this.selectedDateKey = this.toDateKey(fallbackDate);
    }
  }

  private startOfCalendarMonth(date: Date): Date {
    const firstOfMonth = new Date(date.getFullYear(), date.getMonth(), 1);
    return this.addDays(firstOfMonth, -firstOfMonth.getDay());
  }

  private startOfWeek(date: Date): Date {
    return this.addDays(date, -date.getDay());
  }

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  private parseDate(date: Date | string): Date {
    if (date instanceof Date) {
      return date;
    }

    const [datePart] = date.split('T');
    const [year, month, day] = datePart.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private formatShortDate(date: Date): string {
    return date.toLocaleDateString(this.activeLocale, {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    });
  }

  private get activeLocale(): string {
    const lang = this.translocoService.getActiveLang();
    if (lang === 'hi') {
      return 'hi-IN';
    }

    if (lang === 'kn') {
      return 'kn-IN';
    }

    return 'en-IN';
  }

  private readonly fallbackLabels: Record<string, string> = {
    'panchang.title': 'Panchang',
    'panchang.calendarSubtitle': 'Hindu calendar and timings',
    'panchang.calendarDescription': 'View the daily Panchang inside a calendar with Tithi, Nakshatra, Yoga, Karana, and important timings.',
    'panchang.calendarTitle': 'Hindu Calendar',
    'panchang.selectedDay': 'Selected Day',
    'panchang.locationSearch': 'Search city for Panchang',
    'panchang.locationAria': 'Search Panchang location',
    'panchang.loadingDaily': 'Loading daily Panchang...',
    'panchang.eclipseTitle': 'Eclipses in',
    'panchang.loadingEclipses': 'Loading yearly eclipses...',
    'panchang.eclipseUnavailable': 'Yearly eclipse timings are not available for this location yet.',
    'panchang.eclipse.solar': 'Solar Eclipse',
    'panchang.eclipse.lunar': 'Lunar Eclipse',
    'panchang.eclipseWindow': 'Eclipse',
    'panchang.maximum': 'Maximum',
    'panchang.sutak': 'Sutak',
    'panchang.noData': 'No Panchang data',
    'panchang.today': 'Today',
    'panchang.previous': 'Previous period',
    'panchang.next': 'Next period',
    'panchang.mode.daily': 'Daily',
    'panchang.mode.weekly': 'Weekly',
    'panchang.mode.monthly': 'Monthly',
    'panchang.month': 'Month',
    'panchang.paksha': 'Paksha',
    'panchang.pada': 'Pada',
    'panchang.padaShort': 'P',
    'panchang.sunrise': 'Sunrise',
    'panchang.sunset': 'Sunset',
    'panchang.moonrise': 'Moonrise',
    'panchang.moonset': 'Moonset',
    'panchang.ends': 'Ends',
    'panchang.detailsUnavailable': 'Daily Panchang details are not available for this date yet.',
    'panchang.openMuhurtha': 'Open Muhurtha Finder',
    'panchang.gocharTitle': 'Gochar Kundali',
    'panchang.gocharDate': 'Selected date transit',
    'panchang.loadingGochar': 'Loading Gochar...',
    'panchang.gocharUnavailable': 'Gochar details are not available for this date.',
    'panchang.currentTransits': 'Current transits',
    'panchang.signChanges': 'Sign changes',
    'panchang.retrogradeShort': 'R',
    'panchang.noSignChanges': 'No sign changes found near this date.',
  };
  private toDateKey(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}

interface PanchangLocation {
  name: string;
  latitude: number;
  longitude: number;
  timezone: string;
}

interface PanchangLocationResult {
  id: string;
  name: string;
  displayName: string;
  latitude: number;
  longitude: number;
}

interface SpecialTithiMarker {
  kind: 'amavasya' | 'purnima' | 'ekadashi' | 'chaturthi';
  icon: string;
  label: string;
}
interface CalendarDay {
  date: Date;
  dateKey: string;
  dayNumber: number;
  dayName: string;
  isCurrentMonth: boolean;
  isToday: boolean;
}



