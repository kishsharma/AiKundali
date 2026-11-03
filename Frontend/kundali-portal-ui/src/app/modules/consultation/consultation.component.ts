import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  HostListener,
  OnInit,
  ViewChild,
  inject
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { KundaliMetadataInsightsComponent } from '../../components/kundali-metadata-insights/kundali-metadata-insights.component';
import { KundaliScoreCard } from '../../components/kundali-score-card/kundali-score-card';
import { KundaliSummary } from '../../components/kundali-summary/kundali-summary';
import { MahadashaAntardasha } from '../../components/mahadasha-antardasha/mahadasha-antardasha';
import { buildDashaPlanetScores } from '../../components/mahadasha-antardasha/dasha-period-tone';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';
import { PanchangCardComponent } from '../../components/panchang-card/panchang-card.component';
import { RemediesRecommendationComponent } from '../../components/remedies-recommendation/remedies-recommendation.component';
import { SouthIndianChartComponent } from '../../components/south-indian-chart/south-indian-chart.component';
import { TransitInsightComponent } from '../../components/transit-insight/transit-insight.component';
import { WesternChartComponent } from '../../components/western-chart/western-chart.component';
import { AuthService } from '../../core/auth/auth.service';
import { isPaidUser } from '../../core/auth/plan.util';
import { DashaApiService } from '../../core/services/dasha-api.service';
import { SavedChartMapperService, SavedKundaliDetail } from '../../shared/services/saved-chart-mapper.service';
import { extractDoshas, extractYogas } from '../../shared/utils/kundali-yoga-dosha.util';
import { KundaliService } from '../kundali/kundali.service';
import { KundaliDoshasPanelComponent } from '../../components/kundali-doshas-panel/kundali-doshas-panel.component';
import { KundaliYogasPanelComponent } from '../../components/kundali-yogas-panel/kundali-yogas-panel.component';
import { ConsultationPlanetsSection } from './sections/consultation-planets.section';
import { ConsultationPredictionsSection } from './sections/consultation-predictions.section';

interface SavedChartOption {
  id: string;
  fullName: string;
  displayName?: string;
}

export type ConsultationSectionId =
  | 'summary'
  | 'scores'
  | 'predictions'
  | 'chart'
  | 'planets'
  | 'yogas'
  | 'doshas'
  | 'dasha'
  | 'remedies'
  | 'transit';

@Component({
  selector: 'app-consultation',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TranslocoPipe,
    NorthIndianChartComponent,
    SouthIndianChartComponent,
    WesternChartComponent,
    KundaliSummary,
    PanchangCardComponent,
    KundaliMetadataInsightsComponent,
    KundaliScoreCard,
    MahadashaAntardasha,
    RemediesRecommendationComponent,
    TransitInsightComponent,
    ConsultationPlanetsSection,
    KundaliYogasPanelComponent,
    KundaliDoshasPanelComponent,
    ConsultationPredictionsSection
  ],
  templateUrl: './consultation.component.html',
  styleUrl: './consultation.component.scss'
})
export class ConsultationComponent implements OnInit {
  private readonly kundaliApi = inject(KundaliService);
  private readonly dashaApi = inject(DashaApiService);
  private readonly chartMapper = inject(SavedChartMapperService);
  private readonly auth = inject(AuthService);
  private readonly transloco = inject(TranslocoService);
  private readonly changeDetector = inject(ChangeDetectorRef);

  @ViewChild('sectionPinPicker', { static: false }) sectionPinPicker?: ElementRef<HTMLElement>;
  readonly divisionalChartOptions = [
    { id: 'D2', label: 'D2 Hora' },
    { id: 'D3', label: 'D3 Drekkana' },
    { id: 'D4', label: 'D4 Chaturthamsa' },
    { id: 'D7', label: 'D7 Saptamsa' },
    { id: 'D9', label: 'D9 Navamsa' },
    { id: 'D10', label: 'D10 Dashamsa' },
    { id: 'D12', label: 'D12 Dwadashamsa' },
    { id: 'D16', label: 'D16 Shodasamsa' },
    { id: 'D20', label: 'D20 Vimsamsa' },
    { id: 'D24', label: 'D24 Siddhamsa' },
    { id: 'D27', label: 'D27 Bhamsa' },
    { id: 'D30', label: 'D30 Trimsamsa' },
    { id: 'D40', label: 'D40 Khavedamsa' },
    { id: 'D45', label: 'D45 Akshavedamsa' },
    { id: 'D60', label: 'D60 Shashtiamsa' }
  ];
  readonly sectionOptions: Array<{ id: ConsultationSectionId; labelKey: string }> = [
    { id: 'summary', labelKey: 'consultation.sections.summary' },
    { id: 'scores', labelKey: 'consultation.sections.scores' },
    { id: 'predictions', labelKey: 'consultation.sections.predictions' },
    { id: 'chart', labelKey: 'consultation.sections.chart' },
    { id: 'planets', labelKey: 'consultation.sections.planets' },
    { id: 'yogas', labelKey: 'consultation.sections.yogas' },
    { id: 'doshas', labelKey: 'consultation.sections.doshas' },
    { id: 'dasha', labelKey: 'consultation.sections.dasha' },
    { id: 'remedies', labelKey: 'consultation.sections.remedies' },
    { id: 'transit', labelKey: 'consultation.sections.transit' }
  ];

