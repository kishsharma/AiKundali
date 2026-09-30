import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { TranslocoPipe } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';
import {
  GrahaPraveshaSearchResponse,
  GrahaPraveshaWindow,
  MuhurthaApiService,
} from '../../core/services/muhurtha-api.service';
import { LocationService } from '../../core/services/location.service';

@Component({
  selector: 'app-graha-pravesha-muhurtha',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, SectionCardComponent, TranslocoPipe],
  template: `
    <app-page-header
      [title]="'grahaPravesha.title' | transloco"
      [subtitle]="'grahaPravesha.subtitle' | transloco">
    </app-page-header>

    <section class="gp-shell">
      <app-section-card [title]="'grahaPravesha.title' | transloco" [interactive]="false">
        <form class="gp-form" [formGroup]="form" (ngSubmit)="find()">
          <label>
            <span>{{ 'grahaPravesha.location' | transloco }}</span>
            <input type="text" formControlName="city" autocomplete="off" />
            <div class="gp-dropdown" *ngIf="locationResults.length">
              <button type="button" *ngFor="let place of locationResults" (click)="selectLocation(place)">{{ place.displayName }}</button>
            </div>
          </label>
          <label>
            <span>{{ 'grahaPravesha.dateRange' | transloco }}</span>
            <span class="gp-dates">
              <input type="date" formControlName="fromDate" />
              <input type="date" formControlName="toDate" />
            </span>
          </label>
          <label>
            <span>{{ 'grahaPravesha.calendar' | transloco }}</span>
            <select formControlName="calendarSystem">
              <option value="AMANTA">Amanta</option>
              <option value="PURNIMANTA">Purnimanta</option>
            </select>
          </label>
          <button type="submit" [disabled]="loading()">{{ (loading() ? 'grahaPravesha.finding' : 'grahaPravesha.find') | transloco }}</button>
        </form>
        <p class="gp-error" *ngIf="errorMessage()">{{ errorMessage() }}</p>
        <a routerLink="/app/panchang/muhurtha">{{ 'grahaPravesha.back' | transloco }}</a>
      </app-section-card>

      <app-section-card [title]="'grahaPravesha.results' | transloco" [interactive]="false">
        <p *ngIf="!response()">{{ 'grahaPravesha.empty' | transloco }}</p>
        <p *ngIf="isEmptyResult()">{{ 'grahaPravesha.none' | transloco }}</p>
        <article class="gp-card" *ngFor="let item of response()?.results">
          <header>
            <strong>{{ item.date }}</strong>
            <span class="gp-status" [attr.data-status]="item.status">{{ item.status }}</span>
          </header>
          <p>{{ formatClock(item.start) }} - {{ formatClock(item.end) }}</p>
          <p>{{ 'grahaPravesha.lagna' | transloco }}: {{ item.lagna }}</p>
          <ul>
            <li *ngFor="let line of checklist(item)">{{ line }}</li>
          </ul>
          <button type="button" (click)="open(item)">{{ 'grahaPravesha.viewDetails' | transloco }}</button>
        </article>
      </app-section-card>
    </section>

    <aside class="gp-drawer" *ngIf="selected() as item">
      <button type="button" class="gp-close" (click)="selected.set(null)">{{ 'grahaPravesha.close' | transloco }}</button>
      <h2>{{ 'grahaPravesha.title' | transloco }}</h2>
      <p>{{ item.date }}</p>
      <p>{{ formatClock(item.start) }} - {{ formatClock(item.end) }}</p>
      <p>{{ 'grahaPravesha.location' | transloco }}: {{ form.controls.city.value }}</p>

      <h3>{{ 'grahaPravesha.panchang' | transloco }}</h3>
      <ul>
        <li>Tithi: {{ mark(item, 'TITHI') }} {{ item.tithi }}</li>
        <li>Nakshatra: {{ mark(item, 'NAKSHATRA') }} {{ item.nakshatra }}</li>
        <li>Yoga: {{ mark(item, 'YOGA') }} {{ item.yoga }}</li>
        <li>Karana: {{ mark(item, 'KARANA') }} {{ item.karana }}</li>
        <li>Vara: {{ mark(item, 'VARA') }} {{ item.vara }}</li>
      </ul>

      <h3>{{ 'grahaPravesha.timing' | transloco }}</h3>
      <ul>
        <li *ngFor="let period of item.timingRestrictions">
          {{ period.name }}: {{ formatClock(period.start) }} - {{ formatClock(period.end) }}
          <span>{{ overlaps(item, period.start, period.end) ? 'Overlap' : 'Outside selected Muhurtha' }}</span>
        </li>
      </ul>

      <h3>{{ 'grahaPravesha.chakra' | transloco }}</h3>
      <p>{{ 'grahaPravesha.kalasha' | transloco }}: {{ item.kalashaCount }}</p>
      <p>{{ item.kalashaEligible ? 'Eligible' : 'Outside favorable range' }}</p>

      <h3>{{ 'grahaPravesha.lagna' | transloco }}</h3>
      <p>{{ mark(item, 'LAGNA') }} {{ item.lagna }}</p>

      <h3>{{ 'grahaPravesha.why' | transloco }}</h3>
      <ul class="gp-evidence">
        <li *ngFor="let evidence of item.evidence" [attr.data-result]="evidence.result">
          <strong>{{ evidence.result }}</strong>
          {{ evidence.ruleId }} — {{ evidence.message }}
        </li>
      </ul>

      <h3 *ngIf="response()?.knownGaps?.length">{{ 'grahaPravesha.gaps' | transloco }}</h3>
      <ul>
        <li *ngFor="let gap of response()?.knownGaps">{{ gap }}</li>
      </ul>
    </aside>
  `,
  styles: [`
    .gp-shell { display: grid; gap: 16px; }
    .gp-form { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 12px; align-items: end; }
    .gp-form label { display: grid; gap: 6px; position: relative; }
    .gp-dates { display: flex; gap: 8px; }
    .gp-dropdown { position: absolute; top: 100%; z-index: 2; background: #fff; border: 1px solid #ddd; display: grid; min-width: 240px; }
    .gp-dropdown button, .gp-card button, .gp-form button, .gp-close { cursor: pointer; }
    .gp-card { border: 1px solid #e6d7b8; border-radius: 12px; padding: 14px; margin-top: 12px; display: grid; gap: 6px; }
    .gp-card header { display: flex; justify-content: space-between; gap: 12px; }
    .gp-status { font-size: 12px; letter-spacing: .04em; }
    .gp-status[data-status="RECOMMENDED"] { color: #1b7f3a; }
    .gp-status[data-status="SUITABLE"] { color: #8a5a12; }
    .gp-status[data-status="ACCEPTABLE"] { color: #555; }
    .gp-error { color: #9b2335; }
    .gp-drawer { position: fixed; top: 0; right: 0; width: min(420px, 100%); height: 100%; overflow: auto; background: #fffaf2; border-left: 1px solid #e6d7b8; padding: 20px; z-index: 20; box-shadow: -8px 0 24px rgba(0,0,0,.08); }
    .gp-evidence li[data-result="FAIL"] { color: #9b2335; }
    .gp-evidence li[data-result="WARNING"] { color: #8a5a12; }
    .gp-evidence li[data-result="PASS"] { color: #1b7f3a; }
  `],
})
export class GrahaPraveshaMuhurthaComponent implements OnInit {
  private fb = inject(FormBuilder);
  private api = inject(MuhurthaApiService);
  private locations = inject(LocationService);
  private cdr = inject(ChangeDetectorRef);

