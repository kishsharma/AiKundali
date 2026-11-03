import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { Subscription } from 'rxjs';
import { KundaliService } from '../kundali/kundali.service';
import { LocationService, LocationSearchResult } from '../../core/services/location.service';
import { HoroscopeService } from './horoscope.service';
import { RashifalShareCardComponent } from './rashifal-share-card/rashifal-share-card.component';
import {
  HoroscopeArea,
  HoroscopePeriodResponse,
  HoroscopeRashifalItem,
  HoroscopeRashifalResponse
} from './horoscope.models';

interface SavedChartOption {
  id: string;
  fullName: string;
  displayName?: string;
  moonSign?: string;
}

type HorizonTab = 'Daily' | 'Weekly' | 'Monthly';
type ModeTab = 'personal' | 'general';

@Component({
  selector: 'app-horoscope',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslocoPipe, RashifalShareCardComponent],
  templateUrl: './horoscope.component.html',
  styleUrl: './horoscope.component.scss'
})
export class HoroscopeComponent implements OnInit, OnDestroy {
  private readonly horoscopeApi = inject(HoroscopeService);
  private readonly kundaliApi = inject(KundaliService);
  private readonly locationService = inject(LocationService);
  private readonly transloco = inject(TranslocoService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  mode: ModeTab = 'personal';
  charts: SavedChartOption[] = [];
  selectedChartId = '';
  highlightMoonSign = '';
  selectedRashiName = '';
  horizon: HorizonTab = 'Daily';
  anchorDate = this.todayIso();
  response: HoroscopePeriodResponse | null = null;
  rashifal: HoroscopeRashifalResponse | null = null;
  loadingCharts = true;
  loading = false;
  errorMessage = '';
  gocharaOpen = false;
  selectedGocharaIndex: number | null = null;
  activeAreaInfoKey: string | null = null;

  locationQuery = '';
  locationResults: LocationSearchResult[] = [];
  overrideLatitude: number | null = null;
  overrideLongitude: number | null = null;
  overrideTimezone: string | null = null;
  private locationSearchTimer: ReturnType<typeof setTimeout> | null = null;
  private requestSeq = 0;
  private languageSubscription?: Subscription;

  ngOnInit(): void {
    this.languageSubscription = this.transloco.langChanges$.subscribe(() => {
      if (this.mode === 'general' && !this.loadingCharts) {
        this.load();
      }
    });

    this.kundaliApi.getSavedKundalis().subscribe({
      next: (charts: SavedChartOption[]) => {
        this.charts = (Array.isArray(charts) ? charts : [])
          .map(chart => ({
            ...chart,
            id: String(
              (chart as SavedChartOption & { Id?: string }).id
              ?? (chart as SavedChartOption & { Id?: string }).Id
              ?? ''
            )
          }))
          .filter(chart => chart.id);
        this.selectedChartId = this.charts[0]?.id ?? '';
        this.loadingCharts = false;
        this.load();
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.loadingCharts = false;
        this.errorMessage = this.transloco.translate('horoscope.loadFailed');
        this.changeDetector.markForCheck();
      }
    });
  }

  ngOnDestroy(): void {
    this.languageSubscription?.unsubscribe();
    if (this.locationSearchTimer) {
      clearTimeout(this.locationSearchTimer);
    }
  }

  setMode(mode: ModeTab): void {
    if (this.mode === mode) {
      return;
    }
    this.mode = mode;
    this.load();
  }

  setHorizon(horizon: HorizonTab): void {
    if (this.horizon === horizon) {
      return;
    }
    this.horizon = horizon;
    this.load();
  }

  onControlsChange(): void {
    this.load();
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
          this.changeDetector.markForCheck();
        },
        error: () => {
          this.locationResults = [];
          this.changeDetector.markForCheck();
        }
      });
    }, 400);
  }

  selectLocation(place: LocationSearchResult): void {
    this.overrideLatitude = Number(place.latitude);
    this.overrideLongitude = Number(place.longitude);
    this.overrideTimezone = place.timezone?.startsWith('UTC')
      ? place.timezone
      : 'UTC+05:30';
    this.locationQuery = place.displayName;
    this.locationResults = [];
    this.load();
  }

  clearLocationOverride(): void {
    this.overrideLatitude = null;
    this.overrideLongitude = null;
    this.overrideTimezone = null;
    this.locationQuery = '';
    this.locationResults = [];
    this.load();
  }

  get dateHint(): string {
    if (this.horizon === 'Weekly') {
      return this.transloco.translate('horoscope.dateHintWeekly');
    }
    if (this.horizon === 'Monthly') {
      return this.transloco.translate('horoscope.dateHintMonthly');
    }
    return this.transloco.translate('horoscope.dateHintDaily');
  }

  load(): void {
    const seq = ++this.requestSeq;
    this.loading = true;
    this.errorMessage = '';
    this.changeDetector.markForCheck();

    if (this.mode === 'general') {
      this.horoscopeApi.getRashifal(this.horizon, this.anchorDate, this.transloco.getActiveLang()).subscribe({
        next: (response) => {
          if (seq !== this.requestSeq) {
            return;
          }
          this.rashifal = this.normalizeRashifal(response);
          if (!this.rashifal.items.some(item => item.englishName === this.selectedRashiName)) {
            this.selectedRashiName = this.rashifal.items.find(item => this.isHighlighted(item))?.englishName
              ?? this.rashifal.items[0]?.englishName
              ?? '';
          }
          this.response = null;
          this.loading = false;
          this.changeDetector.markForCheck();
        },
        error: () => {
          if (seq !== this.requestSeq) {
            return;
          }
          this.loading = false;
          this.rashifal = null;
          this.errorMessage = this.transloco.translate('horoscope.loadFailed');
          this.changeDetector.markForCheck();
        }
      });
      return;
    }

    if (!this.selectedChartId || !this.anchorDate) {
      this.loading = false;
      this.response = null;
      this.changeDetector.markForCheck();
      return;
    }

    this.horoscopeApi.getPeriod({
      kundaliId: this.selectedChartId,
      anchorDate: this.anchorDate,
      horizon: this.horizon,
      latitude: this.overrideLatitude,
      longitude: this.overrideLongitude,
      timezone: this.overrideTimezone
    }).subscribe({
      next: (response) => {
        if (seq !== this.requestSeq) {
          return;
        }
        this.response = this.normalizePeriod(response);
        this.activeAreaInfoKey = null;
        this.rashifal = null;
        this.gocharaOpen = false;
        this.selectedGocharaIndex = null;
        this.loading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        if (seq !== this.requestSeq) {
          return;
        }
        this.loading = false;
        this.response = null;
        this.gocharaOpen = false;
        this.selectedGocharaIndex = null;
        this.errorMessage = this.transloco.translate('horoscope.loadFailed');
        this.changeDetector.markForCheck();
      }
    });
  }

  chartLabel(chart: SavedChartOption): string {
    return chart.displayName || chart.fullName || chart.id;
  }

  get selectedGocharaHit() {
    if (this.selectedGocharaIndex == null || !this.response?.gocharaHits?.length) {
      return null;
    }
    return this.response.gocharaHits[this.selectedGocharaIndex] ?? null;
  }

  toggleGocharaSection(): void {
    this.gocharaOpen = !this.gocharaOpen;
    if (this.gocharaOpen && this.selectedGocharaIndex == null && this.response?.gocharaHits?.length) {
      this.selectedGocharaIndex = 0;
    }
    if (!this.gocharaOpen) {
      this.selectedGocharaIndex = null;
    }
  }

  toggleGocharaDetail(index: number): void {
    if (!this.gocharaOpen) {
      this.gocharaOpen = true;
      this.selectedGocharaIndex = index;
      return;
    }
    this.selectedGocharaIndex = this.selectedGocharaIndex === index ? null : index;
  }

  isHighlighted(item: HoroscopeRashifalItem): boolean {
    if (!this.highlightMoonSign) {
      return false;
    }
    const target = this.highlightMoonSign.toLowerCase();
    return item.rashi.toLowerCase() === target || item.englishName.toLowerCase() === target;
  }

  get selectedRashi(): HoroscopeRashifalItem | null {
    const items = this.rashifal?.items ?? [];
    return items.find(item => item.englishName === this.selectedRashiName)
      ?? items.find(item => this.isHighlighted(item))
      ?? items[0]
      ?? null;
  }

  selectRashi(item: HoroscopeRashifalItem): void {
    this.selectedRashiName = item.englishName;
  }

  indianRashiName(item: HoroscopeRashifalItem): string {
    const key = `horoscope.rashi.${item.englishName.toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? item.rashi || item.englishName : localized;
  }

  toneLabel(tone: string): string {
    const key = `horoscope.tone.${(tone || 'mixed').toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? tone : localized;
  }

  areaLabel(areaKey: string): string {
    const key = `horoscope.area.${(areaKey || '').toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? areaKey : localized;
  }

  rashiIconPath(item: HoroscopeRashifalItem): string {
    return 'assets/horoscope/rashi-icons/' + item.englishName.toLowerCase() + '.svg';
  }

  formatRashifalDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
    if (!match) {
      return value ?? '';
    }
    const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    const locale = this.transloco.getActiveLang() === 'hi' ? 'hi-IN'
      : this.transloco.getActiveLang() === 'kn' ? 'kn-IN' : 'en-GB';
    return new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
  }

  toneClass(tone: string | null | undefined): string {
    const value = (tone || 'Mixed').toLowerCase();
    if (value.includes('support')) {
      return 'tone-supportive';
    }
    if (value.includes('challeng')) {
      return 'tone-challenging';
    }
    return 'tone-mixed';
  }

  areaPrediction(area: HoroscopeArea): string {
    if (area.prediction?.trim()) {
      return area.prediction.trim();
    }
    const parts = [
      ...(area.do ?? []),
      ...(area.caution ?? []),
      ...(area.avoid ?? [])
    ].filter(Boolean);
    return parts.join(' · ') || area.tone;
  }

  toggleAreaInfo(area: HoroscopeArea): void {
    const key = area.key || area.title;
    this.activeAreaInfoKey = this.activeAreaInfoKey === key ? null : key;
  }

  isAreaInfoOpen(area: HoroscopeArea): boolean {
    return this.activeAreaInfoKey === (area.key || area.title);
  }

  horizonLabel(horizon: unknown): string {
    if (horizon === 1 || horizon === 'Weekly') {
      return this.transloco.translate('horoscope.weekly');
    }
    if (horizon === 2 || horizon === 'Monthly') {
      return this.transloco.translate('horoscope.monthly');
    }
    return this.transloco.translate('horoscope.daily');
  }

  private todayIso(): string {
    const now = new Date();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }

  private normalizePeriod(raw: HoroscopePeriodResponse): HoroscopePeriodResponse {
    const anyRaw = raw as HoroscopePeriodResponse & Record<string, unknown>;
    const areas = (anyRaw.areas ?? (anyRaw as { Areas?: unknown }).Areas ?? []) as HoroscopePeriodResponse['areas'];
    return {
      horizon: (anyRaw.horizon ?? (anyRaw as { Horizon?: unknown }).Horizon ?? 'Daily') as HoroscopePeriodResponse['horizon'],
      from: String(anyRaw.from ?? (anyRaw as { From?: unknown }).From ?? ''),
      to: String(anyRaw.to ?? (anyRaw as { To?: unknown }).To ?? ''),
      overallTone: String(anyRaw.overallTone ?? (anyRaw as { OverallTone?: unknown }).OverallTone ?? 'Mixed'),
      overallScore: Number(anyRaw.overallScore ?? (anyRaw as { OverallScore?: unknown }).OverallScore ?? 0),
      summaryFacts: (anyRaw.summaryFacts ?? (anyRaw as { SummaryFacts?: string[] }).SummaryFacts ?? []) as string[],
      areas: (Array.isArray(areas) ? areas : []).map(area => this.normalizeArea(area)),
      panchanga: (anyRaw.panchanga ?? (anyRaw as { Panchanga?: unknown }).Panchanga ?? null) as HoroscopePeriodResponse['panchanga'],
      gocharaHits: (anyRaw.gocharaHits ?? (anyRaw as { GocharaHits?: unknown }).GocharaHits ?? []) as HoroscopePeriodResponse['gocharaHits'],
      dashaContext: (anyRaw.dashaContext ?? (anyRaw as { DashaContext?: unknown }).DashaContext ?? null) as HoroscopePeriodResponse['dashaContext'],
      activeFormations: (anyRaw.activeFormations ?? (anyRaw as { ActiveFormations?: unknown }).ActiveFormations ?? []) as HoroscopePeriodResponse['activeFormations'],
      horizonExtras: (anyRaw.horizonExtras ?? (anyRaw as { HorizonExtras?: unknown }).HorizonExtras ?? null) as HoroscopePeriodResponse['horizonExtras'],
      tithiPravesha: (anyRaw.tithiPravesha ?? (anyRaw as { TithiPravesha?: unknown }).TithiPravesha ?? {
        enabled: false,
        status: 'NotEnabled',
        message: '',
        hierarchicalRules: []
      }) as HoroscopePeriodResponse['tithiPravesha'],
      limitations: (anyRaw.limitations ?? (anyRaw as { Limitations?: string[] }).Limitations ?? []) as string[],
      disclaimer: String(anyRaw.disclaimer ?? (anyRaw as { Disclaimer?: unknown }).Disclaimer ?? '')
    };
  }

  private normalizeArea(area: HoroscopePeriodResponse['areas'][number] | Record<string, unknown>) {
    const raw = area as unknown as Record<string, unknown>;
    return {
      key: String(raw['key'] ?? raw['Key'] ?? ''),
      title: String(raw['title'] ?? raw['Title'] ?? ''),
      tone: String(raw['tone'] ?? raw['Tone'] ?? 'Mixed'),
      score: Number(raw['score'] ?? raw['Score'] ?? 0),
      prediction: String(raw['prediction'] ?? raw['Prediction'] ?? ''),
      facts: (raw['facts'] ?? raw['Facts'] ?? []) as string[],
      do: (raw['do'] ?? raw['Do'] ?? []) as string[],
      caution: (raw['caution'] ?? raw['Caution'] ?? []) as string[],
      avoid: (raw['avoid'] ?? raw['Avoid'] ?? []) as string[]
    };
  }

  private normalizeRashifal(raw: HoroscopeRashifalResponse): HoroscopeRashifalResponse {
    const anyRaw = raw as HoroscopeRashifalResponse & Record<string, unknown>;
    const items = (anyRaw.items ?? (anyRaw as { Items?: unknown }).Items ?? []) as HoroscopeRashifalItem[];
    return {
      horizon: (anyRaw.horizon ?? (anyRaw as { Horizon?: unknown }).Horizon ?? 'Daily') as HoroscopeRashifalResponse['horizon'],
      from: String(anyRaw.from ?? (anyRaw as { From?: unknown }).From ?? ''),
      to: String(anyRaw.to ?? (anyRaw as { To?: unknown }).To ?? ''),
      disclaimer: String(anyRaw.disclaimer ?? (anyRaw as { Disclaimer?: unknown }).Disclaimer ?? ''),
      items: (Array.isArray(items) ? items : []).map(item => {
        const rawItem = item as HoroscopeRashifalItem & Record<string, unknown>;
        return {
          rashi: String(rawItem.rashi ?? rawItem['Rashi'] ?? ''),
          englishName: String(rawItem.englishName ?? rawItem['EnglishName'] ?? ''),
          tone: String(rawItem.tone ?? rawItem['Tone'] ?? 'Mixed'),
          score: Number(rawItem.score ?? rawItem['Score'] ?? 0),
          summary: String(rawItem.summary ?? rawItem['Summary'] ?? ''),
          gocharaHighlights: (rawItem.gocharaHighlights ?? rawItem['GocharaHighlights'] ?? []) as string[],
          areas: ((rawItem.areas ?? rawItem['Areas'] ?? []) as HoroscopeRashifalItem['areas']).map(area => {
            const a = area as unknown as Record<string, unknown>;
            return {
              key: String(a['key'] ?? a['Key'] ?? ''),
              tone: String(a['tone'] ?? a['Tone'] ?? 'Mixed'),
              line: String(a['line'] ?? a['Line'] ?? '')
            };
          })
        };
      })
    };
  }
}