  charts: SavedChartOption[] = [];
  selectedChartId = '';
  private previousChartId = '';
  pinnedSections: ConsultationSectionId[] = ['chart'];
  sectionMenuOpen = false;
  chartName = '';
  kundaliResponse: any = null;
  birthFormValue: Record<string, string> = {};
  planets: any[] = [];
  houses: any[] = [];
  ascendant = '';
  scores: any = null;
  yogas: any[] = [];
  doshas: any[] = [];
  mahadashaPeriods: any[] = [];
  advancedStrengthTables: any = null;
  divisionalCharts: any[] = [];
  d9Chart: any = null;
  activeChartType: 'north' | 'south' | 'western' = 'north';
  activeDivisionalChart = 'D9';
  activeStrengthView: 'positions' | 'shadbala' | 'summary' | 'ashtakavarga' = 'positions';
  showLagnaDegrees = false;
  showDivisionalDegrees = false;
  showGocharDegrees = false;
  loadingCharts = true;
  loadingChart = false;
  errorMessage = '';

  private loadRequest = 0;

  get isPremiumUser(): boolean {
    return isPaidUser(this.auth.getCurrentUser());
  }

  get showsChart(): boolean {
    return this.isPinned('chart');
  }

  get showsPlanets(): boolean {
    return this.isPinned('planets');
  }

  get isChartOnly(): boolean {
    return this.pinnedSections.length === 1 && this.showsChart;
  }

  get hasAshtakavargaTab(): boolean {
    const raw = this.advancedStrengthTables;
    const rows = raw?.houseSupport ?? raw?.HouseSupport;
    return Array.isArray(rows) && rows.length > 0;
  }

  get hasLoadedChart(): boolean {
    return !!this.kundaliResponse;
  }

  get visibleSections(): Array<{ id: ConsultationSectionId; labelKey: string }> {
    return this.sectionOptions.filter(section => {
      if (section.id === 'transit') {
        return !!this.kundaliResponse?.transitInsight;
      }
      return true;
    });
  }

  get pinnedSectionCount(): number {
    return this.pinnedSections.length;
  }

  get sectionMenuLabel(): string {
    if (!this.pinnedSections.length) {
      return this.transloco.translate('consultation.section');
    }
    if (this.pinnedSections.length === 1) {
      return this.sectionLabel(this.pinnedSections[0]);
    }
    return this.transloco.translate('consultation.pinnedCount', { count: this.pinnedSections.length });
  }

  isPinned(sectionId: ConsultationSectionId): boolean {
    return this.pinnedSections.includes(sectionId);
  }

  sectionLabel(sectionId: ConsultationSectionId): string {
    const match = this.sectionOptions.find(section => section.id === sectionId);
    return match ? this.transloco.translate(match.labelKey) : sectionId;
  }

  toggleSectionMenu(): void {
    this.sectionMenuOpen = !this.sectionMenuOpen;
    this.changeDetector.markForCheck();
  }

  closeSectionMenu(): void {
    if (!this.sectionMenuOpen) {
      return;
    }
    this.sectionMenuOpen = false;
    this.changeDetector.markForCheck();
  }

