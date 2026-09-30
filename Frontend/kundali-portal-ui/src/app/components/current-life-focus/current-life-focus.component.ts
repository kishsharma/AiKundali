import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { Observable, of, throwError } from 'rxjs';
import { finalize, map, switchMap, tap } from 'rxjs/operators';

import { SectionCardComponent } from '../../shared/components/section-card.component';
import { KundaliService } from '../../modules/kundali/kundali.service';
import {
  CurrentLifeFocusArea,
  CurrentLifeFocusResponse,
  CurrentLifeFocusState,
  FocusedLifeAreaResponse,
  SpecificConcernOption
} from './current-life-focus.models';
import { CurrentLifeFocusService } from './current-life-focus.service';

@Component({
  selector: 'app-current-life-focus',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, SectionCardComponent],
  templateUrl: './current-life-focus.component.html',
  styleUrl: './current-life-focus.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CurrentLifeFocusComponent implements OnChanges {
  @Input() kundaliId: string | null = null;
  @Input() savePayload: any | null = null;
  @Input() resetKey: number | string | null = null;
  @Output() kundaliSaved = new EventEmitter<{ id: string; reusedExisting: boolean }>();

  state: CurrentLifeFocusState = 'initial';
  topResponse: CurrentLifeFocusResponse | null = null;
  focusedResponse: FocusedLifeAreaResponse | null = null;
  selectedAreaKey = '';
  selectedConcernKey = '';
  errorMessage = '';
  analysisDate = '';
  reusedExistingNote = false;

  private inFlight = false;

  constructor(
    private readonly currentLifeFocusService: CurrentLifeFocusService,
    private readonly kundaliService: KundaliService,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['resetKey'] && !changes['resetKey'].firstChange) {
      this.reset();
    }
  }

  get isBusy(): boolean {
    return this.inFlight || this.state === 'loadingTop' || this.state === 'loadingFocus';
  }

  get areas(): CurrentLifeFocusArea[] {
    return this.topResponse?.areas ?? [];
  }

  get prompt(): string {
    return this.topResponse?.prompt || this.topResponse?.question || '';
  }


  areaScore(area: CurrentLifeFocusArea): number | undefined {
    return area.score ?? area.activationScore;
  }

  areaTone(area: CurrentLifeFocusArea): string {
    return area.experienceTone || area.tone || 'Mixed';
  }

  areaReasons(area: CurrentLifeFocusArea): string[] {
    return (area.primaryFactors?.length ? area.primaryFactors : area.shortReasons) ?? [];
  }

  areaHouses(area: CurrentLifeFocusArea): number[] {
    return area.activatedHouses ?? [];
  }

  focusedTone(focused: FocusedLifeAreaResponse): string {
    return focused.experienceTone || focused.tone || 'Mixed';
  }

  focusedHouseLabels(focused: FocusedLifeAreaResponse): string[] {
    return (focused.activatedHouses ?? []).map(item => {
      if (typeof item === 'number') {
        return `${item}`;
      }

      const details = [item.role, Number.isFinite(Number(item.activationScore)) ? `${Math.round(Number(item.activationScore))}` : '']
        .filter(Boolean)
        .join(' · ');
      return details ? `${item.house} - ${details}` : `${item.house}`;
    });
  }

  focusedConcernOptions(focused: FocusedLifeAreaResponse): SpecificConcernOption[] {
    return (focused.specificConcerns?.length ? focused.specificConcerns : focused.followUpOptions) ?? [];
  }

  concernTitle(option: SpecificConcernOption): string {
    return option.title || option.label || option.key;
  }

  analyse(): void {
    if (this.isBusy || !this.savePayload) {
      return;
    }

    this.inFlight = true;
    this.state = 'loadingTop';
    this.errorMessage = '';
    this.focusedResponse = null;
    this.selectedAreaKey = '';
    this.selectedConcernKey = '';
    this.analysisDate = new Date().toISOString();

    this.ensureSavedKundali()
      .pipe(
        switchMap(kundaliId => this.currentLifeFocusService.getCurrentLifeFocus({
          kundaliId,
          analysisDate: this.analysisDate
        })),
        finalize(() => {
          this.inFlight = false;
          this.cdr.markForCheck();
        })
      )
      .subscribe({
        next: response => {
          this.topResponse = response;
          this.state = this.hasStrongActivation(response) ? 'topLoaded' : 'noStrongActivation';
        },
        error: error => {
          this.state = 'error';
          this.errorMessage = error?.error?.message || error?.message || 'Unable to analyse current life focus.';
        }
      });
  }

  selectArea(area: CurrentLifeFocusArea): void {
    if (this.isBusy || !this.topResponse?.analysisId) {
      return;
    }

    this.inFlight = true;
    this.state = 'loadingFocus';
    this.selectedAreaKey = area.key;
    this.selectedConcernKey = '';
    this.errorMessage = '';

    this.currentLifeFocusService.getFocusedLifeAreaAnalysis(this.topResponse.analysisId, {
      selectedAreaKey: area.key
    })
      .pipe(finalize(() => {
        this.inFlight = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: response => {
          this.focusedResponse = response;
          this.state = 'focusLoaded';
        },
        error: error => {
          this.state = 'error';
          this.errorMessage = error?.error?.message || error?.message || 'Unable to load focused analysis.';
        }
      });
  }

  selectSpecificConcern(option: SpecificConcernOption): void {
    if (this.isBusy || !this.topResponse?.analysisId || !this.selectedAreaKey) {
      return;
    }

    this.inFlight = true;
    this.state = 'loadingFocus';
    this.selectedConcernKey = option.key;

    this.currentLifeFocusService.getFocusedLifeAreaAnalysis(this.topResponse.analysisId, {
      selectedAreaKey: this.selectedAreaKey,
      specificConcernKey: option.key
    })
      .pipe(finalize(() => {
        this.inFlight = false;
        this.cdr.markForCheck();
      }))
      .subscribe({
        next: response => {
          this.focusedResponse = response;
          this.state = 'focusLoaded';
        },
        error: error => {
          this.state = 'error';
          this.errorMessage = error?.error?.message || error?.message || 'Unable to update focused analysis.';
        }
      });
  }

  selectNone(): void {
    if (this.isBusy) {
      return;
    }

    this.selectedAreaKey = '';
    this.focusedResponse = null;
    this.state = 'noneSelected';
  }

  selectOther(): void {
    if (this.isBusy) {
      return;
    }

    this.selectedAreaKey = '';
    this.focusedResponse = null;
    this.state = 'otherConcern';
  }

  retry(): void {
    if (this.state === 'error' || this.state === 'noStrongActivation') {
      this.analyse();
    }
  }

  reset(): void {
    this.state = 'initial';
    this.topResponse = null;
    this.focusedResponse = null;
    this.selectedAreaKey = '';
    this.selectedConcernKey = '';
    this.errorMessage = '';
    this.analysisDate = '';
    this.reusedExistingNote = false;
    this.inFlight = false;
    this.cdr.markForCheck();
  }

  activationClass(level: string | null | undefined): string {
    return `activation-${this.normalizeToken(level)}`;
  }

  toneClass(tone: string | null | undefined): string {
    return `tone-${this.normalizeToken(tone)}`;
  }

  trackArea(_: number, area: CurrentLifeFocusArea): string {
    return area.key;
  }

  trackText(index: number, value: string): string {
    return `${index}-${value}`;
  }

  trackConcern(_: number, option: SpecificConcernOption): string {
    return option.key;
  }

  private ensureSavedKundali(): Observable<string> {
    if (this.kundaliId) {
      return of(this.kundaliId);
    }

    return this.kundaliService.saveKundali({ ...this.savePayload, onDuplicate: 'reuse' }).pipe(
      tap(saved => {
        this.reusedExistingNote = !!saved?.reusedExisting;
      }),
      map(saved => String(saved?.id ?? saved?.kundaliId ?? '').trim()),
      switchMap(savedId => savedId ? of(savedId) : throwError(() => new Error('Kundali save did not return an id.'))),
      tap(savedId => this.kundaliSaved.emit({ id: savedId, reusedExisting: this.reusedExistingNote }))
    );
  }

  private hasStrongActivation(response: CurrentLifeFocusResponse): boolean {
    const areas = response?.areas ?? [];
    return areas.length > 0 && areas.some(area => this.normalizeToken(area.activationLevel) !== 'low');
  }

  private normalizeToken(value: string | null | undefined): string {
    return String(value ?? 'neutral').trim().toLowerCase().replace(/\s+/g, '-');
  }
}


