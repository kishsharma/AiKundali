import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KundaliService } from './kundali.service';
import { PageHeaderComponent } from "../../shared/components/page-header.component";
import { SectionCardComponent } from "../../shared/components/section-card.component";
import { of } from 'rxjs';
import { GenerateKundaliRequest } from '../../core/services/kundali-api.service';
import { LatestKundaliStorageService } from '../../core/services/latest-kundali-storage.service';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { LocationService } from '../../core/services/location.service';
import { DashaApiService } from '../../core/services/dasha-api.service';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';
import { SouthIndianChartComponent } from '../../components/south-indian-chart/south-indian-chart.component';
import { WesternChartComponent } from '../../components/western-chart/western-chart.component';
import { SudarshanChakraChartComponent } from '../../components/sudarshan-chakra-chart/sudarshan-chakra-chart.component';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { form } from '@angular/forms/signals';
import { HouseDetailsCard } from "../../components/house-details-card/house-details-card";
import { KundaliSummary } from "../../components/kundali-summary/kundali-summary";
import { PanchangCardComponent } from "../../components/panchang-card/panchang-card.component";
import { KundaliScoreCard } from "../../components/kundali-score-card/kundali-score-card";
import { MahadashaAntardasha } from "../../components/mahadasha-antardasha/mahadasha-antardasha";
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { HouseScoreOverview } from "../../components/house-score-overview/house-score-overview";
import { KundaliMetadataInsightsComponent } from "../../components/kundali-metadata-insights/kundali-metadata-insights.component";
import { TransitInsightComponent } from "../../components/transit-insight/transit-insight.component";
import { RemediesRecommendationComponent } from "../../components/remedies-recommendation/remedies-recommendation.component";
import {
  AstrologerBrandingSetting,
  AstrologerBrandingSettingsService
} from '../../core/services/astrologer-branding-settings.service';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';

@Component({
  selector: 'app-kundali',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    SectionCardComponent,
    NorthIndianChartComponent,
    SouthIndianChartComponent,
    WesternChartComponent,
    SudarshanChakraChartComponent,
    HouseDetailsCard,
    KundaliSummary,
    PanchangCardComponent,
    KundaliScoreCard,
    MahadashaAntardasha,
    TranslocoPipe,
    HouseScoreOverview,
    KundaliMetadataInsightsComponent,
    TransitInsightComponent,
    RemediesRecommendationComponent
],
  templateUrl: './kundali.component.html',
  styleUrls: ['./kundali.component.scss', './kundali.component.exportpdf.scss']
})
export class KundaliComponent {

constructor(private translocoService: TranslocoService,private cdr: ChangeDetectorRef) {}
  private fb = inject(FormBuilder);
  private kundaliService = inject(KundaliService);
  private dashaApiService = inject(DashaApiService);
  private locationService = inject(LocationService);
  private latestKundaliStorage = inject(LatestKundaliStorageService);
  private apiTerms = inject(ApiTermTranslationService);
  private brandingSettingsService = inject(AstrologerBrandingSettingsService);
  locationResults: any[] = [];
  kundaliResponse: any = null;
  brandingSettings: AstrologerBrandingSetting | null = null;
  houseScores: any[] = [];
  loading = false;
  showChart = false;
  mahadashaPeriods: any[] = [];
  savingKundali = false;
  savedKundaliId: string | null = null;
  saveMessage = '';
  horaverseOpenError = '';
  isChartPinned = false;
  displayPersonName = '';
  janmaStoryLines: string[] = [];
  displayedJanmaStoryLines: string[] = [];
  janmaStoryHighlightTerms: Array<{ value: string; type: string }> = [];
  private janmaStoryTimers: ReturnType<typeof setTimeout>[] = [];
  readonly kundaliPreparationSteps = [
    'Mapping planetary positions',
    'Reading house strengths',
    'Preparing yogas and doshas',
    'Opening dasha timeline',
    'Composing your report'
  ];
  planets: any[] = [];
  summary: any;
  scores: any[] = [];
  yogas: any[] = [];
  doshas: any[] = [];
  openYogaIndex: number | null = null;
  openDoshaIndex: number | null = null;
  selectedHouseData: any;
  activeChartType: 'north' | 'south' | 'western' = 'north';
  activeDivisionalChart: 'd1' | 'd9' = 'd1';
  d1Planets: any[] = [];
  d1Houses: any[] = [];
  d1Ascendant: string | undefined = '';
  d9Chart: any = null;
  today: Date = new Date();
exportingPdf = false;

