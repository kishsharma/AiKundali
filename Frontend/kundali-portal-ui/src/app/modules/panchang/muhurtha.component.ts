import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { SectionCardComponent } from '../../shared/components/section-card.component';
import { FindMuhurthaRequest, MuhurthaApiService, MuhurthaRecommendation, MuhurthaResponse, MuhurthaRuleResult } from '../../core/services/muhurtha-api.service';
import { LocationService } from '../../core/services/location.service';
import { PortalNotesComponent } from '../../shared/components/portal-notes/portal-notes.component';
import { PortalWheelPickerComponent } from '../../shared/components/portal-wheel-picker.component';

interface Option {
  label: string;
  value: number;
}

interface PanchangDayFact {
  label: string;
  value: string;
  window?: string;
  highlight?: boolean;
}

interface PanchangDayGroup {
  key: string;
  dateLabel: string;
  hinduLabel: string;
  slots: MuhurthaRecommendation[];
  bestScore: number;
  firstSlot: MuhurthaRecommendation;
}

@Component({
  selector: 'app-muhurtha',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, SectionCardComponent, PortalNotesComponent, PortalWheelPickerComponent],
  template: `
    <app-page-header
      title="Muhurtha"
      subtitle="Auspicious timing finder"
      [showActions]="true">
      <button page-actions type="button" class="notes-page-action" (click)="openNotes()" aria-label="Open Muhurtha notes" title="Notes">
        <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false"><path d="M5 4h14v16H5z"></path><path d="M8 8h8"></path><path d="M8 12h8"></path><path d="M8 16h5"></path></svg>
        <span>Notes</span>
      </button>
    </app-page-header>

    <app-portal-notes
      [open]="isNotesOpen"
      [allowPageSave]="true"
      (closed)="closeNotes()"
      contextType="Muhurtha"
      title="Muhurtha Notes"
      subtitle="Private notes saved for the Muhurtha page.">
    </app-portal-notes>

    <div class="muhurtha-shell">
      <app-section-card title="Search Criteria" [interactive]="false">
        <form class="muhurtha-form" [formGroup]="form" (ngSubmit)="findMuhurtha()">
          <div class="input-strip">
            <label><span>Event</span><select formControlName="eventType"><option *ngFor="let event of eventTypes" [ngValue]="event.value">{{ event.label }}</option></select></label>
            <label><span>Regional Profile</span><select formControlName="regionalProfile"><option *ngFor="let profile of regionalProfiles" [ngValue]="profile.value">{{ profile.label }}</option></select></label>
            <label><span>From Date</span><app-portal-wheel-picker mode="date" label="From Date" placeholder="Select start date" formControlName="requestedStart"></app-portal-wheel-picker></label>
            <label><span>To Date</span><app-portal-wheel-picker mode="date" label="To Date" placeholder="Select end date" formControlName="requestedEnd"></app-portal-wheel-picker></label>
            <label><span>Daily Start</span><app-portal-wheel-picker mode="time" label="Daily Start" placeholder="Select daily start" formControlName="dailyStartTime"></app-portal-wheel-picker></label>
            <label><span>Daily End</span><app-portal-wheel-picker mode="time" label="Daily End" placeholder="Select daily end" formControlName="dailyEndTime"></app-portal-wheel-picker></label>
            <label class="location-field"><span>Place</span><input type="text" formControlName="locationName" autocomplete="off" />
              <div class="location-dropdown" *ngIf="locationResults.length">
                <button type="button" class="location-item" *ngFor="let place of locationResults" (click)="selectLocation(place)">{{ place.displayName }}</button>
              </div>
            </label>
            <label class="location-field"><span>Timezone</span><input type="text" formControlName="timezone" autocomplete="off" (focus)="showTimezoneOptions(false)" />
              <div class="location-dropdown compact" *ngIf="timezoneResults.length">
                <button type="button" class="location-item" *ngFor="let timezone of timezoneResults" (click)="selectTimezone(timezone)">{{ timezone }}</button>
              </div>
            </label>
            <label><span>Latitude</span><input type="number" step="0.000001" formControlName="latitude" /></label>
            <label><span>Longitude</span><input type="number" step="0.000001" formControlName="longitude" /></label>
          </div>

          <div class="criteria-toolbar">
            <div class="toolbar-filters">
              <label class="check-row"><input type="checkbox" formControlName="avoidRahuKalam" /><span>Avoid Rahu Kalam</span></label>
              <label class="check-row"><input type="checkbox" formControlName="avoidBhadra" /><span>Avoid Bhadra</span></label>
              <label class="check-row"><input type="checkbox" formControlName="preferGoodChoghadiya" /><span>Good Choghadiya</span></label>
              <label class="check-row"><input type="checkbox" formControlName="checkMuhurthaLagna" /><span>Lagna</span></label>
              <label class="check-row strong"><input type="checkbox" formControlName="checkPersonalHoroscope" /><span>Personalized</span></label>
            </div>
            <div class="range-buttons">
              <button type="button" (click)="setQuickRange(8)">Next 8 days</button>
              <button type="button" (click)="setQuickRange(15)">15 days</button>
              <button type="button" (click)="setQuickRange(30)">30 days</button>
            </div>
            <div class="actions">
              <button type="submit" [disabled]="loading() || form.invalid">{{ loading() ? 'Finding...' : 'Find Muhurtha' }}</button>
              <span class="hint" *ngIf="form.invalid">Complete required fields.</span>
            </div>
          </div>

          <div class="personal-fields" *ngIf="isPersonalized()">
            <label><span>Name</span><input type="text" formControlName="personName" /></label>
            <label><span>Birth Date</span><app-portal-wheel-picker mode="date" label="Birth Date" placeholder="Select birth date" formControlName="birthDate"></app-portal-wheel-picker></label>
            <label><span>Birth Time</span><app-portal-wheel-picker mode="time" label="Birth Time" placeholder="Select birth time" formControlName="birthTime"></app-portal-wheel-picker></label>
            <label class="location-field"><span>Birth Place</span><input type="text" formControlName="birthPlace" autocomplete="off" />
              <div class="location-dropdown" *ngIf="birthLocationResults.length">
                <button type="button" class="location-item" *ngFor="let place of birthLocationResults" (click)="selectBirthLocation(place)">{{ place.displayName }}</button>
              </div>
            </label>
            <label><span>Birth Latitude</span><input type="number" step="0.000001" formControlName="birthLatitude" /></label>
            <label><span>Birth Longitude</span><input type="number" step="0.000001" formControlName="birthLongitude" /></label>
            <label class="location-field"><span>Birth Timezone</span><input type="text" formControlName="birthTimezone" autocomplete="off" (focus)="showTimezoneOptions(true)" />
              <div class="location-dropdown compact" *ngIf="birthTimezoneResults.length">
                <button type="button" class="location-item" *ngFor="let timezone of birthTimezoneResults" (click)="selectBirthTimezone(timezone)">{{ timezone }}</button>
              </div>
            </label>
          </div>
        </form>
      </app-section-card>

      <app-section-card title="Recommendations" [interactive]="false">
        <div class="state error" *ngIf="errorMessage()">{{ errorMessage() }}</div>
        <div class="state" *ngIf="!loading() && !errorMessage() && !response()">Select event, dates, and location to find Muhurtha windows.</div>
        <div class="state" *ngIf="loading()">Calculating Panchanga and Muhurtha rules...</div>

        <ng-container *ngIf="response() as result">
          <div class="response-toolbar" [formGroup]="form">
            <div class="response-summary">
              <span>{{ visibleSlots().length }} visible of {{ result.recommendedSlots.length }} slots</span>
              <span *ngIf="result.usedPersonalHoroscope">Personalized checks applied</span>
              <span *ngIf="!result.usedPersonalHoroscope">Generic Muhurtha mode</span>
              <span class="year-context" *ngIf="hinduYearLabel() as yearLabel">{{ yearLabel }}</span>
            </div>
            <div class="result-controls" *ngIf="hasResultSlots()">
              <div class="view-toggle">
                <button type="button" [class.active]="isCardView()" (click)="setResultView('card')">Card</button>
                <button type="button" [class.active]="isTableView()" (click)="setResultView('table')">Table</button>
                <button type="button" [class.active]="isPanchangView()" (click)="setResultView('panchang')">Panchang</button>
              </div>
              <div class="quality-filter">
                <button type="button" class="quality-pill all" [class.active]="isQualityFilter('all')" (click)="setQualityFilter('all')"><i></i>All</button>
                <button type="button" class="quality-pill favourable" [class.active]="isQualityFilter('favourable')" (click)="setQualityFilter('favourable')"><i></i>Favourable</button>
                <button type="button" class="quality-pill acceptable" [class.active]="isQualityFilter('acceptable')" (click)="setQualityFilter('acceptable')"><i></i>Acceptable</button>
                <button type="button" class="quality-pill weak" [class.active]="isQualityFilter('weak')" (click)="setQualityFilter('weak')"><i></i>Weak</button>
              </div>
              <label class="sort-field"><span>Sort</span><select formControlName="resultSort"><option value="score">Score based</option><option value="time">Time based</option></select></label>
            </div>
          </div>

          <div class="unsupported" *ngIf="result.unsupportedCalculations.length"><strong>Not yet included:</strong> {{ result.unsupportedCalculations.join(', ') }}</div>

          <ng-container *ngIf="visibleSlots().length; else emptyResults">
            <div class="panchang-view" *ngIf="isPanchangView(); else standardResults">
              <div class="panchang-range-strip">
                <span>{{ hinduPeriodLabel() }}</span>
                <span>{{ englishPeriodLabel() }}</span>
              </div>

              <section class="panchang-day" *ngFor="let group of panchangDayGroups()">
                <header class="panchang-day-head">
                  <div>
                    <strong>{{ group.dateLabel }}</strong>
                    <span>{{ group.hinduLabel }}</span>
                  </div>
                  <div class="panchang-day-meta">
                    <span>{{ group.slots.length }} slots</span>
                    <span>Best {{ group.bestScore }}</span>
                  </div>
                </header>

                <div class="panchang-facts">
                  <div class="panchang-fact" *ngFor="let fact of panchangDayFacts(group)" [class.highlight]="fact.highlight">
                    <span>{{ fact.label }}</span>
                    <strong>{{ fact.value }}</strong>
                    <small *ngIf="fact.window">{{ fact.window }}</small>
                  </div>
                </div>

                <div class="panchang-slots">
                  <div class="panchang-slot-row" *ngFor="let slot of group.slots">
                    <div class="panchang-slot-time">
                      <code [title]="slot.slotId">{{ shortSlotId(slot) }}</code>
                      <strong>{{ formatTime(slot.startTime) }} - {{ formatTime(slot.endTime) }}</strong>
                    </div>
                    <div class="panchang-slot-main">
                      <span>{{ muhurthaCategory(slot) }}</span>
                      <small><b>Lagna:</b> {{ timingName(slot, 'lagna') }} <b>Chandra:</b> {{ timingName(slot, 'chandraRashi') }}</small>
                    </div>
                    <div class="slot-rule-actions">
                      <span class="panchang-slot-score" [ngClass]="scoreClass(slot)" [title]="scoreTooltip(slot)">{{ slot.score }} {{ verdictLabel(slot.verdict) }}</span>
                      <div class="rule-info-wrap">
                        <button type="button" class="rule-info-btn" title="Rules used" (click)="toggleRuleInfo(slot)">i</button>
                        <div class="rule-info-popover" *ngIf="isRuleInfoOpen(slot)">
                          <strong>Rules used</strong>
                          <ul><li *ngFor="let item of ruleInfoItems(slot)">{{ item }}</li></ul>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </section>
            </div>

            <ng-template #standardResults>
              <div class="panchang-table" *ngIf="isTableView(); else cardResults">
                <div class="table-period">
                  <span>{{ hinduPeriodLabel() }}</span>
                  <span>{{ englishPeriodLabel() }}</span>
                </div>
                <div class="table-scroll">
                  <table>
                    <thead>
                      <tr>
                        <th>Time</th>
                        <th>Tithi</th>
                        <th>Var</th>
                        <th>Nakshatra</th>
                        <th>Karana</th>
                        <th>Yoga</th>
                        <th>Chandra Rashi</th>
                        <th>Lagna</th>
                        <th>Category</th>
                        <th>Score</th>
                        <th>Rules</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr *ngFor="let slot of visibleSlots()">
                        <td><strong>{{ formatTime(slot.startTime) }}</strong><small>{{ formatDate(slot.startTime) }}</small></td>
                        <td><span class="table-chip" [class.highlight]="isHighlightedTithi(slot)">{{ timingName(slot, 'tithi') }}</span><small>{{ timingWindow(slot, 'tithi') }}</small></td>
                        <td>{{ ruleMeta(slot, 'tithi_suitability', 'vaar') }}</td>
                        <td><span class="table-value">{{ timingName(slot, 'nakshatra') }}</span><small>{{ timingWindow(slot, 'nakshatra') }}</small></td>
                        <td><span class="table-value">{{ timingName(slot, 'karana') }}</span><small>{{ timingWindow(slot, 'karana') }}</small></td>
                        <td><span class="table-value">{{ timingName(slot, 'yoga') }}</span><small>{{ timingWindow(slot, 'yoga') }}</small></td>
                        <td><span class="table-value">{{ timingName(slot, 'chandraRashi') }}</span><small>{{ timingWindow(slot, 'chandraRashi') }}</small></td>
                        <td><span class="table-value">{{ timingName(slot, 'lagna') }}</span><small>{{ timingWindow(slot, 'lagna') }}</small></td>
                        <td>{{ muhurthaCategory(slot) }}</td>
                        <td><span class="table-score" [ngClass]="scoreClass(slot)" [title]="scoreTooltip(slot)">{{ slot.score }}</span></td>
                        <td>
                          <div class="rule-info-wrap table-rule-info">
                            <button type="button" class="rule-info-btn" title="Rules used" (click)="toggleRuleInfo(slot)">i</button>
                            <div class="rule-info-popover" *ngIf="isRuleInfoOpen(slot)">
                              <strong>Rules used</strong>
                              <ul><li *ngFor="let item of ruleInfoItems(slot)">{{ item }}</li></ul>
                            </div>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>

              <ng-template #cardResults>
                <div class="slot-list">
                  <article class="slot-card" *ngFor="let slot of visibleSlots()">
                    <div class="slot-head">
                      <div class="slot-main"><code class="slot-id" [title]="slot.slotId">{{ shortSlotId(slot) }}</code><div class="date-line"><strong>{{ formatDate(slot.startTime) }}</strong><span class="time-pill">{{ formatTime(slot.startTime) }} - {{ formatTime(slot.endTime) }}</span></div><span class="hindu-date">{{ hinduDateLabel(slot) }}</span></div>
                      <div class="card-actions">
                        <div class="score" [ngClass]="scoreClass(slot)" [title]="scoreTooltip(slot)">{{ slot.score }}<small>{{ verdictLabel(slot.verdict) }}</small></div>
                        <div class="rule-info-wrap">
                          <button type="button" class="rule-info-btn" title="Rules used" (click)="toggleRuleInfo(slot)">i</button>
                          <div class="rule-info-popover" *ngIf="isRuleInfoOpen(slot)">
                            <strong>Rules used</strong>
                            <ul><li *ngFor="let item of ruleInfoItems(slot)">{{ item }}</li></ul>
                          </div>
                        </div>
                      </div>
                    </div>

                    <div class="notes" *ngIf="slot.strengths.length || slot.warnings.length">
                      <div *ngIf="slot.strengths.length"><b>Strengths</b><p [class.tithi-note-highlight]="isHighlightedTithi(slot)">{{ slot.strengths.slice(0, 3).join('; ') }}</p></div>
                      <div *ngIf="slot.warnings.length"><b>Warnings</b><p>{{ slot.warnings.slice(0, 3).join('; ') }}</p></div>
                    </div>
                  </article>
                </div>
              </ng-template>
            </ng-template>
          </ng-container>
          <ng-template #emptyResults><div class="state">No slots match the current UI filters.</div></ng-template>
        </ng-container>
      </app-section-card>
    </div>
  `,
  styles: [`
    .notes-page-action { display: inline-flex; align-items: center; justify-content: center; gap: 0.35rem; min-height: 2.25rem; padding: 0.45rem 0.75rem; border: 1px solid var(--theme-line); border-radius: var(--theme-radius-control); background: var(--theme-surface); color: var(--theme-text); font: inherit; font-size: 0.82rem; font-weight: 850; cursor: pointer; }
    .notes-page-action svg { width: 1rem; height: 1rem; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; stroke-linejoin: round; }
    .muhurtha-shell { display: grid; gap: 1rem; align-items: start; }
    .muhurtha-form { display: grid; gap: 0.8rem; }
    .input-strip, .personal-fields { display: grid; grid-template-columns: repeat(5, minmax(135px, 1fr)); gap: 0.75rem; align-items: end; }
    .criteria-toolbar { display: flex; justify-content: space-between; gap: 0.75rem; align-items: center; flex-wrap: wrap; padding: 0.7rem; border: 1px solid #ece5f2; border-radius: 0.5rem; background: #fbf8fd; }
    .toolbar-filters { display: flex; flex-wrap: wrap; gap: 0.5rem; align-items: center; }
    .personal-fields { padding-top: 0.8rem; border-top: 1px solid #eeeeee; }
    label { display: grid; gap: 0.4rem; color: #424242; font-size: 0.85rem; font-weight: 560; }
    input, select { width: 100%; border: 1px solid #d8d8d8; border-radius: 0.45rem; padding: 0.55rem 0.65rem; color: #212121; background: #fff; font: inherit; }
    input:focus, select:focus { outline: none; border-color: #7e3ba8; box-shadow: 0 0 0 3px rgba(126, 59, 168, 0.12); }
    .location-field { position: relative; }
    .location-dropdown { position: absolute; z-index: 20; top: calc(100% + 0.25rem); left: 0; right: 0; max-height: 220px; overflow: auto; border: 1px solid #ded7e5; border-radius: 0.5rem; background: #fff; box-shadow: 0 12px 28px rgba(32, 24, 40, 0.14); }
    .location-dropdown.compact { max-height: 190px; }
    .location-item { width: 100%; border: 0; border-bottom: 1px solid #f0edf4; border-radius: 0; padding: 0.65rem 0.75rem; background: #fff; color: #333; text-align: left; font-weight: 560; line-height: 1.35; }
    .location-item:hover, .location-item:focus { background: #f7f0fb; color: #5a2d78; }
    .check-row { display: flex; align-items: center; gap: 0.45rem; padding: 0.5rem 0.6rem; border: 1px solid #ece5f2; border-radius: 0.45rem; background: #fff; white-space: nowrap; }
    .check-row input { width: auto; }
    .check-row.strong { border-color: #d8c3e8; background: #f7f0fb; }
    .range-buttons, .view-toggle { display: inline-flex; align-items: center; gap: 0.35rem; padding: 0.18rem; border: 1px solid #ded7e5; border-radius: 0.45rem; background: #fff; }
    .range-buttons button, .view-toggle button { padding: 0.38rem 0.55rem; border-radius: 0.35rem; color: #4d345e; background: transparent; font-size: 0.76rem; }
    .range-buttons button:hover, .view-toggle button:hover, .view-toggle button.active { color: #fff; background: #7e3ba8; }
    .actions { display: flex; align-items: center; gap: 0.65rem; flex-wrap: wrap; margin-left: auto; }
    button { border: none; border-radius: 0.45rem; padding: 0.58rem 0.95rem; color: #fff; background: #7e3ba8; cursor: pointer; font-weight: 600; }
    button:disabled { cursor: not-allowed; opacity: 0.6; }
    .hint, .state, .unsupported { color: #757575; font-size: 0.9rem; }
    .state { padding: 1.25rem; border: 1px dashed #dedede; border-radius: 0.5rem; background: #fafafa; }
    .state.error { color: #b3261e; border-color: #f2b8b5; background: #fff8f7; }
    .response-toolbar { display: flex; justify-content: space-between; gap: 1rem; align-items: start; flex-wrap: wrap; margin-bottom: 1rem; }
    .response-summary, .facts { display: flex; flex-wrap: wrap; gap: 0.5rem; margin-bottom: 1rem; }
    .response-toolbar .response-summary { margin-bottom: 0; }
    .result-controls { display: flex; align-items: end; gap: 0.75rem; flex-wrap: wrap; }
    .quality-filter { display: flex; align-items: center; gap: 0.35rem; flex-wrap: wrap; }
    .quality-pill { display: inline-flex; align-items: center; gap: 0.3rem; border: 1px solid #ded7e5; border-radius: 999px; padding: 0.42rem 0.58rem; color: #424242; background: #fff; font-size: 0.76rem; font-weight: 560; }
    .quality-pill i { width: 0.55rem; height: 0.55rem; border-radius: 999px; background: #8a8a8a; }
    .quality-pill.favourable i { background: #1b5e20; }
    .quality-pill.acceptable i { background: #f0b429; }
    .quality-pill.weak i { background: #ef6c00; }
    .quality-pill.active { border-color: #7e3ba8; box-shadow: 0 0 0 2px rgba(126, 59, 168, 0.12); }
    .sort-field { width: 138px; min-width: 138px; gap: 0.25rem; font-size: 0.78rem; }
    .sort-field select { padding: 0.45rem 0.55rem; border-radius: 0.4rem; color: #212121; background: #fff; }
    .sort-field option { color: #212121; background: #fff; }
    .response-summary span, .facts span { padding: 0.35rem 0.6rem; border-radius: 999px; background: #f0edf4; color: #4d345e; font-size: 0.78rem; font-weight: 560; }
    .response-summary .year-context { background: #fff8e1; color: #6f4e00; }
    .unsupported { margin-bottom: 1rem; padding: 0.75rem; border-radius: 0.5rem; background: #fff8e1; color: #6f4e00; }
    .card-actions, .slot-rule-actions { display: inline-flex; align-items: flex-start; gap: 0.35rem; justify-self: end; }
    .rule-info-wrap { position: relative; display: inline-flex; align-items: center; }
    .rule-info-btn { display: inline-flex; align-items: center; justify-content: center; width: 1.35rem; height: 1.35rem; min-width: 1.35rem; padding: 0; border: 1px solid #d8c3e8; border-radius: 999px; background: #fff; color: #5a2d78; font-size: 0.72rem; font-weight: 900; line-height: 1; }
    .rule-info-btn:hover, .rule-info-btn:focus { background: #f7f0fb; color: #3d155b; box-shadow: 0 0 0 2px rgba(126, 59, 168, 0.12); }
    .rule-info-popover { position: absolute; z-index: 30; top: calc(100% + 0.35rem); right: 0; width: min(260px, 78vw); padding: 0.55rem 0.65rem; border: 1px solid #ded7e5; border-radius: 0.45rem; background: #fff; color: #333; box-shadow: 0 14px 32px rgba(32, 24, 40, 0.18); text-align: left; }
    .rule-info-popover strong { display: block; margin-bottom: 0.35rem; color: #4d345e; font-size: 0.74rem; }
    .rule-info-popover ul { display: grid; gap: 0.24rem; margin: 0; padding-left: 1rem; }
    .rule-info-popover li { color: #424242; font-size: 0.72rem; line-height: 1.3; }
    .table-rule-info .rule-info-popover { left: auto; right: 0; }
    .panchang-view { display: grid; gap: 0.85rem; }
    .panchang-range-strip { display: flex; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap; padding: 0.65rem 0.8rem; border: 1px solid #e7d3a1; border-radius: 0.35rem; background: #fffaf0; color: #6f3b16; font-size: 0.82rem; font-weight: 560; }
    .panchang-day { border: 1px solid #e2c98d; border-radius: 0.35rem; background: #fffdf8; box-shadow: inset 0 2px 0 #9b2335; overflow: hidden; }
    .panchang-day-head { display: flex; justify-content: space-between; gap: 0.75rem; align-items: start; flex-wrap: wrap; padding: 0.7rem 0.8rem; border-bottom: 3px double #e2c98d; background: #fff7e3; color: #3d2b1f; }
    .panchang-day-head div:first-child { display: grid; gap: 0.18rem; }
    .panchang-day-head strong { color: #8b1e2d; font-size: 0.95rem; }
    .panchang-day-head span { color: #6f3b16; font-size: 0.8rem; font-weight: 560; }
    .panchang-day-meta { display: inline-flex; gap: 0.4rem; flex-wrap: wrap; }
    .panchang-day-meta span { padding: 0.22rem 0.45rem; border: 1px solid #e7d3a1; border-radius: 999px; background: #fffdf8; }
    .panchang-facts { display: grid; grid-template-columns: repeat(5, minmax(120px, 1fr)); border-bottom: 1px solid #eadbad; }
    .panchang-fact { display: grid; gap: 0.12rem; padding: 0.55rem 0.65rem; border-right: 1px solid #eadbad; color: #3d2b1f; min-width: 0; }
    .panchang-fact:last-child { border-right: 0; }
    .panchang-fact span { color: #8b1e2d; font-size: 0.68rem; font-weight: 560; text-transform: uppercase; }
    .panchang-fact strong { font-size: 0.82rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .panchang-fact small { color: #7b6b58; font-size: 0.68rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .panchang-fact.highlight { background: #fff3c4; }
    .panchang-slots { display: grid; }
    .panchang-slot-row { display: grid; grid-template-columns: minmax(190px, 0.9fr) minmax(260px, 1.5fr) auto; gap: 0.55rem; align-items: center; padding: 0.38rem 0.6rem; border-bottom: 1px solid #f0e5c2; }
    .panchang-slot-row:last-child { border-bottom: 0; }
    .panchang-slot-time { display: flex; align-items: center; gap: 0.45rem; min-width: 0; }
    .panchang-slot-main { display: flex; align-items: baseline; gap: 0.55rem; min-width: 0; }
    .panchang-slot-time code { flex: 0 0 auto; width: fit-content; padding: 0.1rem 0.32rem; border-radius: 0.25rem; background: #f6f1fa; color: #5a2d78; font-size: 0.64rem; font-weight: 560; }
    .panchang-slot-time strong, .panchang-slot-main span { color: #2f2a24; font-size: 0.8rem; font-weight: 560; white-space: nowrap; }
    .panchang-slot-main small { color: #6f6256; font-size: 0.7rem; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .panchang-slot-main small b { color: #8b1e2d; font-weight: 560; }
    .panchang-slot-score { justify-self: end; display: inline-flex; align-items: center; white-space: nowrap; border-radius: 999px; padding: 0.22rem 0.46rem; font-size: 0.68rem; font-weight: 700; border: 1px solid transparent; }
    .panchang-slot-score.excellent { background: #e8f5e9; color: #1b5e20; border-color: #a5d6a7; }
    .panchang-slot-score.good { background: #e0f2f1; color: #00695c; border-color: #80cbc4; }
    .panchang-slot-score.acceptable { background: #fff8e1; color: #6f4e00; border-color: #f0cf70; }
    .panchang-slot-score.weak { background: #fff3e0; color: #a04b00; border-color: #ffb74d; }
    .panchang-slot-score.not-recommended, .panchang-slot-score.blocked { background: #ffebee; color: #b3261e; border-color: #ef9a9a; }
    .panchang-table { border: 1px solid #e7e0ec; border-radius: 0.5rem; overflow: hidden; background: #fff; }
    .table-period { display: flex; justify-content: space-between; gap: 0.75rem; flex-wrap: wrap; padding: 0.65rem 0.8rem; background: #f7f0fb; color: #4d345e; font-size: 0.8rem; font-weight: 560; }
    .table-scroll { overflow-x: auto; }
    table { width: 100%; border-collapse: collapse; min-width: 980px; }
    th, td { padding: 0.55rem 0.65rem; border-bottom: 1px solid #eee8f4; text-align: left; vertical-align: top; font-size: 0.8rem; }
    th { background: #fbf8fd; color: #4d345e; font-size: 0.72rem; text-transform: uppercase; }
    td strong, td small, .table-value { display: block; white-space: nowrap; }
    td small { margin-top: 0.16rem; color: #757575; font-size: 0.7rem; }
    .table-chip, .table-score { display: inline-flex; align-items: center; border-radius: 999px; padding: 0.25rem 0.45rem; font-weight: 600; }
    .table-chip { background: #eef5f8; color: #20566b; }
    .table-chip.highlight { background: #fff3c4; color: #6f4e00; border: 1px solid #f0cf70; }
    .table-score.excellent { background: #e8f5e9; color: #1b5e20; }
    .table-score.good { background: #e0f2f1; color: #00695c; }
    .table-score.acceptable { background: #fff8e1; color: #6f4e00; }
    .table-score.weak { background: #fff3e0; color: #a04b00; }
    .table-score.not-recommended, .table-score.blocked { background: #ffebee; color: #b3261e; }
    .slot-list { display: grid; grid-template-columns: repeat(auto-fit, minmax(340px, 1fr)); gap: 0.8rem; }
    .slot-card { border: 1px solid #e7e0ec; border-radius: 0.5rem; padding: 0.85rem; background: #fff; }
    .slot-head { display: flex; justify-content: space-between; gap: 0.75rem; margin-bottom: 0.65rem; align-items: flex-start; }
    .slot-main { display: grid; gap: 0.22rem; min-width: 0; }
    .date-line { display: flex; align-items: center; gap: 0.5rem; flex-wrap: wrap; }
    .slot-head span { color: #757575; font-size: 0.82rem; }
    .slot-head .hindu-date { color: #5a2d78; font-weight: 560; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 100%; }
    .slot-id { width: fit-content; padding: 0.14rem 0.38rem; border-radius: 0.3rem; background: #f6f1fa; color: #5a2d78; font-size: 0.68rem; font-weight: 560; }
    .time-pill { display: inline-flex; align-items: center; width: fit-content; padding: 0.16rem 0.45rem; border-radius: 999px; background: #f0edf4; color: #4d345e !important; font-size: 0.74rem !important; font-weight: 560; }
    .score { min-width: 58px; text-align: center; border-radius: 0.45rem; padding: 0.28rem 0.35rem; font-size: 1rem; font-weight: 700; border: 1px solid transparent; }
    .score.excellent { background: #e8f5e9; color: #1b5e20; border-color: #a5d6a7; }
    .score.good { background: #e0f2f1; color: #00695c; border-color: #80cbc4; }
    .score.acceptable { background: #fff8e1; color: #6f4e00; border-color: #f0cf70; }
    .score.weak { background: #fff3e0; color: #a04b00; border-color: #ffb74d; }
    .score.not-recommended { background: #ffebee; color: #b3261e; border-color: #ef9a9a; }
    .score.blocked { background: #fde7e9; color: #8c1d18; border-color: #d93025; }
    .score small { display: block; font-size: 0.58rem; line-height: 1; margin-top: 0.08rem; }
    .notes { display: grid; gap: 0.75rem; color: #424242; font-size: 0.85rem; }
    .notes p { margin: 0.25rem 0 0; color: #616161; line-height: 1.45; }
    .notes p.tithi-note-highlight { width: fit-content; padding: 0.25rem 0.45rem; border: 1px solid #f0cf70; border-radius: 0.35rem; background: #fff8df; color: #6f4e00; font-weight: 560; }
    :host { display: block; color: var(--theme-text); }
    :host ::ng-deep app-page-header .page-header,
    :host ::ng-deep app-section-card > .card { background: var(--theme-surface); border-color: var(--theme-line); color: var(--theme-text); box-shadow: 0 18px 44px var(--theme-shadow); }
    :host ::ng-deep app-section-card .card-content { color: var(--theme-text); }
    .criteria-toolbar, .personal-fields, .state, .unsupported, .panchang-table, .slot-card, .panchang-day, .panchang-range-strip, .table-period { border-color: var(--theme-line); background: var(--theme-surface-soft, var(--theme-surface)); color: var(--theme-text); }
    label, .notes, .panchang-fact, .panchang-day-head, td, .table-value, .slot-main strong { color: var(--theme-text); }
    label span, .hint, .state, .unsupported, td small, .notes p, .slot-head span, .panchang-fact small, .panchang-slot-main small, .response-summary span, .facts span { color: var(--theme-text-muted); }
    input, select, .sort-field select { border-color: var(--theme-line); background: var(--theme-input-bg); color: var(--theme-text); }
    input::placeholder { color: var(--theme-text-muted); }
    input:focus, select:focus { border-color: var(--theme-accent); box-shadow: 0 0 0 3px color-mix(in srgb, var(--theme-accent) 18%, transparent); }
    select option, .sort-field option { background: var(--theme-surface); color: var(--theme-text); }
    .location-dropdown { border-color: var(--theme-line); background: var(--theme-surface); box-shadow: 0 16px 34px var(--theme-shadow); }
    .location-item { border-bottom-color: var(--theme-line-soft); background: var(--theme-surface); color: var(--theme-text); }
    .location-item:hover, .location-item:focus { background: color-mix(in srgb, var(--theme-accent) 12%, var(--theme-surface)); color: var(--theme-accent); }
    .check-row, .range-buttons, .view-toggle, .quality-pill { border-color: var(--theme-line); background: var(--theme-input-bg); color: var(--theme-text); }
    .check-row.strong, .quality-pill.active, .view-toggle button.active { border-color: color-mix(in srgb, var(--theme-accent) 62%, var(--theme-line)); background: color-mix(in srgb, var(--theme-accent) 14%, var(--theme-input-bg)); color: var(--theme-accent); }
    button { background: var(--theme-accent); color: var(--theme-bg); }
    .range-buttons button, .view-toggle button { color: var(--theme-text-muted); background: transparent; }
    .range-buttons button:hover, .view-toggle button:hover, .view-toggle button.active { color: var(--theme-bg); background: var(--theme-accent); }
    .response-summary span, .facts span, .time-pill, .slot-id, .panchang-slot-time code, .table-chip { background: color-mix(in srgb, var(--theme-accent) 12%, var(--theme-input-bg)); color: var(--theme-accent); }
    .response-summary .year-context, .unsupported, .panchang-range-strip, .panchang-day-head, .panchang-day-meta span { background: color-mix(in srgb, var(--theme-accent) 10%, var(--theme-surface)); color: var(--theme-text); }
    .state { border-style: dashed; background: color-mix(in srgb, var(--theme-input-bg) 78%, transparent); }
    .state.error { color: var(--theme-danger); border-color: color-mix(in srgb, var(--theme-danger) 42%, var(--theme-line)); background: color-mix(in srgb, var(--theme-danger) 10%, var(--theme-surface)); }
    .panchang-day { box-shadow: inset 0 2px 0 var(--theme-accent); }
    .panchang-day-head { border-bottom-color: var(--theme-line); }
    .panchang-day-head strong, .panchang-day-head span, .panchang-fact span, .panchang-slot-main small b, .slot-head .hindu-date { color: var(--theme-accent); }
    .panchang-facts, .panchang-fact, .panchang-slot-row, .panchang-table, th, td, .slot-card { border-color: var(--theme-line); }
    th { background: color-mix(in srgb, var(--theme-input-bg) 84%, transparent); color: var(--theme-accent); }
    .panchang-slot-time strong, .panchang-slot-main span { color: var(--theme-text); }
    .panchang-fact.highlight, .table-chip.highlight, .notes p.tithi-note-highlight { background: color-mix(in srgb, var(--theme-accent) 18%, var(--theme-input-bg)); color: var(--theme-accent); border-color: color-mix(in srgb, var(--theme-accent) 42%, var(--theme-line)); }
    .score.excellent, .table-score.excellent, .panchang-slot-score.excellent { background: color-mix(in srgb, #35c46b 16%, var(--theme-input-bg)); color: #6ee7a0; border-color: color-mix(in srgb, #35c46b 44%, var(--theme-line)); }
    .score.good, .table-score.good, .panchang-slot-score.good { background: color-mix(in srgb, #2dd4bf 16%, var(--theme-input-bg)); color: #5eead4; border-color: color-mix(in srgb, #2dd4bf 44%, var(--theme-line)); }
    .score.acceptable, .table-score.acceptable, .panchang-slot-score.acceptable { background: color-mix(in srgb, var(--theme-accent) 18%, var(--theme-input-bg)); color: var(--theme-accent); border-color: color-mix(in srgb, var(--theme-accent) 44%, var(--theme-line)); }
    .score.weak, .table-score.weak, .panchang-slot-score.weak { background: color-mix(in srgb, #f59e0b 16%, var(--theme-input-bg)); color: #fbbf24; border-color: color-mix(in srgb, #f59e0b 44%, var(--theme-line)); }
    .score.not-recommended, .score.blocked, .table-score.not-recommended, .table-score.blocked, .panchang-slot-score.not-recommended, .panchang-slot-score.blocked { background: color-mix(in srgb, var(--theme-danger) 16%, var(--theme-input-bg)); color: var(--theme-danger); border-color: color-mix(in srgb, var(--theme-danger) 44%, var(--theme-line)); }
    @media (max-width: 760px) { .muhurtha-shell { padding: 0 12px 18px; } .slot-list { grid-template-columns: 1fr; } .response-toolbar { padding: 0.75rem; } }
    @media (max-width: 1200px) { .input-strip, .personal-fields { grid-template-columns: repeat(3, minmax(135px, 1fr)); } }
    @media (max-width: 900px) { .panchang-facts { grid-template-columns: repeat(2, minmax(120px, 1fr)); } .panchang-slot-row { grid-template-columns: 1fr; align-items: start; } .panchang-slot-score { justify-self: start; } }
    @media (max-width: 760px) { .input-strip, .personal-fields { grid-template-columns: 1fr; } .criteria-toolbar { align-items: stretch; } .toolbar-filters, .range-buttons, .actions, .result-controls { width: 100%; } .range-buttons { justify-content: space-between; } .sort-field { width: 138px; max-width: 100%; } }
  `],
})
export class MuhurthaComponent implements OnInit {
  isNotesOpen = false;

