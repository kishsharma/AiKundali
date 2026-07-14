import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable, catchError, map, of, startWith, tap } from 'rxjs';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header.component';
import { MahadashaService, MissingLatestKundaliPayloadError } from '../../services/mahadasha.service';
import {
  AntardashaPeriod,
  ImportantDate,
  LifeArea,
  MahadashaAnalysis,
  MahadashaPeriod,
  PredictionPhase,
} from '../../models/mahadasha.model';

interface MahadashaPageState {
  analysis: MahadashaAnalysis | null;
  loading: boolean;
  missingChart: boolean;
  error: string | null;
}

@Component({
  selector: 'app-mahadasha-page',
  standalone: true,
  imports: [CommonModule, RouterLink, PageHeaderComponent],
  templateUrl: './mahadasha-page.component.html',
  styleUrls: ['./mahadasha-page.component.scss'],
})
export class MahadashaPageComponent {
  private readonly mahadashaService = inject(MahadashaService);
  private readonly mahadashaWindowSize = 7;
  private readonly antardashaWindowSize = 5;

  selectedMahadashaIndex = 0;
  selectedAntardashaIndex = 0;

  readonly state$: Observable<MahadashaPageState> = this.mahadashaService.getAnalysis().pipe(
    tap((analysis) => this.initializeTimelineSelection(analysis)),
    map((analysis) => ({
      analysis,
      loading: false,
      missingChart: false,
      error: null,
    })),
    catchError((error) => of({
      analysis: null,
      loading: false,
      missingChart: error instanceof MissingLatestKundaliPayloadError,
      error: error?.message ?? 'Unable to load Mahadasha analysis.',
    })),
    startWith({
      analysis: null,
      loading: true,
      missingChart: false,
      error: null,
    })
  );

  readonly note =
    'Dasha results vary based on your actions, planetary strength, and transits. Use this guidance to make better decisions and grow in life.';

  readonly phaseOrder: Array<'start' | 'middle' | 'end'> = ['start', 'middle', 'end'];

  private readonly monthYearFormatter = new Intl.DateTimeFormat('en-GB', { month: 'short', year: 'numeric' });

  readonly strengthRows = [
    { label: 'Dasha Lord', value: 'planetWithNature' },
    { label: 'Dasha Lord Strength', value: 'planetStrength' },
    { label: 'Placement', value: 'placement' },
    { label: 'Aspect on Lagna', value: 'aspectOnLagna' },
    { label: 'Sitting with Benefics', value: 'supportedByBenefics' },
    { label: 'Overall Result', value: 'overallResult' },
  ] as const;

  previousMahadasha(analysis: MahadashaAnalysis): void {
    if (!this.canPreviousMahadasha()) {
      return;
    }

    this.setSelectedMahadashaIndex(analysis, this.selectedMahadashaIndex - 1);
  }

  nextMahadasha(analysis: MahadashaAnalysis): void {
    if (!this.canNextMahadasha(analysis)) {
      return;
    }

    this.setSelectedMahadashaIndex(analysis, this.selectedMahadashaIndex + 1);
  }

  previousAntardasha(analysis: MahadashaAnalysis): void {
    if (!this.canPreviousAntardasha()) {
      return;
    }

    this.selectedAntardashaIndex--;
  }

  nextAntardasha(analysis: MahadashaAnalysis): void {
    if (!this.canNextAntardasha(analysis)) {
      return;
    }

    this.selectedAntardashaIndex++;
  }

  selectMahadasha(analysis: MahadashaAnalysis, period: MahadashaPeriod): void {
    const index = analysis.timeline.mahadasha.findIndex((item) => item === period);

    if (index >= 0) {
      this.setSelectedMahadashaIndex(analysis, index);
    }
  }

  selectAntardasha(analysis: MahadashaAnalysis, period: AntardashaPeriod): void {
    const index = this.selectedAntardashas(analysis).findIndex((item) => item === period);

    if (index >= 0) {
      this.selectedAntardashaIndex = index;
    }
  }