  loading = signal(false);
  errorMessage = signal('');
  response = signal<GrahaPraveshaSearchResponse | null>(null);
  selected = signal<GrahaPraveshaWindow | null>(null);
  locationResults: any[] = [];

  readonly form = this.fb.group({
    city: ['Bengaluru', Validators.required],
    latitude: [12.9716, Validators.required],
    longitude: [77.5946, Validators.required],
    timezone: ['Asia/Kolkata', Validators.required],
    fromDate: ['2027-04-01', Validators.required],
    toDate: ['2027-04-30', Validators.required],
    calendarSystem: ['AMANTA', Validators.required],
  });

  ngOnInit(): void {
    this.form.controls.city.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap((value) => {
        const query = value || '';
        return query.trim().length < 3 ? of([]) : this.locations.searchLocation(query);
      }),
    ).subscribe((results) => {
      this.locationResults = results;
      this.cdr.detectChanges();
    });
  }

  selectLocation(place: any): void {
    this.form.patchValue({
      city: place.displayName,
      latitude: Number(place.latitude),
      longitude: Number(place.longitude),
      timezone: String(place.countryCode || '').toLowerCase() === 'in' ? 'Asia/Kolkata' : (place.timezone || this.form.controls.timezone.value),
    }, { emitEvent: false });
    this.locationResults = [];
  }

  find(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.loading.set(true);
    this.errorMessage.set('');
    this.selected.set(null);
    this.api.searchGrahaPravesha({
      location: {
        city: value.city || '',
        latitude: Number(value.latitude),
        longitude: Number(value.longitude),
        timezone: value.timezone || 'Asia/Kolkata',
      },
      fromDate: value.fromDate || '',
      toDate: value.toDate || '',
      calendarSystem: value.calendarSystem || 'AMANTA',
      birthChart: null,
    }).subscribe({
      next: (response) => {
        this.response.set(response);
        this.loading.set(false);
      },
      error: (error) => {
        this.errorMessage.set(error?.message || 'Unable to find Graha Pravesha Muhurtha.');
        this.loading.set(false);
      },
    });
  }

  open(item: GrahaPraveshaWindow): void {
    this.selected.set(item);
  }

  isEmptyResult(): boolean {
    const current = this.response();
    return !!current && current.results.length === 0;
  }

  checklist(item: GrahaPraveshaWindow): string[] {
    return ['TITHI', 'NAKSHATRA', 'VARA', 'KALASHA', 'RAHU_KALAM', 'LAGNA']
      .map((category) => {
        const evidence = item.evidence.find((entry) => entry.category === category && entry.result !== 'SKIPPED');
        if (!evidence) {
          return '';
        }
        const mark = evidence.result === 'FAIL' ? '✗' : evidence.result === 'WARNING' ? '!' : '✓';
        return `${mark} ${evidence.message}`;
      })
      .filter((line) => line.length > 0);
  }

  mark(item: GrahaPraveshaWindow, category: string): string {
    const evidence = item.evidence.find((entry) => entry.category === category && entry.result !== 'SKIPPED');
    if (!evidence) {
      return '';
    }
    if (evidence.result === 'FAIL') {
      return '✗';
    }
    if (evidence.result === 'WARNING') {
      return '!';
    }
    return '✓';
  }

  overlaps(item: GrahaPraveshaWindow, start: string, end: string): boolean {
    return start < item.end && end > item.start;
  }

  formatClock(value: string): string {
    const [hourText, minuteText] = value.split(':');
    const hour = Number(hourText);
    if (Number.isNaN(hour) || minuteText === undefined) {
      return value;
    }
    const suffix = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;
    return `${hour12}:${minuteText} ${suffix}`;
  }
}