  openNotes(): void { this.isNotesOpen = true; }
  closeNotes(): void { this.isNotesOpen = false; }

  private fb = inject(FormBuilder);
  private muhurthaApi = inject(MuhurthaApiService);
  private locationService = inject(LocationService);
  private cdr = inject(ChangeDetectorRef);

  loading = signal(false);
  errorMessage = signal('');
  response = signal<MuhurthaResponse | null>(null);
  locationResults: any[] = [];
  birthLocationResults: any[] = [];
  timezoneResults: string[] = [];
  birthTimezoneResults: string[] = [];
  activeRuleSlotId = signal<string | null>(null);
  readonly timezoneOptions = this.buildTimezoneOptions();

  readonly eventTypes: Option[] = [
    { label: 'Marriage', value: 0 }, { label: 'Engagement', value: 1 }, { label: 'Gruha Pravesha', value: 2 },
    { label: 'Bhoomi Puja', value: 3 }, { label: 'Property Purchase', value: 4 }, { label: 'Business Opening', value: 5 },
    { label: 'Job Joining', value: 6 }, { label: 'Vehicle Purchase', value: 7 }, { label: 'Travel', value: 8 },
    { label: 'Education Start', value: 9 }, { label: 'Medical Procedure', value: 10 }, { label: 'Naming Ceremony', value: 11 },
    { label: 'Annaprashana', value: 12 }, { label: 'Upanayana', value: 13 }, { label: 'General Auspicious Work', value: 14 },
  ];