  canPreviousMahadasha(): boolean {
    return this.selectedMahadashaIndex > 0;
  }

  canNextMahadasha(analysis: MahadashaAnalysis): boolean {
    return this.selectedMahadashaIndex < analysis.timeline.mahadasha.length - 1;
  }

  canPreviousAntardasha(): boolean {
    return this.selectedAntardashaIndex > 0;
  }

  canNextAntardasha(analysis: MahadashaAnalysis): boolean {
    return this.selectedAntardashaIndex < this.selectedAntardashas(analysis).length - 1;
  }

  selectedMahadasha(analysis: MahadashaAnalysis): MahadashaPeriod {
    return analysis.timeline.mahadasha[this.selectedMahadashaIndex] ?? analysis.timeline.mahadasha[analysis.timeline.currentMahadashaIndex];
  }

  selectedAntardashas(analysis: MahadashaAnalysis): AntardashaPeriod[] {
    return this.selectedMahadasha(analysis)?.antardashas?.length
      ? this.selectedMahadasha(analysis).antardashas ?? []
      : analysis.timeline.antardasha;
  }

  selectedAntardasha(analysis: MahadashaAnalysis): AntardashaPeriod | null {
    return this.selectedAntardashas(analysis)[this.selectedAntardashaIndex] ?? null;
  }

  isViewingCurrentMahadasha(analysis: MahadashaAnalysis): boolean {
    return this.selectedMahadashaIndex === analysis.timeline.currentMahadashaIndex;
  }

  isViewingCurrentAntardasha(analysis: MahadashaAnalysis): boolean {
    return this.isViewingCurrentMahadasha(analysis)
      && this.selectedAntardashaIndex === analysis.timeline.currentAntardashaIndex;
  }

  visibleMahadashas(analysis: MahadashaAnalysis): MahadashaPeriod[] {
    return this.windowAround(analysis.timeline.mahadasha, this.selectedMahadashaIndex, this.mahadashaWindowSize);
  }

  visibleAntardashas(analysis: MahadashaAnalysis): AntardashaPeriod[] {
    return this.windowAround(this.selectedAntardashas(analysis), this.selectedAntardashaIndex, this.antardashaWindowSize);
  }

  isSelectedMahadasha(analysis: MahadashaAnalysis, period: MahadashaPeriod): boolean {
    return this.selectedMahadasha(analysis) === period;
  }

  isSelectedAntardasha(analysis: MahadashaAnalysis, period: AntardashaPeriod): boolean {
    return this.selectedAntardashas(analysis)[this.selectedAntardashaIndex] === period;
  }

  getStars(rating: number): boolean[] {
    return Array.from({ length: 5 }, (_, index) => index < Math.round(rating));
  }

  getPlanetMark(planet: string): string {
    const marks: Record<string, string> = {
      Sun: 'Su',
      Moon: 'Mo',
      Mars: 'Ma',
      Rahu: 'Ra',
      Jupiter: 'Ju',
      Saturn: 'Sa',
      Mercury: 'Me',
      Ketu: 'Ke',
      Venus: 'Ve',
    };

    return marks[planet] ?? planet.slice(0, 2);
  }

  getAreaMark(area: string): string {
    const marks: Record<string, string> = {
      Career: 'Br',
      Finance: 'Rs',
      Relationships: 'He',
      Marriage: 'Ma',
      Health: 'Ec',
      Education: 'Ed',
      Spirituality: 'Sp',
      Travel: 'Tr',
    };

    return marks[area] ?? area.slice(0, 2);
  }

  getStrengthValue(analysis: MahadashaAnalysis, key: string): string {
    const strength = analysis.strength;

    if (key === 'planetWithNature') {
      return `${strength.planet} (${analysis.currentMahadasha.nature})`;
    }

    if (key === 'aspectOnLagna') {
      return strength.aspectOnLagna ? 'Yes' : 'No';
    }

    if (key === 'supportedByBenefics') {
      const support = strength.supportedBy.length ? ` (${strength.supportedBy.join(', ')})` : '';
      return strength.supportedByBenefics ? `Yes${support}` : 'No';
    }

    return String(strength[key as keyof typeof strength] ?? '');
  }