  private readonly planetGlyphMap: Record<string, string> = {
    Sun: '☉',
    Moon: '☾',
    Mars: '♂',
    Mercury: '☿',
    Jupiter: '♃',
    Venus: '♀',
    Saturn: '♄',
    Rahu: '☊',
    Ketu: '☋'
  };
  private readonly rashiWatermarkMap: Record<string, { symbol: string; indianName: string; englishName: string }> = {
    aries: { symbol: '🐏', indianName: 'Mesha', englishName: 'Aries' },
    mesha: { symbol: '🐏', indianName: 'Mesha', englishName: 'Aries' },
    taurus: { symbol: '🐂', indianName: 'Vrishabha', englishName: 'Taurus' },
    vrishabha: { symbol: '🐂', indianName: 'Vrishabha', englishName: 'Taurus' },
    gemini: { symbol: '👥', indianName: 'Mithuna', englishName: 'Gemini' },
    mithuna: { symbol: '👥', indianName: 'Mithuna', englishName: 'Gemini' },
    cancer: { symbol: '🦀', indianName: 'Karka', englishName: 'Cancer' },
    karka: { symbol: '🦀', indianName: 'Karka', englishName: 'Cancer' },
    karkataka: { symbol: '🦀', indianName: 'Karka', englishName: 'Cancer' },
    leo: { symbol: '🦁', indianName: 'Simha', englishName: 'Leo' },
    simha: { symbol: '🦁', indianName: 'Simha', englishName: 'Leo' },
    sinh: { symbol: '🦁', indianName: 'Simha', englishName: 'Leo' },
    virgo: { symbol: '🌾', indianName: 'Kanya', englishName: 'Virgo' },
    kanya: { symbol: '🌾', indianName: 'Kanya', englishName: 'Virgo' },
    libra: { symbol: '⚖', indianName: 'Tula', englishName: 'Libra' },
    tula: { symbol: '⚖', indianName: 'Tula', englishName: 'Libra' },
    scorpio: { symbol: '🦂', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrischika: { symbol: '🦂', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrishchika: { symbol: '🦂', indianName: 'Vrischika', englishName: 'Scorpio' },
    sagittarius: { symbol: '🏹', indianName: 'Dhanu', englishName: 'Sagittarius' },
    dhanu: { symbol: '🏹', indianName: 'Dhanu', englishName: 'Sagittarius' },
    capricorn: { symbol: '🐐', indianName: 'Makara', englishName: 'Capricorn' },
    makara: { symbol: '🐐', indianName: 'Makara', englishName: 'Capricorn' },
    aquarius: { symbol: '🏺', indianName: 'Kumbha', englishName: 'Aquarius' },
    kumbha: { symbol: '🏺', indianName: 'Kumbha', englishName: 'Aquarius' },
    pisces: { symbol: '🐟🐟', indianName: 'Meena', englishName: 'Pisces' },
    meena: { symbol: '🐟🐟', indianName: 'Meena', englishName: 'Pisces' },
    meen: { symbol: '🐟🐟', indianName: 'Meena', englishName: 'Pisces' }
  };

  ngOnInit(): void {
    this.loadBrandingSettings();

    this.kundaliForm.controls.birthPlace.valueChanges
      .pipe(
        debounceTime(500),

        tap(value => {
          console.log('Typing:', value);
        }),

        distinctUntilChanged(),

        switchMap((value: string | null) => {

          if (!value || value.length < 3) {
            return of([]);
          }

          return this.locationService.searchLocation(value);
        })
      )
      .subscribe(results => {

        console.log(results);

        this.locationResults = results;
      });
  }

  loadBrandingSettings(): void {
    this.brandingSettingsService.getMine().subscribe({
      next: (settings) => {
        this.brandingSettings = settings;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Branding settings load error:', error);
        this.brandingSettings = null;
      }
    });
  }

  get activeBranding(): AstrologerBrandingSetting | null {
    return this.brandingSettings?.isActive ? this.brandingSettings : null;
  }

  get showChartBranding(): boolean {
    return !!this.activeBranding?.showOnChartPage;
  }

  get showPdfBranding(): boolean {
    return !!this.activeBranding?.showOnPdfReport;
  }

  get reportLogoUrl(): string {
    return this.showPdfBranding ? this.activeBranding?.logoUrl || 'assets/logo.png' : 'assets/logo.png';
  }

  get reportAstrologerName(): string {
    return this.showPdfBranding
      ? this.activeBranding?.astrologerName || this.translocoService.translate('kundali.reportAuthorName')
      : this.translocoService.translate('kundali.reportAuthorName');
  }

  get reportAstrologerDesignation(): string {
    return this.showPdfBranding
      ? this.activeBranding?.designation || this.translocoService.translate('kundali.reportAuthorRole')
      : this.translocoService.translate('kundali.reportAuthorRole');
  }

  get reportAstrologerContact(): string {
    if (!this.showPdfBranding || !this.activeBranding) {
      return this.translocoService.translate('kundali.reportAuthorContact');
    }

    return [
      this.activeBranding.contactNumber,
      this.activeBranding.email
    ].filter(Boolean).join(' | ');
  }

  get chandraRashiWatermark(): { symbol: string; indianName: string; englishName: string } | null {
    const moonSign = this.firstText(
      this.summary?.rashi,
      this.summary?.moonSign,
      this.summary?.chandraRashi,
      this.kundaliResponse?.rashi,
      this.kundaliResponse?.rashiName,
      this.kundaliResponse?.summary?.moonSign,
      this.kundaliResponse?.summary?.chandraRashi,
      this.planets.find(planet => String(planet?.planet ?? '').toLowerCase() === 'moon')?.sign
    );

    if (!moonSign) {
      return null;
    }

    const normalized = moonSign.toLowerCase().replace(/[^a-z]/g, '');
    const matchedKey = Object.keys(this.rashiWatermarkMap)
      .find(key => normalized === key || normalized.includes(key) || key.includes(normalized));

    return matchedKey
      ? this.rashiWatermarkMap[matchedKey]
      : { symbol: moonSign.slice(0, 1).toUpperCase(), indianName: moonSign, englishName: 'Moon Rashi' };
  }
  private buildKundaliPayload(): GenerateKundaliRequest {
    const value = this.kundaliForm.getRawValue();
    return {
      name: value.fullName ?? '',
      gender: value.gender ?? '',
      birthDateTime: `${value.birthDate ?? ''}T${value.birthTime ?? '00:00'}:00`,
      birthplace: value.birthPlace ?? '',
      birthPlace: value.birthPlace ?? '',
      latitude: this.toNumber(value.latitude),
      longitude: this.toNumber(value.longitude),
      timeZoneOffset: 5.30
    };
  }

  private firstText(...values: unknown[]): string {
    for (const value of values) {
      const text = String(value ?? '').trim();
      if (text) {
        return text;
      }
    }

    return '';
  }

  private toNumber(value: unknown): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  saveCurrentKundali(): void {
    if (!this.kundaliResponse || this.savingKundali) { return; }
    this.savingKundali = true;
    this.saveMessage = '';
    const payload = { ...this.buildKundaliPayload(), chartData: this.kundaliResponse };
    this.kundaliService.saveKundali(payload).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? saved?.kundaliId ?? this.savedKundaliId;
        this.saveMessage = 'Kundali saved.';
        this.savingKundali = false;
        window.dispatchEvent(new CustomEvent('kundali-saved'));
        this.cdr.detectChanges();
      },
      error: () => {
        this.saveMessage = 'Unable to save Kundali.';
        this.savingKundali = false;
        this.cdr.detectChanges();
      }
    });
  }

  toggleChartPin(): void { this.isChartPinned = !this.isChartPinned; }

  selectedCoordinates: any = null;

  selectLocation(place: any): void {

    this.kundaliForm.patchValue({
      birthPlace: place.display_name,
      latitude: place.lat,
      longitude: place.lon
    });

    this.selectedCoordinates = {
      latitude: place.lat,
      longitude: place.lon
    };

    this.locationResults = [];

    console.log(this.selectedCoordinates);
  }
  kundaliForm = this.fb.group({
    fullName: ['kishore', Validators.required],
    gender: ['Male', Validators.required],
    birthDate: ['1986-10-06', Validators.required],
    birthTime: ['11:45', Validators.required],
    birthPlace: ['Hingoli, Maharashtra, India', Validators.required],
    latitude: ['19.5431163'],
    longitude: ['77.1739432']
  });

  formatBirthDate(value: string | null | undefined): string {
    if (!value) {
      return '—';
    }

    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);

    if (!match) {
      return value;
    }

    const [, year, month, day] = match;
    const parsedDate = new Date(Number(year), Number(month) - 1, Number(day));

    return parsedDate.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  normalizeStrength(value: string | null | undefined): string {
    if (!value) {
      return 'NotApplicable';
    }

    return value.replace(/^strength\./, '').trim();
  }

  getStrengthLabel(value: string | null | undefined): string {
    const normalized = this.normalizeStrength(value);
    const key = `strength.${normalized}`;
    const translated = this.translocoService.translate(key);

    if (translated && translated !== key) {
      return translated;
    }

    return normalized
      .replace(/([a-z])([A-Z])/g, '$1 $2')
      .replace(/\s+/g, ' ');
  }

  getStrengthClass(value: string | null | undefined): Record<string, boolean> {
    const normalized = this.normalizeStrength(value);
    const isWeak = normalized.includes('Weak') || normalized.includes('Enemy') || normalized.includes('Debilitated');
    const isStrong = !isWeak && (normalized.includes('Own') || normalized.includes('Exalted'));

    return {
      strong: isStrong,
      neutral: !isStrong && !isWeak && (normalized.includes('Neutral') || normalized.includes('NotApplicable')),
      weak: isWeak
    };
  }

  getPlanetGlyph(planet: string | null | undefined): string {
    return this.planetGlyphMap[planet ?? ''] ?? '•';
  }

  getPlanetSupportStatus(planet: any): 'supportive' | 'neutral' | 'challenging' {
    const normalized = this.normalizeStrength(planet?.detailedStrength);
    const isChallenging = planet?.isCombust || normalized.includes('Weak') || normalized.includes('Enemy') || normalized.includes('Debilitated');
    const isSupportive = !isChallenging && (normalized.includes('Own') || normalized.includes('Exalted'));

    if (isChallenging) {
      return 'challenging';
    }

    if (isSupportive) {
      return 'supportive';
    }

    return 'neutral';
  }

  getPlanetSupportClass(planet: any): Record<string, boolean> {
    const status = this.getPlanetSupportStatus(planet);

    return {
      supportive: status === 'supportive',
      neutral: status === 'neutral',
      challenging: status === 'challenging'
    };
  }

  getPlanetSupportLabel(planet: any): string {
    const status = this.getPlanetSupportStatus(planet);

    if (status === 'supportive') {
      return 'Supportive';
    }

    if (status === 'challenging') {
      return 'Challenging';
    }

    return 'Neutral';
  }

  getAspectedByText(planet: any): string {
    const aspects = this.normalizePlanetNameList(planet?.aspectedBy);

    if (!aspects.length) {
      return this.translocoService.translate('common.none');
    }

    return aspects.map(name => this.translatePlanetName(name)).join(', ');
  }

  hasPlanetAspects(planet: any): boolean {
    return this.normalizePlanetNameList(planet?.aspectedBy).length > 0;
  }

  translatePlanetName(planet: unknown): string { return this.apiTerms.translatePlanet(planet); }
  translateSignName(sign: unknown): string { return this.apiTerms.translateSign(sign); }
  translateNakshatraName(nakshatra: unknown): string { return this.apiTerms.translateNakshatra(nakshatra); }

  private normalizePlanetNameList(value: any): string[] {
    if (!value) {
      return [];
    }

    if (Array.isArray(value)) {
      return value.map(item => String(item).trim()).filter(Boolean);
    }

    if (typeof value === 'string') {
      return value.split(',').map(item => item.trim()).filter(Boolean);
    }

    return [];
  }

  get activeChartPlanets(): any[] {
    return this.activeDivisionalChart === 'd9'
      ? this.d9Chart?.planets ?? []
      : this.d1Planets;
  }

  get activeChartHouses(): any[] {
    return this.activeDivisionalChart === 'd9'
      ? this.d9Chart?.houses ?? []
      : this.d1Houses;
  }

  get activeChartAscendant(): string {
    return this.activeDivisionalChart === 'd9'
      ? this.d9Chart?.ascendant ?? ''
      : this.d1Ascendant ?? '';
  }

  get chartTitle(): string {
    return this.activeDivisionalChart === 'd9'
      ? 'Navamsa Kundali (D9)'
      : 'Lagna Kundali';
  }

  get primaryChartPlanets(): any[] {
    return this.exportingPdf ? this.d1Planets : this.activeChartPlanets;
  }

  get primaryChartHouses(): any[] {
    return this.exportingPdf ? this.d1Houses : this.activeChartHouses;
  }

  get primaryChartAscendant(): string {
    return this.exportingPdf ? this.d1Ascendant ?? '' : this.activeChartAscendant;
  }

  get primaryChartTitle(): string {
    return this.exportingPdf ? 'Lagna Kundali' : this.chartTitle;
  }

  setActiveDivisionalChart(chart: 'd1' | 'd9'): void {
    if (chart === 'd9' && !this.d9Chart) {
      return;
    }

    this.activeDivisionalChart = chart;
    this.setDefaultHouseSelection();
  }

  generateKundali(): void {
    this.showChart = false;
    this.selectedHouseData = null;
    this.activeDivisionalChart = 'd1';
    if (this.kundaliForm.invalid) {
      this.kundaliForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload = this.buildKundaliPayload();
    this.kundaliService.generateKundali(payload)
      .subscribe({
        next: (response) => {
          console.log('Generated:', response);
          this.kundaliResponse = response;
          console.log(response);
console.log('scoreslog'  , this.kundaliResponse?.scores);
          this.planets = response.planets;
          this.d1Planets = response.planets ?? [];
          this.d1Houses = response.houses ?? [];
          this.summary = response.summary;
          this.d1Ascendant = response.summary?.ascendant;
          this.d9Chart = response.d9Chart ?? null;
          this.scores = response.scores;
          this.yogas = this.extractYogas(response);
          this.doshas = this.extractDoshas(response);
          this.openYogaIndex = null;
          this.openDoshaIndex = null;
          this.houseScores = response.houses;  
          this.kundaliResponse = response;
          this.showChart = true;
          this.savedKundaliId = null;
          this.saveMessage = '';
          this.latestKundaliStorage.save(payload);
          this.startJanmaStory(response);
          this.setDefaultHouseSelection();
          this.cdr.detectChanges();
          console.log('houseScoreslog'  , this.houseScores);
          console.log('houseScoreslog2'  ,  response.houses);
          console.log(this.planets);

          this.dashaApiService.getMahadashaPeriods(payload).subscribe({
            next: (mahaResponse) => {
              console.log('Mahadasha periods:', mahaResponse);
              this.mahadashaPeriods = mahaResponse;
              this.startJanmaStory(response);
              this.loading = false;
              this.cdr.detectChanges();
              console.log('Mahadasha periods:', this.mahadashaPeriods);
            },
            error: (error) => {
              console.error('Mahadasha periods error:', error);
              this.loading = false;
              this.cdr.detectChanges();
            }
          });
        },
        error: (error) => {
          console.error(error);
          this.showChart = false;
          this.selectedHouseData = null;
          setTimeout(() => {
  this.loading = false;
});
        }
      });
  }

  setDefaultHouseSelection(): void {
    const availableHouses = this.activeChartHouses?.length
      ? this.activeChartHouses
      : this.kundaliResponse?.houses ?? this.houseScores ?? [];
    const defaultHouse = availableHouses.find((item: any) => item?.houseNumber != null) ?? availableHouses[0] ?? null;
    this.selectedHouseData = defaultHouse;
  }

  extractYogas(response: any): any[] {
    const rawYogas = response?.yogas ?? response?.summary?.yogas ?? response?.summary?.yoga ?? response?.yoga;

    if (Array.isArray(rawYogas)) {
      return rawYogas
        .map((item: any) => {
          if (!item) {
            return null;
          }

          if (typeof item === 'string') {
            const trimmed = item.trim();
            return trimmed ? { name: trimmed } : null;
          }

          const name = item.name || item.title || item.label || item.yogaName;

          if (!name) {
            return null;
          }

          return {
            name: String(name),
            category: item.category,
            strength: item.strength,
            reason: item.reason,
            sanskritName: item.sanskritName,
            description: item.description,
            impact: item.impact,
            involvedPlanets: item.involvedPlanets ?? item.planets ?? [],
            involvedHouses: item.involvedHouses ?? item.houses ?? [],
            impactAreas: this.normalizeImpactAreas(item.impactAreas ?? item.ImpactAreas ?? item.effects ?? item.Effects ?? [])
          };
        })
        .filter(Boolean);
    }

    if (typeof rawYogas === 'string') {
      return rawYogas
        .split(',')
        .map((item: string) => item.trim())
        .filter(Boolean)
        .map((name: string) => ({ name }));
    }

    return [];
  }

  extractDoshas(response: any): any[] {
    const rawDoshas = response?.doshas ?? response?.summary?.doshas ?? response?.summary?.dosha ?? response?.dosha;

    if (Array.isArray(rawDoshas)) {
      return rawDoshas
        .map((item: any) => {
          if (!item) {
            return null;
          }

          if (typeof item === 'string') {
            const trimmed = item.trim();
            return trimmed ? { name: trimmed } : null;
          }

          const name = item.name || item.Name || item.title || item.label || item.doshaName || item.DoshaName;

          if (!name) {
            return null;
          }

          return {
            name: String(name),
            category: item.category ?? item.Category,
            severity: item.severity ?? item.Severity ?? item.strength ?? item.Strength ?? item.status ?? item.Status,
            reason: item.reason ?? item.Reason,
            description: item.description ?? item.Description,
            involvedPlanets: item.involvedPlanets ?? item.InvolvedPlanets ?? item.planets ?? item.Planets ?? [],
            involvedHouses: item.involvedHouses ?? item.InvolvedHouses ?? item.houses ?? item.Houses ?? [],
            impactAreas: this.normalizeImpactAreas(item.impactAreas ?? item.ImpactAreas ?? item.effects ?? item.Effects ?? []),
            remedies: item.remedies ?? item.Remedies ?? []
          };
        })
        .filter(Boolean);
    }

    if (typeof rawDoshas === 'string') {
      return rawDoshas
        .split(',')
        .map((item: string) => item.trim())
        .filter(Boolean)
        .map((name: string) => ({ name }));
    }

    return [];
  }

  private normalizeImpactAreas(rawImpactAreas: any): any[] {
    if (!Array.isArray(rawImpactAreas)) {
      return [];
    }

    return rawImpactAreas
      .map((area: any) => {
        const category = area?.category ?? area?.Category;
        const statements = area?.statements ?? area?.Statements ?? [];

        if (!category || !Array.isArray(statements) || !statements.length) {
          return null;
        }

        return {
          category,
          statements
        };
      })
      .filter(Boolean);
  }

  getDoshaSeverityClass(value: string | null | undefined): Record<string, boolean> {
    const severity = (value ?? '').toLowerCase();

    return {
      high: severity.includes('high'),
      medium: severity.includes('medium'),
      low: severity.includes('low') || !severity
    };
  }

  toggleYogaDetails(index: number): void {
    this.openYogaIndex = this.openYogaIndex === index ? null : index;
  }

  toggleDoshaDetails(index: number): void {
    this.openDoshaIndex = this.openDoshaIndex === index ? null : index;
  }

  isYogaDetailsOpen(index: number): boolean {
    return this.openYogaIndex === index;
  }

  isDoshaDetailsOpen(index: number): boolean {
    return this.openDoshaIndex === index;
  }

  hasImpactDetails(item: any): boolean {
    return !!(item?.impactAreas?.length || item?.remedies?.length || item?.impact);
  }

  getImpactIcon(category: string | null | undefined): string {
    const normalized = (category ?? '').trim();

    if (!normalized) {
      return 'IN';
    }

    return normalized
      .split(/[\s&/,-]+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }

  getImpactTone(category: string | null | undefined): Record<string, boolean> {
    const normalized = (category ?? '').toLowerCase();

    return {
      mind: normalized.includes('mental') || normalized.includes('emotional') || normalized.includes('mind'),
      relation: normalized.includes('relationship') || normalized.includes('marriage') || normalized.includes('family'),
      career: normalized.includes('career') || normalized.includes('financial') || normalized.includes('wealth'),
      health: normalized.includes('health'),
      spiritual: normalized.includes('spiritual')
    };
  }

  private async addElementToPdf(
    pdf: jsPDF,
    element: HTMLElement,
    pdfWidth: number,
    pageHeight: number,
    margin: number = 8,
    pageHeightUsed: number = 0,
    spacing: number = 6
  ): Promise<number> {
    const canvas = await html2canvas(element, {
      scale: 2,
      backgroundColor: '#ffffff',
      useCORS: true,
      logging: false,
      windowWidth: element.scrollWidth,
      width: element.scrollWidth,
      height: element.scrollHeight,
      onclone: (clonedDocument: Document) => this.sanitizePdfClone(clonedDocument)
    });

    const usableWidth = pdfWidth - margin * 2;
    const imgWidth = canvas.width;
    const imgHeight = canvas.height;
    const contentHeight = pageHeight - margin * 2;
    const renderedHeight = (imgHeight * usableWidth) / imgWidth;

    let currentPageHeight = pageHeightUsed;

    if (renderedHeight > contentHeight) {
      let startY = 0;
      while (startY < imgHeight) {
        const remainingHeight = imgHeight - startY;
        const sliceHeight = Math.min(remainingHeight, Math.ceil((contentHeight * imgWidth) / usableWidth));

        if (sliceHeight <= 0) {
          break;
        }

        if (currentPageHeight > 0) {
          pdf.addPage();
          currentPageHeight = 0;
        }

        const sliceCanvas = document.createElement('canvas');
        sliceCanvas.width = imgWidth;
        sliceCanvas.height = sliceHeight;
        const ctx = sliceCanvas.getContext('2d');

        if (!ctx) {
          break;
        }

        ctx.drawImage(canvas, 0, startY, imgWidth, sliceHeight, 0, 0, imgWidth, sliceHeight);

        const sliceHeightPx = (sliceHeight * usableWidth) / imgWidth;
        pdf.addImage(sliceCanvas.toDataURL('image/png'), 'PNG', margin, margin + currentPageHeight, usableWidth, sliceHeightPx);

        currentPageHeight = sliceHeightPx;
        startY += sliceHeight;
      }

      return currentPageHeight;
    }

    if (currentPageHeight + renderedHeight + spacing > contentHeight) {
      pdf.addPage();
      currentPageHeight = 0;
    }

    pdf.addImage(
      canvas.toDataURL('image/png'),
      'PNG',
      margin,
      margin + currentPageHeight,
      usableWidth,
      renderedHeight
    );

    return currentPageHeight + renderedHeight + spacing;
  }

  private sanitizePdfClone(clonedDocument: Document): void {
    const unsupportedColorPattern = /(color-mix|oklch|lab\(|lch\(|color\()/i;

    clonedDocument.querySelectorAll<HTMLElement>('*').forEach((node) => {
      const tagName = node.tagName.toLowerCase();

      if (tagName === 'svg' || tagName === 'path' || tagName === 'polygon' || tagName === 'line' || tagName === 'circle' || tagName === 'text') {
        return;
      }

      const inlineStyle = node.getAttribute('style');
      if (inlineStyle && unsupportedColorPattern.test(inlineStyle)) {
        node.setAttribute(
          'style',
          inlineStyle
            .replace(/color-mix\([^;)]*(?:\)[^;)]*)?\)/gi, '#ffffff')
            .replace(/oklch\([^)]*\)/gi, '#000000')
            .replace(/lab\([^)]*\)/gi, '#000000')
            .replace(/lch\([^)]*\)/gi, '#000000')
            .replace(/color\([^)]*\)/gi, '#000000')
        );
      }

      node.style.backgroundImage = 'none';
      node.style.boxShadow = 'none';
      node.style.textShadow = 'none';
      node.style.outlineColor = '#d9deee';
      node.style.borderColor = '#d9deee';
    });

    clonedDocument.body.style.backgroundColor = '#ffffff';
    clonedDocument.body.style.color = '#000000';
  }

  async downloadPdf(): Promise<void> {
    this.exportingPdf = true;
    const report = document.getElementById('kundali-report');

    if (!report) {
      this.exportingPdf = false;
      return;
    }

    try {
      document.body.classList.add('pdf-mode');
      document.body.classList.add('kundali-layout');
      report.classList.add('pdf-mode');
      document.getElementById('divtabs')?.classList.add('displaynone');

      await new Promise(resolve => setTimeout(resolve, 400));

      const pdf = new jsPDF({
        orientation: 'p',
        unit: 'mm',
        format: 'a4',
        compress: true
      });

      const pdfWidth = pdf.internal.pageSize.getWidth();
      const pdfHeight = pdf.internal.pageSize.getHeight();

      const elementsToRender: HTMLElement[] = [];
      const header = document.getElementById('pdfHeader');
      if (header) {
        elementsToRender.push(header);
      }

      const chartSection = document.querySelector('#kundali-report > app-section-card') as HTMLElement | null;
      if (chartSection) {
        elementsToRender.push(chartSection);
      }

      const detailGrid = document.querySelector('#kundali-report > .detail-grid') as HTMLElement | null;
      if (detailGrid) {
        elementsToRender.push(detailGrid);
      }

      const yogaSection = document.getElementById('yogaSection');
      if (yogaSection) {
        elementsToRender.push(yogaSection);
      }

      const doshaSection = document.getElementById('doshaSection');
      if (doshaSection) {
        elementsToRender.push(doshaSection);
      }

      const planetPositionsSection = document.querySelector('#kundali-report > .results-sections > app-section-card.full-row') as HTMLElement | null;
      if (planetPositionsSection) {
        elementsToRender.push(planetPositionsSection);
      }
      const kundaliscore = document.querySelector('#kundali-report > .results-sections > .kundaliscore-grid') as HTMLElement | null;
      if (kundaliscore) {
        elementsToRender.push(kundaliscore);
      }

      const lowerDetailCards = document.querySelectorAll<HTMLElement>(
        '#kundali-report > .results-sections > .detail-grid > app-section-card'
      );
      lowerDetailCards.forEach(section => elementsToRender.push(section));

    

      const footer = document.getElementById('pdfFooter');
      if (footer) {
        elementsToRender.push(footer);
      }

      let pageHeightUsed = 0;
      for (const element of elementsToRender) {
        pageHeightUsed = await this.addElementToPdf(pdf, element, pdfWidth, pdfHeight, 5, pageHeightUsed, 3);
      }

      await pdf.save('kundali-report.pdf');
    }
    finally {
      document.getElementById('divtabs')?.classList.remove('displaynone');
      report.classList.remove('pdf-mode');
      document.body.classList.remove('pdf-mode');
      this.cdr.detectChanges();
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
    }
  }