  readonly regionalProfiles: Option[] = [
    { label: 'General', value: 0 }, { label: 'North India', value: 1 }, { label: 'South India', value: 2 },
  ];

  readonly form = this.fb.group({
    eventType: [14, Validators.required], regionalProfile: [0, Validators.required],
    requestedStart: [this.toDateInput(new Date()), Validators.required], requestedEnd: [this.toDateInput(this.addDays(new Date(), 7)), Validators.required],
    dailyStartTime: ['08:00'], dailyEndTime: ['18:00'], locationName: ['Bangalore, Karnataka, India', Validators.required],
    latitude: [12.9716, Validators.required], longitude: [77.5946, Validators.required], timezone: ['Asia/Kolkata', Validators.required],
    avoidRahuKalam: [true], avoidBhadra: [true], preferGoodChoghadiya: [false], checkMuhurthaLagna: [true], checkPersonalHoroscope: [false],
    personName: [''], birthDate: [''], birthTime: [''], birthPlace: ['Bangalore, Karnataka, India'], birthLatitude: [12.9716], birthLongitude: [77.5946], birthTimezone: ['Asia/Kolkata'],
    resultSort: ['score'],
    resultQuality: ['all'],
    resultView: ['card'],
  });

  ngOnInit(): void {
    this.form.controls.locationName.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap((value) => this.searchLocations(value || '', false)),
    ).subscribe((results) => { this.locationResults = results; this.cdr.detectChanges(); });

