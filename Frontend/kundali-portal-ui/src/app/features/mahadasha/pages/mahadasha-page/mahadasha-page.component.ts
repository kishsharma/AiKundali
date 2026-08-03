import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BehaviorSubject, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/components/page-header.component';
import { AstrologyIconComponent, AstrologyIconName } from '../../../../shared/components/astrology-icon.component';
import { SavedChartSelectorComponent } from '../../../../shared/components/saved-chart-selector/saved-chart-selector.component';
import { SavedChartMapperService, SavedKundaliDetail } from '../../../../shared/services/saved-chart-mapper.service';
import { GenerateKundaliRequest } from '../../../../core/services/kundali-api.service';
import { MahadashaService, MissingLatestKundaliPayloadError } from '../../services/mahadasha.service';
import {
  AntardashaPeriod,
  DashaLevelStrength,
  ImportantDate,
  LifeArea,
  MahadashaAnalysis,
  MahadashaPeriod,
  PratyantardashaPeriod,
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
  imports: [CommonModule, RouterLink, PageHeaderComponent, AstrologyIconComponent, SavedChartSelectorComponent],
  templateUrl: './mahadasha-page.component.html',
  styleUrls: ['./mahadasha-page.component.scss'],
})
export class MahadashaPageComponent {
  private readonly mahadashaService = inject(MahadashaService);
  private readonly savedChartMapper = inject(SavedChartMapperService);
  private readonly analysisPayload$ = new BehaviorSubject<GenerateKundaliRequest | undefined>(undefined);
  private readonly mahadashaWindowSize = 7;
  private readonly antardashaWindowSize = 5;
  private readonly pratyantardashaWindowSize = 5;

  selectedMahadashaIndex = 0;
  selectedAntardashaIndex = 0;
  selectedPratyantardashaIndex = 0;
  selectedChartId: string | null = null;