  togglePin(sectionId: ConsultationSectionId, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();

    if (sectionId === 'transit' && !this.kundaliResponse?.transitInsight) {
      return;
    }

    const alreadyPinned = this.isPinned(sectionId);
    if (alreadyPinned && this.pinnedSections.length === 1) {
      return;
    }

    if (alreadyPinned) {
      this.pinnedSections = this.pinnedSections.filter(id => id !== sectionId);
    } else {
      this.pinnedSections = [...this.pinnedSections, sectionId];
      if (sectionId === 'planets') {
        this.activeStrengthView = 'positions';
      }
    }

    this.changeDetector.markForCheck();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.sectionMenuOpen) {
      return;
    }
    const root = this.sectionPinPicker?.nativeElement;
    if (root && !root.contains(event.target as Node)) {
      this.closeSectionMenu();
    }
  }

  get dashaPlanetScores(): Record<string, number> {
    return buildDashaPlanetScores(this.advancedStrengthTables, this.planets);
  }

  get remediesRecommendation(): any {
    return this.kundaliResponse?.remediesRecommendation || this.kundaliResponse?.RemediesRecommendation || null;
  }

  get divisionalChartChoices(): Array<{ id: string; label: string }> {
    return this.divisionalChartOptions;
  }

  get divisionalChartPlanets(): any[] {
    const chart = this.findDivisionalChart(this.activeDivisionalChart);
    return chart?.planets ?? chart?.Planets ?? [];
  }

  get divisionalChartHouses(): any[] {
    const chart = this.findDivisionalChart(this.activeDivisionalChart);
    return chart?.houses ?? chart?.Houses ?? [];
  }

  get divisionalChartAscendant(): string {
    const chart = this.findDivisionalChart(this.activeDivisionalChart);
    return chart?.ascendant ?? chart?.Ascendant ?? '';
  }

  get gocharInsight(): any {
    return this.kundaliResponse?.transitInsight ?? this.kundaliResponse?.TransitInsight ?? null;
  }

  get gocharPlanets(): any[] {
    const insight = this.gocharInsight;
    const planets = insight?.planets ?? insight?.Planets;
    return Array.isArray(planets) ? planets : [];
  }

  get gocharAscendant(): string {
    const insight = this.gocharInsight;
    return String(insight?.natalAscendant ?? insight?.NatalAscendant ?? this.ascendant ?? '').trim();
  }

  get hasGocharChart(): boolean {
    return this.gocharPlanets.length > 0;
  }

  get gocharCalculatedAt(): string {
    const insight = this.gocharInsight;
    const raw = String(insight?.calculatedAtUtc ?? insight?.CalculatedAtUtc ?? '').trim();
    if (!raw) {
      return '';
    }
    const date = new Date(raw);
    return Number.isNaN(date.getTime()) ? raw : date.toLocaleString();
  }

  setActiveChartType(type: 'north' | 'south' | 'western'): void {
    this.activeChartType = type;
    this.changeDetector.markForCheck();
  }

  setActiveDivisionalChart(chart: string): void {
    if (!this.hasDivisionalChart(chart)) {
      return;
    }
    this.activeDivisionalChart = chart;
    this.changeDetector.markForCheck();
  }

  setStrengthView(view: 'positions' | 'shadbala' | 'summary' | 'ashtakavarga'): void {
    if (view === 'ashtakavarga' && !this.hasAshtakavargaTab) {
      return;
    }
    this.activeStrengthView = view;
    this.changeDetector.markForCheck();
  }

  hasDivisionalChart(chartType: string): boolean {
    return !!this.findDivisionalChart(chartType);
  }

  private findDivisionalChart(chartType: string): any | null {
    const match = this.divisionalCharts.find(
      chart => String(chart?.chartType ?? chart?.ChartType ?? '').toUpperCase() === chartType.toUpperCase()
    );
    if (match) {
      return match;
    }
    return chartType.toUpperCase() === 'D9' ? this.d9Chart : null;
  }

  private readDivisionalCharts(response: any): any[] {
    const charts = response?.divisionalCharts ?? response?.DivisionalCharts;
    return Array.isArray(charts) ? charts : [];
  }

  ngOnInit(): void {
    this.kundaliApi.getSavedKundalis().subscribe({
      next: (charts: SavedChartOption[]) => {
        this.charts = (Array.isArray(charts) ? charts : [])
          .map(chart => ({
            ...chart,
            id: String(
              (chart as SavedChartOption & { Id?: string }).id ??
                (chart as SavedChartOption & { Id?: string }).Id ??
                ''
            )
          }))
          .filter(chart => chart.id);
        this.selectedChartId = this.charts[0]?.id ?? '';
        this.previousChartId = this.selectedChartId;
        this.loadingCharts = false;
        this.changeDetector.markForCheck();
        if (this.selectedChartId) {
          this.loadSelectedChart();
        }
      },
      error: () => {
        this.loadingCharts = false;
        this.errorMessage = this.transloco.translate('consultation.loadFailed');
        this.changeDetector.markForCheck();
      }
    });
  }

  chartLabel(chart: SavedChartOption): string {
    return chart.displayName || chart.fullName;
  }

  onChartChange(): void {
    if (this.selectedChartId === this.previousChartId) {
      return;
    }

    this.previousChartId = this.selectedChartId;
    this.loadSelectedChart();
  }


  private loadSelectedChart(): void {
    if (!this.selectedChartId) {
      return;
    }

    const request = ++this.loadRequest;
    this.loadingChart = true;
    this.errorMessage = '';
    this.changeDetector.markForCheck();

    this.kundaliApi.getSavedKundali(this.selectedChartId).subscribe({
      next: saved => {
        if (request !== this.loadRequest) {
          return;
        }

        const chartData = saved?.chartData ?? saved?.ChartData ?? null;
        this.applySavedPayload(saved, chartData);
        this.loadingChart = false;

        this.ensureMahadasha(saved, request);
        this.changeDetector.markForCheck();
      },
      error: () => {
        if (request !== this.loadRequest) {
          return;
        }
        this.loadingChart = false;
        this.resetPayload();
        this.errorMessage = this.transloco.translate('consultation.chartFailed');
        this.changeDetector.markForCheck();
      }
    });
  }

  private applySavedPayload(saved: any, chartData: any): void {
    this.kundaliResponse = chartData;
    this.chartName =
      saved?.displayName ||
      saved?.fullName ||
      this.chartLabel(this.charts.find(c => c.id === this.selectedChartId) ?? { id: '', fullName: 'chart' });
    this.birthFormValue = {
      fullName: saved?.fullName ?? '',
      surname: saved?.surname ?? '',
      fatherName: saved?.fatherName ?? '',
      motherName: saved?.motherName ?? '',
      gotra: saved?.gotra ?? '',
      gender: saved?.gender ?? '',
      birthDate: String(saved?.dateOfBirth ?? '').slice(0, 10),
      birthTime: String(saved?.timeOfBirth ?? '').slice(0, 5),
      birthPlace: saved?.birthPlace ?? ''
    };
    this.planets = chartData?.planets ?? chartData?.Planets ?? [];
    this.houses = chartData?.houses ?? chartData?.Houses ?? [];
    this.ascendant =
      chartData?.summary?.ascendant ??
      chartData?.Summary?.ascendant ??
      chartData?.ascendant ??
      '';
    this.scores = chartData?.scores ?? chartData?.Scores ?? null;
    this.yogas = extractYogas(chartData);
    this.doshas = extractDoshas(chartData);
    this.advancedStrengthTables =
      chartData?.advancedStrengthTables ?? chartData?.AdvancedStrengthTables ?? null;
    this.mahadashaPeriods = chartData?.mahadashaPeriods ?? chartData?.MahadashaPeriods ?? [];
    this.divisionalCharts = this.readDivisionalCharts(chartData);
    this.d9Chart = chartData?.d9Chart ?? chartData?.D9Chart ?? null;
    this.activeDivisionalChart = this.hasDivisionalChart('D9')
      ? 'D9'
      : this.divisionalChartChoices.find(option => this.hasDivisionalChart(option.id))?.id ?? 'D9';

    if (!chartData?.transitInsight) {
      this.pinnedSections = this.pinnedSections.filter(id => id !== 'transit');
    }
    if (!this.pinnedSections.length) {
      this.pinnedSections = ['chart'];
    }
  }

  private ensureMahadasha(saved: any, request: number): void {
    if (this.mahadashaPeriods.length || !saved) {
      return;
    }

    const detail: SavedKundaliDetail = {
      id: String(saved.id ?? this.selectedChartId),
      displayName: saved.displayName ?? this.chartName,
      fullName: saved.fullName ?? this.chartName,
      dateOfBirth: saved.dateOfBirth ?? '',
      timeOfBirth: saved.timeOfBirth ?? '',
      birthPlace: saved.birthPlace ?? '',
      createdAt: saved.createdAt ?? '',
      gender: saved.gender,
      latitude: saved.latitude,
      longitude: saved.longitude,
      timeZone: saved.timeZone
    };

    this.dashaApi.getMahadashaPeriods(this.chartMapper.toGenerateKundaliRequest(detail)).subscribe({
      next: periods => {
        if (request !== this.loadRequest) {
          return;
        }
        this.mahadashaPeriods = Array.isArray(periods) ? periods : [];
        this.changeDetector.markForCheck();
      },
      error: () => {
        if (request !== this.loadRequest) {
          return;
        }
        this.mahadashaPeriods = [];
        this.changeDetector.markForCheck();
      }
    });
  }

  private resetPayload(): void {
    this.kundaliResponse = null;
    this.birthFormValue = {};
    this.planets = [];
    this.houses = [];
    this.ascendant = '';
    this.scores = null;
    this.yogas = [];
    this.doshas = [];
    this.mahadashaPeriods = [];
    this.advancedStrengthTables = null;
    this.divisionalCharts = [];
    this.d9Chart = null;
    this.activeDivisionalChart = 'D9';
  }

}