    this.form.controls.birthPlace.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap((value) => this.searchLocations(value || '', true)),
    ).subscribe((results) => { this.birthLocationResults = results; this.cdr.detectChanges(); });

    this.form.controls.timezone.valueChanges.pipe(
      debounceTime(150),
      distinctUntilChanged(),
    ).subscribe((value) => { this.timezoneResults = this.searchTimezones(value || ''); this.cdr.detectChanges(); });

    this.form.controls.birthTimezone.valueChanges.pipe(
      debounceTime(150),
      distinctUntilChanged(),
    ).subscribe((value) => { this.birthTimezoneResults = this.searchTimezones(value || ''); this.cdr.detectChanges(); });
  }

  selectLocation(place: any): void {
    this.form.patchValue({
      locationName: place.displayName,
      latitude: Number(place.latitude),
      longitude: Number(place.longitude),
      timezone: this.resolveTimezone(place, this.form.controls.timezone.value),
    }, { emitEvent: false });

    this.locationResults = [];
  }

  selectTimezone(timezone: string): void {
    this.form.patchValue({ timezone }, { emitEvent: false });
    this.timezoneResults = [];
  }

  selectBirthTimezone(timezone: string): void {
    this.form.patchValue({ birthTimezone: timezone }, { emitEvent: false });
    this.birthTimezoneResults = [];
  }
  showTimezoneOptions(birthTimezone: boolean): void {
    if (birthTimezone) {
      this.birthTimezoneResults = this.searchTimezones(this.form.controls.birthTimezone.value || '');
      return;
    }

    this.timezoneResults = this.searchTimezones(this.form.controls.timezone.value || '');
  }
  selectBirthLocation(place: any): void {
    this.form.patchValue({
      birthPlace: place.displayName,
      birthLatitude: Number(place.latitude),
      birthLongitude: Number(place.longitude),
      birthTimezone: this.resolveTimezone(place, this.form.controls.birthTimezone.value || this.form.controls.timezone.value),
    }, { emitEvent: false });

    this.birthLocationResults = [];
  }
  isPersonalized(): boolean { return !!this.form.controls.checkPersonalHoroscope.value; }

  findMuhurtha(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    if (this.isPersonalized() && !this.hasCompletePersonalDetails()) {
      this.errorMessage.set('Personalized recommendation needs birth date, birth time, birth place, latitude, and longitude.');
      return;
    }
    this.loading.set(true); this.errorMessage.set(''); this.response.set(null);
    this.muhurthaApi.find(this.buildRequest()).subscribe({
      next: (response) => { this.response.set(response); this.loading.set(false); },
      error: (error) => { this.errorMessage.set(error?.message || 'Unable to find Muhurtha recommendations.'); this.loading.set(false); },
    });
  }

  visibleSlots(): MuhurthaRecommendation[] {
    const slots = this.filteredSlots();
    const filters = this.form.getRawValue();
    return this.sortSlots(slots, filters.resultSort || 'score');
  }

  hasResultSlots(): boolean {
    return (this.response()?.recommendedSlots.length ?? 0) > 0;
  }

  panchangDayGroups(): PanchangDayGroup[] {
    const orderedSlots = this.sortSlots(this.filteredSlots(), 'time');
    const groups = new Map<string, MuhurthaRecommendation[]>();

    orderedSlots.forEach((slot) => {
      const key = this.dayKey(slot.startTime);
      const daySlots = groups.get(key) ?? [];
      daySlots.push(slot);
      groups.set(key, daySlots);
    });

    return Array.from(groups.entries()).map(([key, slots]) => {
      const firstSlot = slots[0];
      return {
        key,
        dateLabel: this.formatDayDate(firstSlot.startTime),
        hinduLabel: this.hinduDateLabel(firstSlot),
        slots,
        bestScore: Math.max(...slots.map((slot) => slot.score)),
        firstSlot,
      };
    });
  }

  panchangDayFacts(group: PanchangDayGroup): PanchangDayFact[] {
    const slot = group.firstSlot;
    return [
      { label: 'Tithi', value: this.timingName(slot, 'tithi'), window: this.timingWindow(slot, 'tithi'), highlight: this.isHighlightedTithi(slot) },
      { label: 'Nakshatra', value: this.timingName(slot, 'nakshatra'), window: this.timingWindow(slot, 'nakshatra') },
      { label: 'Yoga', value: this.timingName(slot, 'yoga'), window: this.timingWindow(slot, 'yoga') },
      { label: 'Karana', value: this.timingName(slot, 'karana'), window: this.timingWindow(slot, 'karana') },
      { label: 'Chandra', value: this.timingName(slot, 'chandraRashi'), window: this.timingWindow(slot, 'chandraRashi') },
    ];
  }

  toggleRuleInfo(slot: MuhurthaRecommendation): void {
    const slotId = slot.slotId || slot.startTime;
    this.activeRuleSlotId.set(this.activeRuleSlotId() === slotId ? null : slotId);
  }

  isRuleInfoOpen(slot: MuhurthaRecommendation): boolean {
    return this.activeRuleSlotId() === (slot.slotId || slot.startTime);
  }

  ruleInfoItems(slot: MuhurthaRecommendation): string[] {
    const items = (slot.ruleResults || [])
      .filter((rule) => String(rule.status).toLowerCase() !== 'skipped')
      .sort((left, right) => Math.abs(right.scoreImpact || 0) - Math.abs(left.scoreImpact || 0))
      .slice(0, 6)
      .map((rule) => this.ruleInfoText(rule));

    return items.length ? items : ['Base Muhurtha score applied'];
  }

  private ruleInfoText(rule: MuhurthaRuleResult): string {
    const name = this.formatRuleName(rule.ruleKey || 'Rule');
    const value = rule.metadata?.['value'] || rule.metadata?.['moonSign'] || '';
    const score = rule.scoreImpact ? ` (${this.signedScore(rule.scoreImpact)})` : '';
    if (value) {
      return `${name}: ${value}${score}`;
    }

    return `${name}${score}`;
  }
  setQualityFilter(value: string): void {
    this.form.patchValue({ resultQuality: value });
  }

  isQualityFilter(value: string): boolean {
    return this.form.controls.resultQuality.value === value;
  }

  setResultView(value: 'card' | 'table' | 'panchang'): void {
    this.form.patchValue({ resultView: value });
  }

  isCardView(): boolean {
    return this.form.controls.resultView.value === 'card';
  }

  isTableView(): boolean {
    return this.form.controls.resultView.value === 'table';
  }

  isPanchangView(): boolean {
    return this.form.controls.resultView.value === 'panchang';
  }
  setQuickRange(days: number): void {
    const start = new Date();
    const end = this.addDays(start, days);
    this.form.patchValue({
      requestedStart: this.toDateInput(start),
      requestedEnd: this.toDateInput(end),
    });

    if (this.response() && this.form.valid) {
      this.findMuhurtha();
    }
  }

  ruleMeta(slot: MuhurthaRecommendation, ruleKey: string, metadataKey: string): string {
    return slot.ruleResults?.find((rule) => rule.ruleKey?.toLowerCase() === ruleKey)?.metadata?.[metadataKey] || '-';
  }

  ruleValue(slot: MuhurthaRecommendation, ruleKey: string): string {
    return this.ruleMeta(slot, ruleKey, 'value');
  }

  timingName(slot: MuhurthaRecommendation, key: keyof MuhurthaRecommendation['panchangTimings']): string {
    return slot.panchangTimings?.[key]?.name || '-';
  }

  timingWindow(slot: MuhurthaRecommendation, key: keyof MuhurthaRecommendation['panchangTimings']): string {
    const detail = slot.panchangTimings?.[key];
    if (!detail?.startTime || !detail?.endTime) {
      return '-';
    }

    return this.formatTime(detail.startTime) + ' - ' + this.formatTime(detail.endTime);
  }

  muhurthaCategory(slot: MuhurthaRecommendation): string {
    const timeRule = slot.ruleResults?.find((rule) => {
      const key = rule.ruleKey?.toLowerCase();
      const passed = String(rule.status).toLowerCase() === 'passed';
      return passed && (key === 'abhijit_muhurtha' || key === 'choghadiya');
    });

    return timeRule ? this.formatRuleName(timeRule.ruleKey) : this.verdictLabel(slot.verdict);
  }

  hinduPeriodLabel(): string {
    const slots = this.visibleSlots();
    const months = this.uniqueMetadataValues(slots, 'maas');
    const yearLabel = this.hinduYearLabel();
    const monthLabel = this.formatRangeValues(months);
    const parts = [yearLabel, monthLabel].filter((value) => !!value);

    return parts.length ? parts.join(' | ') : 'Hindu month details unavailable';
  }

  hinduYearLabel(): string {
    const slots = this.visibleSlots();
    const shake = this.formatYearRange(this.uniqueMetadataValues(slots, 'shake'));
    const samvat = this.formatYearRange(this.uniqueMetadataValues(slots, 'samvat'));
    const parts = [
      shake ? `Shake ${shake}` : '',
      samvat ? `Samvat ${samvat}` : '',
    ].filter((value) => !!value);

    return parts.join(' | ');
  }

  private uniqueMetadataValues(slots: MuhurthaRecommendation[], key: string): string[] {
    return Array.from(new Set(slots
      .map((slot) => this.tithiMetadata(slot)?.[key])
      .filter((value): value is string => !!value)))
      .sort((left, right) => left.localeCompare(right, undefined, { numeric: true }));
  }

  private formatYearRange(values: string[]): string {
    if (!values.length) {
      return '';
    }

    if (values.length === 1) {
      return values[0];
    }

    return `${values[0]}-${values[values.length - 1]}`;
  }

  private formatRangeValues(values: string[]): string {
    if (!values.length) {
      return '';
    }

    if (values.length <= 2) {
      return values.join(' - ');
    }

    return `${values[0]} - ${values[values.length - 1]}`;
  }
  englishPeriodLabel(): string {
    const value = this.form.getRawValue();
    return `${this.formatDateOnly(value.requestedStart || '')} to ${this.formatDateOnly(value.requestedEnd || '')}`;
  }

  private filteredSlots(): MuhurthaRecommendation[] {
    const result = this.response();
    if (!result) {
      return [];
    }

    const filters = this.form.getRawValue();
    return result.recommendedSlots.filter((slot) => {
      if (filters.avoidRahuKalam && this.hasRule(slot, 'rahu_kalam')) {
        return false;
      }

      if (filters.avoidBhadra && this.hasRule(slot, 'bhadra_vishti')) {
        return false;
      }

      if (filters.preferGoodChoghadiya && !this.hasPositiveRule(slot, 'choghadiya')) {
        return false;
      }

      return this.matchesQualityFilter(slot, filters.resultQuality || 'all');
    });
  }

  private matchesQualityFilter(slot: MuhurthaRecommendation, filter: string): boolean {
    if (filter === 'all') {
      return true;
    }

    const quality = this.scoreQuality(slot);
    if (filter === 'favourable') {
      return quality === 'excellent' || quality === 'good';
    }

    return quality === filter;
  }

  private scoreQuality(slot: MuhurthaRecommendation): string {
    if (slot.hardRejected) {
      return 'blocked';
    }

    if (slot.score >= 85) {
      return 'excellent';
    }

    if (slot.score >= 70) {
      return 'good';
    }

    if (slot.score >= 55) {
      return 'acceptable';
    }

    if (slot.score >= 40) {
      return 'weak';
    }

    return 'not-recommended';
  }
  private sortSlots(slots: MuhurthaRecommendation[], sortMode: string): MuhurthaRecommendation[] {
    const ordered = [...slots];
    if (sortMode === 'score') {
      return ordered.sort((left, right) => {
        if (left.hardRejected !== right.hardRejected) {
          return Number(left.hardRejected) - Number(right.hardRejected);
        }

        if (right.score !== left.score) {
          return right.score - left.score;
        }

        return new Date(left.startTime).getTime() - new Date(right.startTime).getTime();
      });
    }

    return ordered.sort((left, right) => new Date(left.startTime).getTime() - new Date(right.startTime).getTime());
  }
  private buildTimezoneOptions(): string[] {
    const supportedValuesOf = (Intl as unknown as { supportedValuesOf?: (key: string) => string[] }).supportedValuesOf;
    const browserTimezones = supportedValuesOf?.('timeZone') ?? [];
    const preferredTimezones = [
      'Asia/Kolkata',
      'Asia/Kathmandu',
      'Asia/Dhaka',
      'Asia/Dubai',
      'Asia/Singapore',
      'Europe/London',
      'America/New_York',
      'America/Los_Angeles',
      'Australia/Sydney',
    ];
    const remaining = browserTimezones
      .filter((timezone) => !preferredTimezones.includes(timezone))
      .sort((left, right) => left.localeCompare(right));

    return [...preferredTimezones, ...remaining];
  }

  private resolveTimezone(place: any, fallbackTimezone?: string | null): string {
    const countryCode = String(place?.countryCode || '').toLowerCase();
    if (countryCode === 'in') {
      return 'Asia/Kolkata';
    }

    return fallbackTimezone || Intl.DateTimeFormat().resolvedOptions().timeZone || 'Asia/Kolkata';
  }
  private searchTimezones(value: string): string[] {
    const query = value.trim().toLowerCase();
    if (!query) {
      return this.timezoneOptions.slice(0, 9);
    }

    return this.timezoneOptions
      .filter((timezone) => timezone.toLowerCase().includes(query))
      .slice(0, 12);
  }
  private searchLocations(value: string, birthSearch: boolean) {
    const query = value.trim();
    if (query.length < 3) {
      if (birthSearch) {
        this.birthLocationResults = [];
      } else {
        this.locationResults = [];
      }

      return of([]);
    }

    return this.locationService.searchLocation(query);
  }
  private hasRule(slot: MuhurthaRecommendation, ruleKey: string): boolean {
    return slot.ruleResults?.some((rule) => rule.ruleKey?.toLowerCase() === ruleKey) ?? false;
  }

  private hasPositiveRule(slot: MuhurthaRecommendation, ruleKey: string): boolean {
    return slot.ruleResults?.some((rule) =>
      rule.ruleKey?.toLowerCase() === ruleKey
      && String(rule.status).toLowerCase() === 'passed'
      && String(rule.severity).toLowerCase() === 'positive') ?? false;
  }

  hinduDateLabel(slot: MuhurthaRecommendation): string {
    const metadata = this.tithiMetadata(slot);
    const parts = [
      metadata?.['vaar'],
      metadata?.['maas'],
      metadata?.['paksha'],
      this.tithiName(slot),
    ].filter((value) => !!value);

    return parts.join(' | ');
  }
  private tithiMetadata(slot: MuhurthaRecommendation): Record<string, string> | undefined {
    return slot.ruleResults?.find((item) => item.ruleKey?.toLowerCase() === 'tithi_suitability')?.metadata;
  }
  tithiName(slot: MuhurthaRecommendation): string {
    const rule = slot.ruleResults?.find((item) => item.ruleKey?.toLowerCase() === 'tithi_suitability');
    const metadataValue = rule?.metadata?.['value'];
    if (metadataValue) {
      return metadataValue;
    }

    return rule?.summary?.replace(/^Tithi is\s+/i, '').replace(/\.$/, '') || 'Unknown';
  }

  isHighlightedTithi(slot: MuhurthaRecommendation): boolean {
    const tithi = this.tithiName(slot).toLowerCase();
    return tithi === 'ekadashi' || tithi === 'purnima';
  }
  scoreClass(slot: MuhurthaRecommendation): string {
    return this.scoreQuality(slot);
  }
  scoreTooltip(slot: MuhurthaRecommendation): string {
    return [
      'Base 50',
      'Generic ' + this.signedScore(slot.scoreBreakdown.genericTimeRulesScore),
      'Panchanga ' + this.signedScore(slot.scoreBreakdown.panchangaRulesScore),
      'Personal ' + this.signedScore(slot.scoreBreakdown.personalHoroscopeRulesScore),
      'Lagna ' + this.signedScore(slot.scoreBreakdown.muhurthaChartRulesScore),
    ].join(' | ');
  }

  signedScore(value: number): string {
    return value > 0 ? `+${value}` : `${value}`;
  }
  private formatRuleName(value: string): string {
    return value.replace(/_/g, ' ').replace(/\b\w/g, (letter) => letter.toUpperCase());
  }

  private formatDateOnly(value: string): string {
    return value ? new Date(`${value}T00:00:00`).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' }) : '-';
  }

  verdictLabel(verdict: number | string): string {
    if (typeof verdict === 'string') {
      return verdict.replace(/([a-z])([A-Z])/g, '$1 $2');
    }

    return ['Excellent', 'Highly Favourable', 'Acceptable', 'Weak', 'Not Recommended', 'Blocked'][verdict] || 'Unknown';
  }
  hasCompletePersonalDetails(): boolean {
    const value = this.form.getRawValue();
    return !!value.birthDate
      && !!value.birthTime
      && !!value.birthPlace
      && value.birthLatitude !== null
      && value.birthLatitude !== undefined
      && value.birthLongitude !== null
      && value.birthLongitude !== undefined;
  }

  shortSlotId(slot: MuhurthaRecommendation): string { return '#' + new Date(slot.startTime).toLocaleDateString(undefined, { month: '2-digit', day: '2-digit' }).replace(/\D/g, '') + '-' + new Date(slot.startTime).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit', hour12: false }).replace(/\D/g, ''); }
  formatDate(value: string): string { return new Date(value).toLocaleDateString(undefined, { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' }); }
  formatDayDate(value: string): string { return new Date(value).toLocaleDateString(undefined, { weekday: 'long', day: 'numeric', month: 'short', year: 'numeric' }); }
  formatTime(value: string): string { return new Date(value).toLocaleTimeString(undefined, { hour: '2-digit', minute: '2-digit' }); }

  private buildRequest(): FindMuhurthaRequest {
    const value = this.form.getRawValue();
    const personalized = !!value.checkPersonalHoroscope;
    const birthDateTime = value.birthDate && value.birthTime ? `${value.birthDate}T${value.birthTime}:00` : null;
    return {
      eventType: Number(value.eventType), regionalProfile: Number(value.regionalProfile),
      location: { name: value.locationName || '', latitude: Number(value.latitude), longitude: Number(value.longitude), timezone: value.timezone || '' },
      requestedStart: this.toLocalIso(value.requestedStart || '', value.dailyStartTime || '00:00'),
      requestedEnd: this.toLocalIso(value.requestedEnd || '', value.dailyEndTime || '23:59'),
      dailyStartTime: value.dailyStartTime ? `${value.dailyStartTime}:00` : null,
      dailyEndTime: value.dailyEndTime ? `${value.dailyEndTime}:00` : null,
      horoscope: personalized && birthDateTime ? {
        name: value.personName || null,
        birthDateTime,
        birthPlace: value.birthPlace || null,
        latitude: Number(value.birthLatitude),
        longitude: Number(value.birthLongitude),
        timezone: value.birthTimezone || value.timezone || null,
      } : null,
      filters: {
        avoidRahuKalam: !!value.avoidRahuKalam,
        avoidBhadra: !!value.avoidBhadra,
        preferGoodChoghadiya: !!value.preferGoodChoghadiya,
        checkPersonalHoroscope: personalized,
        checkMuhurthaLagna: !!value.checkMuhurthaLagna,
      },
    };
  }

  private toLocalIso(date: string, time: string): string { return `${date}T${time.length === 5 ? `${time}:00` : time}`; }
  private dayKey(value: string): string { const date = new Date(value); return `${date.getFullYear()}-${date.getMonth() + 1}-${date.getDate()}`; }
  private toDateInput(date: Date): string { return date.toISOString().slice(0, 10); }
  private addDays(date: Date, days: number): Date { const next = new Date(date); next.setDate(next.getDate() + days); return next; }
}