﻿  private startJanmaStory(response?: any): void {
    const lines = this.buildJanmaStoryLines(response ?? this.kundaliResponse);
    this.clearJanmaStoryTimers();
    this.displayPersonName = this.formatPersonName(this.kundaliForm.value.fullName);
    this.janmaStoryLines = lines;
    this.displayedJanmaStoryLines = lines.map(() => '');
    this.janmaStoryHighlightTerms = this.buildJanmaStoryHighlightTerms(response ?? this.kundaliResponse);
    this.typeJanmaStoryLine(lines, 0, 0);
  }

  private clearJanmaStoryTimers(): void {
    this.janmaStoryTimers.forEach(timer => clearTimeout(timer));
    this.janmaStoryTimers = [];
  }

  getHighlightedStoryLine(line: string): string {
    let html = this.escapeHtml(line);
    const terms = [...this.janmaStoryHighlightTerms]
      .filter(term => term.value.length > 1)
      .sort((a, b) => b.value.length - a.value.length);

    terms.forEach(term => {
      const escapedTerm = this.escapeHtml(term.value);
      const pattern = new RegExp(`(${this.escapeRegExp(escapedTerm)})`, 'gi');
      html = html.replace(pattern, `<span class="story-inline-focus ${term.type}">$1</span>`);
    });

    return html;
  }

  isJanmaStoryComplete(): boolean {
    return this.janmaStoryLines.length > 0
      && this.displayedJanmaStoryLines.length === this.janmaStoryLines.length
      && this.displayedJanmaStoryLines.every((line, index) => line === this.janmaStoryLines[index]);
  }

  shouldShowStoryStar(storyIndex: number, line: string): boolean {
    if (!line) {
      return false;
    }

    const isTypingCurrentLine = line.length < (this.janmaStoryLines[storyIndex]?.length ?? 0);
    const isFinalLine = storyIndex === this.janmaStoryLines.length - 1;
    return isTypingCurrentLine || (isFinalLine && this.isJanmaStoryComplete());
  }

  private escapeHtml(value: string): string {
    return value
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  private escapeRegExp(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }

  private typeJanmaStoryLine(lines: string[], lineIndex: number, charIndex: number): void {
    if (lineIndex >= lines.length) {
      return;
    }

    if (charIndex <= lines[lineIndex].length) {
      this.displayedJanmaStoryLines[lineIndex] = lines[lineIndex].slice(0, charIndex);
      const timer = setTimeout(() => {
        this.typeJanmaStoryLine(lines, lineIndex, charIndex + 1);
        this.cdr.detectChanges();
      }, charIndex === 0 ? 260 : 18);
      this.janmaStoryTimers.push(timer);
      return;
    }

    const timer = setTimeout(() => this.typeJanmaStoryLine(lines, lineIndex + 1, 0), 360);
    this.janmaStoryTimers.push(timer);
  }

  private buildJanmaStoryLines(response: any): string[] {
    const personName = this.firstText(
      this.displayPersonName,
      this.formatPersonName(this.kundaliForm.value.fullName),
      this.storyText('janmaStory.unknownNative', {}, 'This native')
    );
    const ascendant = this.firstText(response?.summary?.ascendant, this.summary?.ascendant, this.d1Ascendant, this.storyText('janmaStory.risingSign', {}, 'the rising sign'));
    const rashi = this.firstText(response?.summary?.rashi, response?.summary?.moonSign, response?.summary?.chandraRashi, response?.rashi, response?.rashiName, this.summary?.rashi, this.summary?.moonSign, this.summary?.chandraRashi);
    const nakshatra = this.firstText(response?.summary?.nakshatra, response?.nakshatra, response?.nakshatraName, this.summary?.nakshatra);
    const pada = this.firstText(response?.summary?.pada, response?.summary?.nakshatraPada, response?.summary?.nakshatraPadaNumber, response?.pada, response?.nakshatraPada, this.summary?.pada, this.summary?.nakshatraPada);
    const akshar = this.firstText(response?.summary?.akshar, response?.summary?.akshara, response?.summary?.nameStartLetter, response?.summary?.luckySyllable, response?.akshar, response?.akshara);
    const birthSignature = [
      ascendant && `${this.storyText('janmaStory.lagnaLabel', {}, 'Lagna')} ${ascendant}`,
      rashi && `${this.storyText('janmaStory.moonLabel', {}, 'Moon')} ${rashi}`,
      nakshatra && `${nakshatra} ${this.storyText('janmaStory.nakshatraLabel', {}, 'Nakshatra')}`,
      pada && `${this.storyText('janmaStory.padaLabel', {}, 'Pada')} ${pada}`,
      akshar && `${this.storyText('janmaStory.aksharLabel', {}, 'Akshar')} ${akshar}`
    ].filter(Boolean).join(' | ');
    const strongestHouse = this.getStrongestHouseStoryLabel();
    const overallScore = this.getOverallScoreStoryLabel();
    const dominantStrengths = this.getDominantScoreFactors('strength').join(', ');
    const dominantWeaknesses = this.getDominantScoreFactors('weakness').join(', ');
    const planetStory = this.getPlanetStoryData();
    const leadingYoga = this.firstText(this.yogas?.[0]?.name);
    const leadingDosha = this.firstText(this.doshas?.[0]?.name);
    const currentDasha = this.getCurrentStoryDasha();
    const remedyLine = this.getRecommendedRemedyStoryLine(response);

    const opening = this.storyText('janmaStory.opening', { name: personName, ascendant }, `${personName}'s janma kundali opens with ${ascendant} rising, setting the first note of temperament, instinct and the way life is approached.`);
    const signatureLine = birthSignature
      ? this.storyText('janmaStory.signature', { signature: birthSignature }, `Main markers: ${birthSignature}. These are the fixed identity points of the janma kundali.`)
      : this.storyText('janmaStory.signatureFallback', {}, 'The main identity markers will appear here as Lagna, Moon sign, Nakshatra, Pada and starting Akshar become available.');
    const mindSignature = [rashi, nakshatra, pada && `Pada ${pada}`, akshar && `Akshar ${akshar}`].filter(Boolean).join(' and ');
    const mindLine = mindSignature
      ? this.storyText('janmaStory.mind', { signature: mindSignature }, `The inner nature carries ${mindSignature}, giving the story its emotional rhythm and recurring pattern of response.`)
      : this.storyText('janmaStory.mindFallback', {}, 'The Moon and nakshatra details add the emotional rhythm of the chart, showing how the person receives life before they act on it.');
    const scoreLine = overallScore || dominantStrengths || dominantWeaknesses
      ? this.storyText('janmaStory.score', {
          overallScore: overallScore || this.storyText('janmaStory.scoreStillOpening', {}, 'a developing score pattern'),
          strengths: dominantStrengths || this.storyText('common.none', {}, 'none'),
          weaknesses: dominantWeaknesses || this.storyText('common.none', {}, 'none')
        }, `The score pattern shows ${overallScore || 'a developing score pattern'}; strengths cluster around ${dominantStrengths || 'steady effort'}, while ${dominantWeaknesses || 'a few sensitive areas'} ask for conscious handling.`)
      : this.storyText('janmaStory.scoreFallback', {}, 'The score pattern is still opening, but it will soon show which life areas are naturally strong and which need patient correction.');
    const strengthLine = strongestHouse
      ? this.storyText('janmaStory.strength', { house: strongestHouse }, `A noticeable strength gathers around ${strongestHouse}, suggesting an area where effort can become visible progress.`)
      : this.storyText('janmaStory.strengthFallback', {}, 'The house pattern shows where life asks for steady effort, where support arrives, and where maturity gradually becomes strength.');
    const planetLine = planetStory.supportive || planetStory.challenging
      ? this.storyText('janmaStory.planetSupport', {
          supportivePlanets: planetStory.supportive || this.storyText('common.none', {}, 'none'),
          challengingPlanets: planetStory.challenging || this.storyText('common.none', {}, 'none')
        }, `${planetStory.supportive || 'Some planets'} act as supportive guides, while ${planetStory.challenging || 'a few placements'} ask for care, timing and cleaner choices.`)
      : this.storyText('janmaStory.planetFallback', {}, 'Planet support will become clearer as dignity, combustion and aspects are read together.');
    const karmicLine = leadingYoga || leadingDosha
      ? this.storyText('janmaStory.karmic', { karmicThemes: [leadingYoga, leadingDosha].filter(Boolean).join(' along with ') }, `The chart also carries ${[leadingYoga, leadingDosha].filter(Boolean).join(' along with ')}, adding a karmic subplot that deserves patient reading.`)
      : this.storyText('janmaStory.karmicFallback', {}, 'Yogas and doshas become the subplots of this chart, describing where grace, friction and turning points may appear.');
    const dashaLine = currentDasha
      ? this.storyText('janmaStory.dasha', { dasha: currentDasha }, `The current chapter is coloured by ${currentDasha}, so the same chart is now expressing itself through that planetary period.`)
      : this.storyText('janmaStory.dashaFallback', {}, 'The dasha sequence will tell which chapter of the story is active now, and which part of the chart is asking to be lived.');

    return [opening, signatureLine, mindLine, scoreLine, strengthLine, planetLine, karmicLine, dashaLine, remedyLine].filter(Boolean);
  }

  private getRecommendedRemedyStoryLine(response: any): string {
    const primaryRemedy = response?.remediesRecommendation?.primary ?? response?.RemediesRecommendation?.Primary;
    const remedyName = this.firstText(primaryRemedy?.name, primaryRemedy?.remedies?.gemstone?.primary?.name, primaryRemedy?.Remedies?.Gemstone?.Primary?.Name);

    if (!remedyName) {
      return '';
    }

    const planet = this.firstText(primaryRemedy?.planet);
    const confidence = Number(primaryRemedy?.confidence);
    const confidenceLabel = Number.isFinite(confidence) ? ` with ${Math.round(confidence)}/100 suitability` : '';
    const planetLabel = planet ? ` for ${planet} support` : '';
    return `Recommended remedy: ${remedyName}${planetLabel}${confidenceLabel}. Review the remedy section below before acting on it.`;
  }

  private storyText(key: string, params: Record<string, any> = {}, fallback: string): string {
    const translated = this.translocoService.translate(key, params);
    return translated && translated !== key ? translated : fallback;
  }

  private formatPersonName(value: any): string {
    return this.firstText(value)
      .replace(/\s+/g, ' ')
      .split(' ')
      .map(part => part ? `${part.charAt(0).toUpperCase()}${part.slice(1).toLowerCase()}` : '')
      .join(' ');
  }

  private getStrongestHouseStoryLabel(): string {
    const houses = Array.isArray(this.houseScores) ? this.houseScores : [];
    const strongest = houses
      .filter((house: any) => Number.isFinite(Number(house?.score)))
      .sort((a: any, b: any) => Number(b.score) - Number(a.score))[0];

    if (!strongest) {
      return '';
    }

    const translatedName = strongest.displayName ? this.translocoService.translate(strongest.displayName) : '';
    const displayName = translatedName && translatedName !== strongest.displayName ? translatedName : strongest.displayName;
    return displayName || `House ${strongest.houseNumber}`;
  }

  private getOverallScoreStoryLabel(): string {
    const scoreData: any = this.scores ?? {};
    const value = Array.isArray(scoreData)
      ? scoreData.find((item: any) => /overall/i.test(item?.name ?? ''))?.value
      : scoreData.overall ?? scoreData.overallScore ?? scoreData.totalScore;

    return Number.isFinite(Number(value)) ? `${Math.round(Number(value))}/100` : '';
  }

  private getDominantScoreFactors(type: 'strength' | 'weakness'): string[] {
    const scoreData: any = this.scores ?? {};
    const directKey = type === 'strength' ? 'dominantStrengths' : 'dominantWeaknesses';
    const factorKey = type === 'strength' ? 'growthFactors' : 'challengeFactors';
    const values = Array.isArray(scoreData?.[directKey])
      ? scoreData[directKey]
      : Array.isArray(scoreData?.[factorKey])
        ? scoreData[factorKey]
        : [];

    return values
      .map((value: any) => this.firstText(value?.label, value?.name, value?.description, value))
      .filter(Boolean)
      .slice(0, 2);
  }

  private getPlanetStoryData(): { supportive: string; challenging: string } {
    const planets = Array.isArray(this.planets) ? this.planets : [];
    const supportive = planets
      .filter(planet => this.getPlanetSupportStatus(planet) === 'supportive')
      .map(planet => this.translatePlanetName(this.firstText(planet?.planet, planet?.name)))
      .filter(Boolean)
      .slice(0, 3);
    const challenging = planets
      .filter(planet => this.getPlanetSupportStatus(planet) === 'challenging')
      .map(planet => this.translatePlanetName(this.firstText(planet?.planet, planet?.name)))
      .filter(Boolean)
      .slice(0, 3);

    return { supportive: supportive.join(', '), challenging: challenging.join(', ') };
  }

  private buildJanmaStoryHighlightTerms(response: any): Array<{ value: string; type: string }> {
    const personName = this.displayPersonName;
    const ascendant = this.firstText(response?.summary?.ascendant, this.summary?.ascendant, this.d1Ascendant);
    const moonSign = this.firstText(response?.summary?.rashi, response?.summary?.moonSign, response?.summary?.chandraRashi, response?.rashi, response?.rashiName, this.summary?.rashi, this.summary?.moonSign, this.summary?.chandraRashi);
    const nakshatra = this.firstText(response?.summary?.nakshatra, response?.nakshatra, response?.nakshatraName, this.summary?.nakshatra);
    const planetStory = this.getPlanetStoryData();
    const recommendedRemedy = this.firstText(response?.remediesRecommendation?.primary?.name, response?.remediesRecommendation?.primary?.remedies?.gemstone?.primary?.name);
    const focusItems: Array<{ value: string; type: string }> = [];

    if (personName) focusItems.push({ value: personName, type: 'person' });
    if (ascendant) focusItems.push({ value: ascendant, type: 'lagna' });
    if (moonSign) focusItems.push({ value: moonSign, type: 'moon' });
    if (nakshatra) focusItems.push({ value: nakshatra, type: 'nakshatra' });
    if (planetStory.supportive) this.splitStoryList(planetStory.supportive).forEach(value => focusItems.push({ value, type: 'supportive' }));
    if (planetStory.challenging) this.splitStoryList(planetStory.challenging).forEach(value => focusItems.push({ value, type: 'challenging' }));
    if (recommendedRemedy) focusItems.push({ value: recommendedRemedy, type: 'remedy' });

    return focusItems;
  }

  private splitStoryList(value: string): string[] {
    return value.split(',').map(item => item.trim()).filter(Boolean);
  }

  private getCurrentStoryDasha(): string {
    const now = new Date();
    const activeMahadasha = this.mahadashaPeriods?.find((period: any) => {
      const start = new Date(period.startDate ?? period.start ?? period.from);
      const end = new Date(period.endDate ?? period.end ?? period.to);
      return !Number.isNaN(start.getTime()) && !Number.isNaN(end.getTime()) && now >= start && now <= end;
    });

    if (!activeMahadasha) {
      return '';
    }

    const mahaName = this.firstText(activeMahadasha.planet, activeMahadasha.mahadasha);
    const activeAntar = activeMahadasha.antardashas?.find((period: any) => {
      const start = new Date(period.startDate ?? period.start ?? period.from);
      const end = new Date(period.endDate ?? period.end ?? period.to);
      return !Number.isNaN(start.getTime()) && !Number.isNaN(end.getTime()) && now >= start && now <= end;
    });
    const antarName = this.firstText(activeAntar?.subPlanet, activeAntar?.planet, activeAntar?.antardasha);

    return [mahaName && `${mahaName} Mahadasha`, antarName && `${antarName} Antardasha`].filter(Boolean).join(' and ');
  }

  getTopSudarshanHouses(sudarshan: any): any[] {
    return [...(sudarshan?.houseEmphasis ?? [])].sort((a: any, b: any) => Number(b?.score ?? 0) - Number(a?.score ?? 0)).slice(0, 3);
  }
  openHoraverse(): void {
    if (!this.kundaliResponse) { this.horaverseOpenError = 'Generate a Kundali before opening HoraVerse.'; return; }
    try {
      const sessionId = `hora-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      const session = { version: 1, createdAt: new Date().toISOString(), birthProfile: this.kundaliForm.getRawValue(), report: this.kundaliResponse, mahadashaPeriods: this.mahadashaPeriods, tone: 'mysterious-safe', theme: 'default-neuro-cosmic' };
      window.localStorage.setItem(`horaverse.session.${sessionId}`, JSON.stringify(session));
      window.localStorage.setItem('horaverse.latestSessionId', sessionId);
      this.horaverseOpenError = '';
      window.open(`/horaverse?session=${encodeURIComponent(sessionId)}`, '_blank');
    } catch {
      this.horaverseOpenError = 'HoraVerse could not open in this browser.';
    }
  }
}