  formatBirthDateTime(value: string): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return value || 'Not available';
    }

    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    }).format(date);
  }

  getDashaOwnerName(analysis: MahadashaAnalysis): string {
    return analysis.birthChart?.dashaBelongsTo || analysis.birthChart?.name || 'this person';
  }
  formatDate(value: string): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }

  formatPeriod(start?: string, end?: string): string {
    return [this.formatMonthYear(start), this.formatMonthYear(end)].filter(Boolean).join(' - ');
  }

  private formatMonthYear(value?: string): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      const match = value.match(/([A-Za-z]{3,9})\s+(\d{4})|(?:\d{1,2}\s+)?([A-Za-z]{3,9})\s+(\d{4})|(\d{4})[-/]?(\d{1,2})?/);

      if (!match) {
        return value;
      }

      const month = match[1] ?? match[3];
      const year = match[2] ?? match[4] ?? match[5];
      const monthIndex = match[6] ? Number(match[6]) - 1 : this.monthIndex(month);
      const fallbackDate = new Date(Number(year), monthIndex >= 0 ? monthIndex : 0, 1);

      return this.monthYearFormatter.format(fallbackDate);
    }

    return this.monthYearFormatter.format(date);
  }

  trackLifeArea(_: number, area: LifeArea): string {
    return area.name;
  }

  trackMahadasha(_: number, period: MahadashaPeriod): string {
    return `${period.planet}-${period.start}-${period.end}`;
  }

  trackAntardasha(_: number, period: AntardashaPeriod): string {
    return `${period.planet}-${period.start}-${period.end}`;
  }

  trackDate(_: number, date: ImportantDate): string {
    return `${date.title}-${date.date}`;
  }

  trackText(_: number, text: string): string {
    return text;
  }

  trackPhase(_: number, phase: PredictionPhase): string {
    return phase.title;
  }

  private setSelectedMahadashaIndex(analysis: MahadashaAnalysis, index: number): void {
    this.selectedMahadashaIndex = this.clampIndex(index, analysis.timeline.mahadasha.length);
    this.selectedAntardashaIndex = this.defaultAntardashaIndex(this.selectedMahadasha(analysis));
  }
  private monthIndex(month?: string): number {
    if (!month) {
      return -1;
    }

    return [
      'jan',
      'feb',
      'mar',
      'apr',
      'may',
      'jun',
      'jul',
      'aug',
      'sep',
      'oct',
      'nov',
      'dec',
    ].findIndex((item) => month.toLowerCase().startsWith(item));
  }

  private initializeTimelineSelection(analysis: MahadashaAnalysis): void {
    this.selectedMahadashaIndex = this.clampIndex(
      analysis.timeline.currentMahadashaIndex,
      analysis.timeline.mahadasha.length
    );
    this.selectedAntardashaIndex = this.clampIndex(
      analysis.timeline.currentAntardashaIndex,
      this.selectedAntardashas(analysis).length
    );
  }

  private defaultAntardashaIndex(period: MahadashaPeriod): number {
    const antardashas = period.antardashas ?? [];
    const currentIndex = antardashas.findIndex((antardasha) => antardasha.status === 'current');
    return currentIndex >= 0 ? currentIndex : 0;
  }

  private windowAround<T>(items: T[], selectedIndex: number, size: number): T[] {
    if (items.length <= size) {
      return items;
    }

    const start = Math.min(
      Math.max(0, selectedIndex - Math.floor(size / 2)),
      items.length - size
    );

    return items.slice(start, start + size);
  }

  private clampIndex(index: number, length: number): number {
    if (length <= 0) {
      return 0;
    }

    return Math.min(Math.max(index, 0), length - 1);
  }
}