  readonly state$: Observable<MahadashaPageState> = this.analysisPayload$.pipe(
    switchMap((payload) => {
      if (!payload) {
        return of({
          analysis: null,
          loading: false,
          missingChart: true,
          error: null,
        });
      }

      return this.mahadashaService.getAnalysis(payload).pipe(
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
        }))
      );
    })
  );

  readonly note =
    'Dasha results vary based on your actions, planetary strength, and transits. Use this guidance to make better decisions and grow in life.';

  readonly phaseOrder: Array<'start' | 'middle' | 'end'> = ['start', 'middle', 'end'];

  private readonly monthYearFormatter = new Intl.DateTimeFormat('en-GB', { month: 'short', year: 'numeric' });

  readonly strengthRows = [
    { label: 'Dasha Lord', value: 'planet' },
    { label: 'Lord Strength', value: 'planetStrength' },
    { label: 'Placement', value: 'placement' },
    { label: 'Sign', value: 'sign' },
    { label: 'Aspect on Lagna', value: 'aspectOnLagna' },
    { label: 'Association', value: 'associationSummary' },
    { label: 'Overall Result', value: 'overallResult' },
  ] as const;
  onSavedChartSelected(chart: SavedKundaliDetail): void {
    this.selectedChartId = chart.id;
    this.analysisPayload$.next(this.savedChartMapper.toGenerateKundaliRequest(chart));
  }

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
    this.selectedPratyantardashaIndex = this.defaultPratyantardashaIndex(this.selectedAntardasha(analysis));
  }

  nextAntardasha(analysis: MahadashaAnalysis): void {
    if (!this.canNextAntardasha(analysis)) {
      return;
    }

    this.selectedAntardashaIndex++;
    this.selectedPratyantardashaIndex = this.defaultPratyantardashaIndex(this.selectedAntardasha(analysis));
  }
  previousPratyantardasha(): void {
    if (!this.canPreviousPratyantardasha()) {
      return;
    }

    this.selectedPratyantardashaIndex--;
  }

  nextPratyantardasha(analysis: MahadashaAnalysis): void {
    if (!this.canNextPratyantardasha(analysis)) {
      return;
    }

    this.selectedPratyantardashaIndex++;
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
      this.selectedPratyantardashaIndex = this.defaultPratyantardashaIndex(this.selectedAntardasha(analysis));
    }
  }
  selectPratyantardasha(analysis: MahadashaAnalysis, period: PratyantardashaPeriod): void {
    const index = this.selectedPratyantardashas(analysis).findIndex((item) => item === period);

    if (index >= 0) {
      this.selectedPratyantardashaIndex = index;
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
  canPreviousPratyantardasha(): boolean {
    return this.selectedPratyantardashaIndex > 0;
  }

  canNextPratyantardasha(analysis: MahadashaAnalysis): boolean {
    return this.selectedPratyantardashaIndex < this.selectedPratyantardashas(analysis).length - 1;
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
  selectedPratyantardashas(analysis: MahadashaAnalysis): PratyantardashaPeriod[] {
    const selectedAntar = this.selectedAntardasha(analysis);

    if (selectedAntar?.pratyantardashas?.length) {
      return selectedAntar.pratyantardashas;
    }

    return this.isViewingCurrentAntardasha(analysis) ? analysis.timeline.pratyantardasha ?? [] : [];
  }

  selectedPratyantardasha(analysis: MahadashaAnalysis): PratyantardashaPeriod | null {
    return this.selectedPratyantardashas(analysis)[this.selectedPratyantardashaIndex] ?? null;
  }

  isViewingCurrentMahadasha(analysis: MahadashaAnalysis): boolean {
    return this.selectedMahadashaIndex === analysis.timeline.currentMahadashaIndex;
  }

  isViewingCurrentAntardasha(analysis: MahadashaAnalysis): boolean {
    return this.isViewingCurrentMahadasha(analysis)
      && this.selectedAntardashaIndex === analysis.timeline.currentAntardashaIndex;
  }
  isViewingCurrentPratyantardasha(analysis: MahadashaAnalysis): boolean {
    return this.isViewingCurrentAntardasha(analysis)
      && this.selectedPratyantardashaIndex === analysis.timeline.currentPratyantardashaIndex;
  }

  visibleMahadashas(analysis: MahadashaAnalysis): MahadashaPeriod[] {
    return this.windowAround(analysis.timeline.mahadasha, this.selectedMahadashaIndex, this.mahadashaWindowSize);
  }

  visibleAntardashas(analysis: MahadashaAnalysis): AntardashaPeriod[] {
    return this.windowAround(this.selectedAntardashas(analysis), this.selectedAntardashaIndex, this.antardashaWindowSize);
  }
  visiblePratyantardashas(analysis: MahadashaAnalysis): PratyantardashaPeriod[] {
    return this.windowAround(this.selectedPratyantardashas(analysis), this.selectedPratyantardashaIndex, this.pratyantardashaWindowSize);
  }

  isSelectedMahadasha(analysis: MahadashaAnalysis, period: MahadashaPeriod): boolean {
    return this.selectedMahadasha(analysis) === period;
  }

  isSelectedAntardasha(analysis: MahadashaAnalysis, period: AntardashaPeriod): boolean {
    return this.selectedAntardashas(analysis)[this.selectedAntardashaIndex] === period;
  }
  isSelectedPratyantardasha(analysis: MahadashaAnalysis, period: PratyantardashaPeriod): boolean {
    return this.selectedPratyantardashas(analysis)[this.selectedPratyantardashaIndex] === period;
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

  getPlanetIconName(planet: string): AstrologyIconName {
    const icons: Record<string, AstrologyIconName> = {
      Sun: 'sun',
      Moon: 'moon',
      Mars: 'mars',
      Mercury: 'mercury',
      Jupiter: 'jupiter',
      Venus: 'venus',
      Saturn: 'saturn',
      Rahu: 'rahu',
      Ketu: 'ketu',
    };

    return icons[planet] ?? 'dasha';
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

  getDashaStrengthLevels(analysis: MahadashaAnalysis): DashaLevelStrength[] {
    const fallback: DashaLevelStrength = {
      level: 'Mahadasha',
      planet: analysis.strength.planet,
      overallStrength: analysis.strength.overallStrength,
      planetStrength: analysis.strength.planetStrength,
      placement: analysis.strength.placement,
      sign: analysis.strength.sign,
      associationTone: analysis.strength.associationTone,
      associationSummary: analysis.strength.associationSummary,
      associationDetails: analysis.strength.associationDetails,
      overallResult: analysis.strength.overallResult,
    };

    return [
      analysis.strength.mahadasha ?? fallback,
      analysis.strength.antardasha,
      analysis.strength.pratyantardasha,
    ].filter((item): item is DashaLevelStrength => Boolean(item?.planet));
  }
  getStrengthValue(analysis: MahadashaAnalysis, key: string): string {
    const strength = analysis.strength;

    if (key === 'aspectOnLagna') {
      return strength.aspectOnLagna ? 'Yes' : 'No';
    }

    if (key === 'supportedByBenefics') {
      const support = strength.supportedBy.length ? ` (${strength.supportedBy.join(', ')})` : '';
      return strength.supportedByBenefics ? `Yes${support}` : 'No';
    }

    return String(strength[key as keyof typeof strength] ?? '');
  }

  getLevelStrengthValue(item: DashaLevelStrength, key: string): string {
    if (key === 'aspectOnLagna') {
      return item.aspectOnLagna ? 'Yes' : 'No';
    }

    if (key === 'placement') {
      return this.formatHousePlacementText(item.placement);
    }

    if (key === 'associationSummary') {
      return item.associationSummary || 'None';
    }

    if (key === 'supportedByBenefics') {
      const support = item.supportedBy?.length ? ` (${item.supportedBy.join(', ')})` : '';
      return item.supportedByBenefics ? `Yes${support}` : 'No';
    }

    return String(item[key as keyof DashaLevelStrength] ?? '');
  }

  private formatHousePlacementText(value: string): string {
    return value?.replace(/\b(\d{1,2})(?:st|nd|rd|th)?\s+House\b/g, (_match, rawNumber: string) => {
      const number = Number(rawNumber);
      const suffix = number === 1 ? 'st' : number === 2 ? 'nd' : number === 3 ? 'rd' : 'th';
      return `${number}${suffix} House`;
    }) ?? '';
  }

  hasAssociationDetails(item: DashaLevelStrength): boolean {
    return Boolean(item.associationDetails?.length);
  }

  getAssociationDetailsText(item: DashaLevelStrength): string {
    return item.associationDetails?.join('\n') || 'No major association found.';
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

  periodCompletedPercent(start?: string, end?: string): number {
    const startDate = this.parseDate(start);
    const endDate = this.parseDate(end);
    const now = Date.now();

    if (!startDate || !endDate || endDate <= startDate) {
      return 0;
    }

    const percent = ((now - startDate) / (endDate - startDate)) * 100;
    return Math.max(0, Math.min(100, Math.round(percent)));
  }

  periodRemainingPercent(start?: string, end?: string): number {
    return 100 - this.periodCompletedPercent(start, end);
  }

  formatRemainingUntil(end?: string): string {
    const endDate = this.parseDate(end);

    if (!endDate) {
      return 'Not available';
    }

    const remainingMs = endDate - Date.now();

    if (remainingMs <= 0) {
      return 'Completed';
    }

    const totalDays = Math.ceil(remainingMs / 86_400_000);
    const years = Math.floor(totalDays / 365);
    const months = Math.floor((totalDays % 365) / 30);
    const days = (totalDays % 365) % 30;
    const parts: string[] = [];

    if (years) {
      parts.push(`${years}y`);
    }

    if (months) {
      parts.push(`${months}m`);
    }

    if (!years && days) {
      parts.push(`${days}d`);
    }

    return parts.length ? parts.join(' ') : 'Less than 1d';
  }
  formatPeriod(start?: string, end?: string): string {
    return [this.formatMonthYear(start), this.formatMonthYear(end)].filter(Boolean).join(' - ');
  }
  formatDetailedPeriod(start?: string, end?: string): string {
    return [start ? this.formatDate(start) : '', end ? this.formatDate(end) : ''].filter(Boolean).join(' - ');
  }

  private parseDate(value?: string): number | null {
    if (!value) {
      return null;
    }

    const time = new Date(value).getTime();
    return Number.isNaN(time) ? null : time;
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
  trackPratyantardasha(_: number, period: PratyantardashaPeriod): string {
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
    this.selectedPratyantardashaIndex = this.defaultPratyantardashaIndex(this.selectedAntardasha(analysis));
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
    this.selectedPratyantardashaIndex = this.clampIndex(
      analysis.timeline.currentPratyantardashaIndex,
      this.selectedPratyantardashas(analysis).length
    );
  }

  private defaultAntardashaIndex(period: MahadashaPeriod): number {
    const antardashas = period.antardashas ?? [];
    const currentIndex = antardashas.findIndex((antardasha) => antardasha.status === 'current');
    return currentIndex >= 0 ? currentIndex : 0;
  }
  private defaultPratyantardashaIndex(period: AntardashaPeriod | null): number {
    const pratyantardashas = period?.pratyantardashas ?? [];
    const currentIndex = pratyantardashas.findIndex((pratyantardasha) => pratyantardasha.status === 'current');
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









