import { ChangeDetectorRef, Component, HostListener, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { KundaliService } from './kundali.service';
import { PageHeaderComponent } from "../../shared/components/page-header.component";
import { SectionCardComponent } from "../../shared/components/section-card.component";
import { firstValueFrom, of } from 'rxjs';
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
import { buildDashaPlanetScores } from "../../components/mahadasha-antardasha/dasha-period-tone";
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { HouseScoreOverview } from "../../components/house-score-overview/house-score-overview";
import { KundaliMetadataInsightsComponent } from "../../components/kundali-metadata-insights/kundali-metadata-insights.component";
import { TransitInsightComponent } from "../../components/transit-insight/transit-insight.component";
import { CurrentLifeFocusComponent } from "../../components/current-life-focus/current-life-focus.component";
import { mapDhruvaKundaliContext } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.mapper';
import { DoshaAttentionReport, rankDoshaAttention, rankYogaAttention } from './dosha-attention';
import { DhruvaKundaliContextService } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.service';
import { RemediesRecommendationComponent } from "../../components/remedies-recommendation/remedies-recommendation.component";
import {
  AstrologerBrandingSetting,
  AstrologerBrandingSettingsService
} from '../../core/services/astrologer-branding-settings.service';
import { NotificationService } from '../../shared/services/notification.service';
import { ThemeService } from '../../core/services/theme.service';
import { AuthService } from '../../core/auth/auth.service';
import { isPaidUser } from '../../core/auth/plan.util';
import { SubscriptionModalService } from '../../core/services/subscription-modal.service';
import { PortalNotesComponent } from '../../shared/components/portal-notes/portal-notes.component';
import { ResultFeedbackComponent } from '../../shared/components/result-feedback/result-feedback.component';
import { PortalWheelPickerComponent } from '../../shared/components/portal-wheel-picker.component';

type KundaliPdfMode = 'desktop' | 'mobile' | 'print-bw' | 'corporate';

interface ActivationGroupedItem {
  item: any;
  index: number;
  activation: any;
}

interface NatalGroupedItem {
  item: any;
  index: number;
}

interface ActivationPeriodGroup {
  title: string;
  period: string;
  dasha: string;
  trigger: string;
  isCurrent: boolean;
  items: ActivationGroupedItem[];
}

interface PredictionOutlookCard {
  lifeArea: string;
  tone: string;
  toneLabel: string;
  toneClass: string;
}

interface PredictionClaimParams {
  name: string;
  area: string;
  theme: string;
  planet: string;
  lord: string;
  house: string;
  houses: string;
  place: string;
  sourceHouse: string;
  targetHouse: string;
  lordPlacement: string;
  dashaSubject: string;
  level: string;
  period: string;
  periodSuffix: string;
}

interface PredictionUserStory {
  about: string;
  headline: string;
  meaning: string;
  now: string;
  scope: string;
  scopeKind: 'window' | 'chart';
  helps: string[];
  watch: string[];
  chartNote: string;
  fitTitle: string;
  direction: string;
  fields: string[];
}

interface PredictionAreaDetailCard extends PredictionOutlookCard {
  prediction: any | null;
  supportClaims: any[];
  challengeClaims: any[];
  mixedClaims: any[];
  dashaClaims: any[];
  natalClaims: any[];
  timingClaims: any[];
  houseConnections: any[];
  effectivePeriod: string;
  briefing: string[];
  story: PredictionUserStory;
}


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
    CurrentLifeFocusComponent,
    RemediesRecommendationComponent,
    PortalNotesComponent,
    PortalWheelPickerComponent,
    ResultFeedbackComponent
  ],
  templateUrl: './kundali.component.html',
  styleUrls: ['./kundali.component.scss', './kundali.component.exportpdf.scss']
})
export class KundaliComponent {

  @ViewChild(PortalNotesComponent) private portalNotes?: PortalNotesComponent;

constructor(private translocoService: TranslocoService,private cdr: ChangeDetectorRef) {}
  private fb = inject(FormBuilder);
  private kundaliService = inject(KundaliService);
  private dashaApiService = inject(DashaApiService);
  private locationService = inject(LocationService);
  private latestKundaliStorage = inject(LatestKundaliStorageService);
  private dhruvaKundaliContext = inject(DhruvaKundaliContextService);
  private apiTerms = inject(ApiTermTranslationService);
  private brandingSettingsService = inject(AstrologerBrandingSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly themeService = inject(ThemeService);
  private readonly authService = inject(AuthService);
  private readonly subscriptionModal = inject(SubscriptionModalService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  locationResults: any[] = [];
  kundaliResponse: any = null;
  brandingSettings: AstrologerBrandingSetting | null = null;
  houseScores: any[] = [];
  loading = false;
  showChart = false;
  showBirthDetailsForm = true;
  mahadashaPeriods: any[] = [];
  savingKundali = false;
  savedKundaliId: string | null = null;
  duplicateSavePrompt: {
    existingId: string;
    fullName: string;
    dateOfBirth: string;
    timeOfBirth: string;
    birthPlace: string;
  } | null = null;
  private openBirthKey: string | null = null;
  reportFeedbackReady = false;
  readonly kundaliFeedbackTopics = [
    { key: 'chart', label: 'Chart' },
    { key: 'prediction', label: 'Prediction' },
    { key: 'layout', label: 'Layout' },
  ];
  readonly reportFeedbackTopics = [
    { key: 'content', label: 'Report' },
    { key: 'delivery', label: 'Sending' },
  ];
  isNotesOpen = false;
  currentLifeFocusResetKey = 0;
  saveMessage = '';
  saveFailed = false;
  generationMessage = '';
  generationFailed = false;
  pdfDownloadMessage = '';
  pdfDownloadFailed = false;
  shareEmail = '';
  shareEmailTouched = false;
  shareMessage = '';
  shareFailed = false;
  shareSending = false;
  selectedEmailPdfMode: KundaliPdfMode = 'desktop';
  shareWhatsAppNumber = '';
  shareWhatsAppTouched = false;
  shareWhatsAppMessage = '';
  loadingSavedKundali = false;
  horaverseOpenError = '';
  isChartPinned = false;
  chartDashaExpanded = false;
  dashaSideboxOpen = false;
  showLagnaDegrees = false;
  showDivisionalDegrees = false;
  showGocharDegrees = false;
  displayPersonName = '';
  janmaStoryLines: string[] = [];
  displayedJanmaStoryLines: string[] = [];
  isJanmaStoryRailCollapsed = true;
  janmaStoryHighlightTerms: Array<{ value: string; type: string }> = [];
  private janmaStoryTimers: ReturnType<typeof setTimeout>[] = [];
  loaderZooming = false;
  loaderScoreReady = false;
  loaderLeaving = false;
  private kundaliLoaderTimer: ReturnType<typeof setTimeout> | null = null;
  private kundaliLoaderStartedAt = 0;
  /** Draw, fade the mark, reveal scores one by one, then fade. Delays match the loader CSS. */
  private readonly kundaliLoaderDrawMs = 1600;
  readonly loaderStageFadeMs = 520;
  readonly loaderScoreStepMs = 440;
  private readonly loaderScoreHoldMs = 720;
  private readonly kundaliLoaderFadeMs = 720;
  readonly predictionLifeAreas = [
    'Career',
    'Finance',
    'Relationships',
    'Education',
    'Family',
    'Property',
    'Health'
  ];
  planets: any[] = [];
  summary: any;
  scores: any[] = [];
  yogas: any[] = [];
  doshas: any[] = [];
  openYogaIndex: number | null = null;
  openDoshaIndex: number | null = null;
  activeReportSection = '';
  openPredictionLifeArea = 'Career';
  activeStrengthView: 'positions' | 'summary' | 'shadbala' | 'ashtakavarga' = 'positions';

  get advancedStrengthTables(): any | null {
    const raw = this.kundaliResponse?.advancedStrengthTables
      ?? this.kundaliResponse?.AdvancedStrengthTables
      ?? null;
    if (!raw) {
      return null;
    }

    return {
      methodNote: this.firstText(raw.methodNote, raw.MethodNote),
      planetaryStrengths: this.asStrengthRows(raw.planetaryStrengths ?? raw.PlanetaryStrengths),
      shadbala: this.resolveShadbalaRows(raw),
      houseSupport: this.asStrengthRows(raw.houseSupport ?? raw.HouseSupport)
    };
  }

  get hasShadbalaTable(): boolean {
    return (this.advancedStrengthTables?.shadbala?.length ?? 0) > 0;
  }

  private resolveShadbalaRows(raw: any): any[] {
    const virupaRows = this.asStrengthRows(
      raw.shadbala ?? raw.Shadbala ?? raw.shadBala ?? raw.ShadBala
    );
    if (virupaRows.length) {
      return virupaRows;
    }

    return this.asStrengthRows(raw.planetaryStrengths ?? raw.PlanetaryStrengths)
      .filter((row: any) => {
        const planet = this.firstText(row?.planet, row?.Planet).toLowerCase();
        return planet && planet !== 'rahu' && planet !== 'ketu';
      });
  }

  formatBalaCell(value: number | string | null | undefined): string {
    if (value == null || value === '') {
      return '—';
    }

    if (typeof value === 'string') {
      const trimmed = value.trim();
      if (!trimmed) {
        return '—';
      }
      if (!/^-?\d+(\.\d+)?$/.test(trimmed)) {
        return trimmed;
      }
      return Number(trimmed).toFixed(1);
    }

    return this.formatVirupa(value);
  }

  formatShadbalaPercent(row: any): string {
    const value = row?.percent ?? row?.Percent;
    return value == null || value === '' ? '—' : `${value}%`;
  }

  private asStrengthRows(value: unknown): any[] {
    return Array.isArray(value) ? value : [];
  }

  get hasReportStatus(): boolean {
    return !!(this.saveMessage || this.generationMessage || this.pdfDownloadMessage || this.shareMessage);
  }

  get currentYogaActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.yogas, 'current', 'Current Yoga Activation')
      : [];
  }

  get upcomingYogaActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.yogas, 'upcoming', 'Upcoming Yoga Activation')
      : [];
  }

  get natalYogaItems(): NatalGroupedItem[] {
    return this.getNatalOnlyItems(this.yogas);
  }

  get currentDoshaActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.doshas, 'current', 'Current Dosha Activation')
      : [];
  }

  get upcomingDoshaActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.doshas, 'upcoming', 'Upcoming Dosha Activation')
      : [];
  }

  get natalDoshaItems(): NatalGroupedItem[] {
    return this.getNatalOnlyItems(this.doshas);
  }

  get doshaAttention(): DoshaAttentionReport {
    return rankDoshaAttention(this.doshas, {
      formatPeriod: (activation) => this.formatActivationPeriod(activation),
      planet: (name) => this.translatePlanetName(name)
    });
  }

  get doshaQuietNames(): string {
    return this.doshaAttention.quiet.map((entry) => entry.name).join(', ');
  }

  get yogaAttention(): DoshaAttentionReport {
    return rankYogaAttention(this.yogas, {
      formatPeriod: (activation) => this.formatActivationPeriod(activation),
      planet: (name) => this.translatePlanetName(name)
    });
  }

  get yogaQuietNames(): string {
    return this.yogaAttention.quiet.map((entry) => entry.name).join(', ');
  }
  selectedHouseData: any;
  activeChartType: 'north' | 'south' | 'western' = 'north';
  activeDivisionalChart = 'D9';
  readonly divisionalChartOptions = [
    { id: 'D1', label: 'D1 Rashi' },
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
  d1Planets: any[] = [];
  d1Houses: any[] = [];
  d1Ascendant: string | undefined = '';
  d9Chart: any = null;
  divisionalCharts: any[] = [];
  today: Date = new Date();
  exportingPdf = false;
  readonly pdfModes: Array<{ mode: KundaliPdfMode; labelKey: string; descriptionKey: string }> = [
    {
      mode: 'desktop',
      labelKey: 'kundali.pdf.desktop',
      descriptionKey: 'kundali.pdf.desktopHint'
    },
    {
      mode: 'mobile',
      labelKey: 'kundali.pdf.mobile',
      descriptionKey: 'kundali.pdf.mobileHint'
    },
    {
      mode: 'print-bw',
      labelKey: 'kundali.pdf.print',
      descriptionKey: 'kundali.pdf.printHint'
    },
    {
      mode: 'corporate',
      labelKey: 'kundali.pdf.corporate',
      descriptionKey: 'kundali.pdf.corporateHint'
    }
  ];

  private readonly planetGlyphMap: Record<string, string> = {
    Sun: '\u2609',
    Moon: '\u263E',
    Mars: '\u2642',
    Mercury: '\u263F',
    Jupiter: '\u2643',
    Venus: '\u2640',
    Saturn: '\u2644',
    Rahu: 'Ra',
    Ketu: 'Ke'
  };
  private readonly rashiWatermarkMap: Record<string, { symbol: string; indianName: string; englishName: string }> = {
    aries: { symbol: '\u2648', indianName: 'Mesha', englishName: 'Aries' },
    mesha: { symbol: '\u2648', indianName: 'Mesha', englishName: 'Aries' },
    taurus: { symbol: '\u2649', indianName: 'Vrishabha', englishName: 'Taurus' },
    vrishabha: { symbol: '\u2649', indianName: 'Vrishabha', englishName: 'Taurus' },
    gemini: { symbol: '\u264A', indianName: 'Mithuna', englishName: 'Gemini' },
    mithuna: { symbol: '\u264A', indianName: 'Mithuna', englishName: 'Gemini' },
    cancer: { symbol: '\u264B', indianName: 'Karka', englishName: 'Cancer' },
    karka: { symbol: '\u264B', indianName: 'Karka', englishName: 'Cancer' },
    karkataka: { symbol: '\u264B', indianName: 'Karka', englishName: 'Cancer' },
    leo: { symbol: '\u264C', indianName: 'Simha', englishName: 'Leo' },
    simha: { symbol: '\u264C', indianName: 'Simha', englishName: 'Leo' },
    sinh: { symbol: '\u264C', indianName: 'Simha', englishName: 'Leo' },
    virgo: { symbol: '\u264D', indianName: 'Kanya', englishName: 'Virgo' },
    kanya: { symbol: '\u264D', indianName: 'Kanya', englishName: 'Virgo' },
    libra: { symbol: '\u264E', indianName: 'Tula', englishName: 'Libra' },
    tula: { symbol: '\u264E', indianName: 'Tula', englishName: 'Libra' },
    scorpio: { symbol: '\u264F', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrischika: { symbol: '\u264F', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrishchika: { symbol: '\u264F', indianName: 'Vrischika', englishName: 'Scorpio' },
    sagittarius: { symbol: '\u2650', indianName: 'Dhanu', englishName: 'Sagittarius' },
    dhanu: { symbol: '\u2650', indianName: 'Dhanu', englishName: 'Sagittarius' },
    capricorn: { symbol: '\u2651', indianName: 'Makara', englishName: 'Capricorn' },
    makara: { symbol: '\u2651', indianName: 'Makara', englishName: 'Capricorn' },
    aquarius: { symbol: '\u2652', indianName: 'Kumbha', englishName: 'Aquarius' },
    kumbha: { symbol: '\u2652', indianName: 'Kumbha', englishName: 'Aquarius' },
    pisces: { symbol: '\u2653', indianName: 'Meena', englishName: 'Pisces' },
    meena: { symbol: '\u2653', indianName: 'Meena', englishName: 'Pisces' },
    meen: { symbol: '\u2653', indianName: 'Meena', englishName: 'Pisces' },
  };

  ngOnInit(): void {
    this.loadBrandingSettings();
    this.syncActiveReportSectionFromHash();

    this.route.paramMap.subscribe(params => {
      const savedId = params.get('id');
      if (savedId) {
        this.loadSavedKundali(savedId);
      }
    });

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
        this.cdr.detectChanges();
      });
  }

  @HostListener('window:hashchange')
  onHashChange(): void {
    this.syncActiveReportSectionFromHash();
  }

  private syncActiveReportSectionFromHash(): void {
    this.activeReportSection = window.location.hash.replace('#', '');
    this.applyActiveReportQuickLink(this.activeReportSection);
  }

  private applyActiveReportQuickLink(sectionId: string): void {
    const quickLinks = Array.from(document.querySelectorAll('.report-quick-links a[data-section-id]'));

    quickLinks.forEach((link) => {
      const element = link as HTMLElement;
      const isActive = element.getAttribute('data-section-id') === sectionId;
      element.classList.toggle('is-active', isActive);

      if (isActive) {
        element.setAttribute('aria-current', 'location');
      } else {
        element.removeAttribute('aria-current');
      }
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

  get kundaliFeedbackContext(): string {
    if (this.savedKundaliId) {
      return this.savedKundaliId;
    }

    const value = this.kundaliForm.getRawValue();
    const key = [value.fullName, value.birthDate, value.birthTime, value.birthPlace]
      .map((item) => String(item || '').trim())
      .filter(Boolean)
      .join('|');
    return (key || 'kundali').slice(0, 200);
  }

  get showChartBranding(): boolean {
    return !!this.activeBranding?.showOnChartPage;
  }

  get showPdfBranding(): boolean {
    return !!this.activeBranding?.showOnPdfReport;
  }

  get showReportBranding(): boolean {
    return this.showChartBranding || this.showPdfBranding;
  }

  get reportLogoUrl(): string {
    return this.showReportBranding ? this.activeBranding?.logoUrl || 'assets/logo.png' : 'assets/logo.png';
  }

  get reportAstrologerName(): string {
    return this.showReportBranding
      ? this.activeBranding?.astrologerName || this.translocoService.translate('kundali.reportAuthorName')
      : this.translocoService.translate('kundali.reportAuthorName');
  }

  get reportAstrologerDesignation(): string {
    return this.showReportBranding
      ? this.activeBranding?.designation || this.translocoService.translate('kundali.reportAuthorRole')
      : this.translocoService.translate('kundali.reportAuthorRole');
  }


  get reportBrandLogoSrc(): string {
    return this.themeService.mode() === 'dark'
      ? 'assets/brand/horalife/horalife-icon-white.svg'
      : 'assets/brand/horalife/horalife-icon-color.svg';
  }
  get reportAstrologerContact(): string {
    if (!this.showReportBranding || !this.activeBranding) {
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
      surname: this.optionalText(value.surname),
      fatherName: this.optionalText(value.fatherName),
      motherName: this.optionalText(value.motherName),
      gotra: this.optionalText(value.gotra),
      gender: value.gender ?? '',
      birthDateTime: `${value.birthDate ?? ''}T${value.birthTime ?? '00:00'}:00`,
      birthplace: value.birthPlace ?? '',
      birthPlace: value.birthPlace ?? '',
      latitude: this.toNumber(value.latitude),
      longitude: this.toNumber(value.longitude),
      timeZoneOffset: 5.30
    };
  }

  get reportDisplayName(): string {
    const name = this.formatPersonName(this.kundaliForm.value.fullName);
    const surname = this.formatPersonName(this.kundaliForm.value.surname);
    if (!name && !surname) {
      return '-';
    }
    if (!surname || name.toLowerCase().endsWith(surname.toLowerCase())) {
      return name || surname;
    }
    return `${name} ${surname}`.trim();
  }

  get reportFamilyDetails(): Array<{ label: string; value: string }> {
    return [
      { label: this.translocoService.translate('kundali.fatherName'), value: this.formatPersonName(this.kundaliForm.value.fatherName) },
      { label: this.translocoService.translate('kundali.motherName'), value: this.formatPersonName(this.kundaliForm.value.motherName) },
      { label: this.translocoService.translate('kundali.gotra'), value: this.firstText(this.kundaliForm.value.gotra) }
    ].filter(detail => !!detail.value);
  }

  get currentLifeFocusSavePayload(): any | null {
    return this.kundaliResponse
      ? { ...this.buildKundaliPayload(), chartData: this.kundaliResponse }
      : null;
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

  private optionalText(value: unknown): string | undefined {
    const text = String(value ?? '').trim();
    return text || undefined;
  }

  private toNumber(value: unknown): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private currentBirthKey(): string {
    const value = this.kundaliForm.getRawValue();
    return [
      String(value.birthDate ?? '').slice(0, 10),
      String(value.birthTime ?? '').slice(0, 5),
      this.roundCoordinate(value.latitude),
      this.roundCoordinate(value.longitude)
    ].join('|');
  }

  private roundCoordinate(value: unknown): string {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed.toFixed(4) : '';
  }

  private rememberOpenBirthKey(): void {
    this.openBirthKey = this.currentBirthKey();
  }

  get isPremiumUser(): boolean {
    return isPaidUser(this.authService.getCurrentUser());
  }

  get premiumPredictionOutlook(): PredictionOutlookCard[] {
    const predictions = Array.isArray(this.kundaliResponse?.predictions)
      ? this.kundaliResponse.predictions
      : [];

    return this.predictionLifeAreas.map((lifeArea) => {
      const prediction = predictions.find((item: any) =>
        this.firstText(item?.lifeArea).toLowerCase() === lifeArea.toLowerCase()
      );
      const tone = this.firstText(this.readNestedProp(prediction, 'synthesis', 'overallTone'), 'Neutral');

      return {
        lifeArea,
        tone,
        toneLabel: this.getPredictionToneLabel(tone),
        toneClass: this.getPredictionToneClass(tone)
      };
    });
  }

  get predictionEffectivePeriod(): string {
    const predictions = Array.isArray(this.kundaliResponse?.predictions)
      ? this.kundaliResponse.predictions
      : [];
    const periodSource = predictions.find((item: any) =>
      this.firstText(item?.synthesis?.effectiveFrom) || this.firstText(item?.synthesis?.effectiveTo)
    );

    if (!periodSource) {
      return '\u2014';
    }

    const from = this.formatPredictionDate(periodSource?.synthesis?.effectiveFrom);
    const to = this.formatPredictionDate(periodSource?.synthesis?.effectiveTo);

    return [from, to].filter(Boolean).join(' - ');
  }

  getPredictionAreaLabel(lifeArea: string): string {
    const key = `kundali.predictions.area.${lifeArea}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : lifeArea;
  }

  getPredictionToneLabel(tone: string | null | undefined): string {
    const normalized = this.firstText(tone).toLowerCase();
    const key = normalized === 'supportive'
      ? 'kundali.predictions.plain.tone.help'
      : normalized === 'challenging'
        ? 'kundali.predictions.plain.tone.care'
        : normalized === 'mixed'
          ? 'kundali.predictions.plain.tone.mixed'
          : 'kundali.predictions.plain.tone.quiet';

    const translated = this.translocoService.translate(key);
    if (translated && translated !== key) {
      return translated;
    }

    return this.getPredictionToneFallback(normalized);
  }

  getPredictionToneClass(tone: string | null | undefined): string {
    const normalized = this.firstText(tone).toLowerCase();
    return ['supportive', 'mixed', 'challenging'].includes(normalized) ? normalized : 'neutral';
  }

  private getPredictionToneFallback(normalized: string): string {
    if (normalized === 'supportive') {
      return 'Helping now';
    }
    if (normalized === 'challenging') {
      return 'Go carefully';
    }
    if (normalized === 'mixed') {
      return 'Mixed now';
    }
    return 'Quiet now';
  }

  formatPredictionDate(value: string | null | undefined): string {
    const raw = this.firstText(value);
    if (!raw) {
      return '\u2014';
    }

    const parsed = new Date(raw);
    if (Number.isNaN(parsed.getTime())) {
      return raw;
    }

    return parsed.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  onUnlockPremium(): void {
    this.subscriptionModal.open();
  }

  get premiumPredictionAreaCards(): PredictionAreaDetailCard[] {
    const predictions = Array.isArray(this.kundaliResponse?.predictions)
      ? this.kundaliResponse.predictions
      : [];

    return this.predictionLifeAreas.map((lifeArea) => {
      const prediction = predictions.find((item: any) =>
        this.firstText(item?.lifeArea).toLowerCase() === lifeArea.toLowerCase()
      ) ?? null;
      const claims = Array.isArray(prediction?.claims) ? prediction.claims : [];
      const tone = this.firstText(this.readNestedProp(prediction, 'synthesis', 'overallTone'), 'Neutral');
      const dashaClaims = this.getPredictionDashaClaims(claims);
      const natalClaims = this.dedupePredictionClaims(this.getPredictionClaimsByStage(claims, 'natal'));
      const houseConnections = this.limitList(this.readNestedProp(prediction, 'synthesis', 'houseConnections'), 3);
      const card: PredictionAreaDetailCard = {
        lifeArea,
        prediction,
        tone,
        toneLabel: this.getPredictionToneLabel(tone),
        toneClass: this.getPredictionToneClass(tone),
        supportClaims: this.getPredictionClaimsByTone(claims, 'supportive'),
        challengeClaims: this.getPredictionClaimsByTone(claims, 'challenging'),
        mixedClaims: this.getPredictionClaimsByTone(claims, 'mixed'),
        dashaClaims,
        natalClaims,
        timingClaims: this.getPredictionTimingClaims(claims),
        houseConnections,
        effectivePeriod: this.formatPredictionPeriod(this.readNestedProp(prediction, 'synthesis', 'effectiveFrom'), this.readNestedProp(prediction, 'synthesis', 'effectiveTo')),
        briefing: [],
        story: {
          about: '',
          headline: '',
          meaning: '',
          now: '',
          scope: '',
          scopeKind: 'chart',
          helps: [],
          watch: [],
          chartNote: '',
          fitTitle: '',
          direction: '',
          fields: []
        }
      };
      card.briefing = this.buildPredictionBriefing(card);
      card.story = this.buildPredictionUserStory(card);
      return card;
    });
  }

  get selectedPredictionArea(): PredictionAreaDetailCard | null {
    return this.premiumPredictionAreaCards.find(area => this.isPredictionAreaOpen(area.lifeArea))
      ?? this.premiumPredictionAreaCards[0]
      ?? null;
  }

  selectPredictionArea(lifeArea: string): void {
    this.openPredictionLifeArea = lifeArea;
  }


  togglePredictionArea(lifeArea: string): void {
    this.selectPredictionArea(lifeArea);
  }

  isPredictionAreaOpen(lifeArea: string): boolean {
    return this.openPredictionLifeArea === lifeArea;
  }

  getPredictionClaimsByTone(claims: any[], tone: string): any[] {
    return this.dedupePredictionClaims(
      claims.filter((claim: any) =>
        this.firstText(this.readProp(claim, 'tone')).toLowerCase() === tone
        && !this.isPredictionTimingClaim(claim)
      )
    ).slice(0, 3);
  }

  getPredictionClaimsByStage(claims: any[], stage: string): any[] {
    return claims.filter((claim: any) =>
      this.firstText(this.readProp(claim, 'stage')).toLowerCase() === stage
    ).slice(0, 4);
  }

  getPredictionDashaClaims(claims: any[]): any[] {
    const levelOrder = ['mahadasha', 'antardasha', 'pratyantardasha'];
    const byLevel = new Map<string, any[]>();

    claims.forEach((claim: any) => {
      const level = this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase();
      const planet = this.firstText(this.readProp(claim, 'dashaPlanet'));

      if (!levelOrder.includes(level) || !planet) {
        return;
      }

      const existing = byLevel.get(level) ?? [];
      existing.push(claim);
      byLevel.set(level, existing);
    });

    return levelOrder
      .map(level => {
        const levelClaims = byLevel.get(level) ?? [];
        if (!levelClaims.length) {
          return null;
        }

        return {
          ...levelClaims[0],
          tone: this.getAggregatedPredictionTone(levelClaims)
        };
      })
      .filter(Boolean);
  }

  private getAggregatedPredictionTone(claims: any[]): string {
    const tones = new Set(
      claims
        .map((claim: any) => this.firstText(this.readProp(claim, 'tone')).toLowerCase())
        .filter(Boolean)
    );

    if (tones.has('mixed') || (tones.has('supportive') && tones.has('challenging'))) {
      return 'mixed';
    }

    if (tones.has('challenging')) {
      return 'challenging';
    }

    if (tones.has('supportive')) {
      return 'supportive';
    }

    return this.firstText(this.readProp(claims[0], 'tone'), 'Neutral');
  }

  getPredictionTimingClaims(claims: any[]): any[] {
    const seen = new Set<string>();

    return claims
      .filter((claim: any) => this.isPredictionTimingClaim(claim))
      .filter((claim: any) => {
        const stage = this.firstText(this.readProp(claim, 'stage')).toLowerCase();
        const identity = stage === 'dasha'
          ? [
            this.firstText(this.readProp(claim, 'key')),
            stage,
            this.firstText(this.readProp(claim, 'tone'))
          ].join('|').toLowerCase()
          : [
            this.firstText(this.readProp(claim, 'key')),
            stage,
            this.firstText(this.readProp(claim, 'startDate')),
            this.firstText(this.readProp(claim, 'endDate'))
          ].join('|').toLowerCase();

        if (seen.has(identity)) {
          return false;
        }

        seen.add(identity);
        return true;
      })
      .slice(0, 4);
  }

  isPredictionTimingClaim(claim: any): boolean {
    const stage = this.firstText(this.readProp(claim, 'stage')).toLowerCase();
    return stage === 'dasha' || stage === 'consolidated' || !!this.firstText(this.readProp(claim, 'dashaLevel')) || !!this.firstText(this.readProp(claim, 'dashaPlanet'));
  }

  getPredictionClaimLabel(claim: any): string {
    const key = this.firstText(this.readProp(claim, 'key'));
    if (!key) {
      return this.translocoService.translate('kundali.predictions.claim.default');
    }

    const params = this.getPredictionClaimParams(claim);
    const patternKey = this.getPredictionClaimPatternKey(key);
    if (patternKey) {
      const patterned = this.translocoService.translate(patternKey, params);
      if (patterned && patterned !== patternKey) {
        return patterned;
      }
    }

    const translationKey = 'kundali.predictions.claim.' + key;
    const translated = this.translocoService.translate(translationKey, params);
    if (translated && translated !== translationKey) {
      return translated;
    }

    return this.buildPredictionClaimFallback(key, params);
  }

  private buildPredictionBriefing(area: PredictionAreaDetailCard): string[] {
    const lines: string[] = [];
    const areaLabel = this.getPredictionAreaLabel(area.lifeArea);
    const name = this.formatPersonName(this.kundaliForm?.value?.fullName);
    const md = area.dashaClaims.find(claim =>
      this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase() === 'mahadasha'
    );
    const ad = area.dashaClaims.find(claim =>
      this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase() === 'antardasha'
    );
    const mdPlanet = this.translatePlanetName(this.readProp(md, 'dashaPlanet'));
    const adPlanet = this.translatePlanetName(this.readProp(ad, 'dashaPlanet'));
    const period = area.effectivePeriod
      || this.getPredictionClaimPeriod(ad)
      || this.getPredictionClaimPeriod(md);
    const windowParams = {
      name,
      area: areaLabel,
      md: mdPlanet,
      ad: adPlanet,
      period,
      periodSuffix: period ? ` (${period})` : '',
      running: ''
    };

    if (name && mdPlanet && adPlanet) {
      lines.push(this.storyText(
        'kundali.predictions.briefing.windowNamed',
        windowParams,
        `${name}, ${mdPlanet} Mahadasha / ${adPlanet} Antardasha${windowParams.periodSuffix} is the active ${areaLabel} window.`
      ));
    } else if (mdPlanet || adPlanet) {
      const running = [mdPlanet && `${mdPlanet} Mahadasha`, adPlanet && `${adPlanet} Antardasha`].filter(Boolean).join(' / ');
      lines.push(this.storyText(
        'kundali.predictions.briefing.window',
        { ...windowParams, running },
        `${running}${windowParams.periodSuffix} is the active ${areaLabel} window.`
      ));
    }

    const natalSupport = area.supportClaims[0];
    const natalChallenge = area.challengeClaims[0] || area.mixedClaims[0];
    const connection = area.houseConnections[0];
    const because = natalSupport
      ? this.getPredictionClaimLabel(natalSupport)
      : connection
        ? this.getHouseConnectionTitle(connection)
        : '';
    const toneLabel = area.toneLabel;

    if (because) {
      lines.push(this.storyText(
        'kundali.predictions.briefing.verdictBecause',
        { area: areaLabel, tone: toneLabel, because },
        `${areaLabel} looks ${toneLabel.toLowerCase()} in this window because ${this.lowerFirst(because)}`
      ));
    } else {
      lines.push(this.storyText(
        'kundali.predictions.briefing.verdict',
        { area: areaLabel, tone: toneLabel },
        `${areaLabel} looks ${toneLabel.toLowerCase()} from the current natal and Dasha factors.`
      ));
    }

    if (natalChallenge && natalChallenge !== natalSupport) {
      lines.push(this.getPredictionClaimLabel(natalChallenge));
    }

    if (connection && natalSupport) {
      const themes = this.getHouseConnectionThemes(connection);
      const title = this.getHouseConnectionTitle(connection);
      lines.push(this.storyText(
        'kundali.predictions.briefing.houseLink',
        { link: title, themes },
        themes ? `${title} ties ${themes}.` : title
      ));
    }

    return [...new Set(lines.filter(line => this.firstText(line)))].slice(0, 4);
  }

  private buildPredictionUserStory(area: PredictionAreaDetailCard): PredictionUserStory {
    const areaKey = this.firstText(area.lifeArea).toLowerCase() || 'career';
    const areaLabel = this.getPredictionAreaLabel(area.lifeArea);
    const tone = this.getPredictionToneClass(area.tone);
    const name = this.formatPersonName(this.kundaliForm?.value?.fullName);
    const md = area.dashaClaims.find(claim =>
      this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase() === 'mahadasha'
    );
    const ad = area.dashaClaims.find(claim =>
      this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase() === 'antardasha'
    );
    const mdPlanet = this.translatePlanetName(this.readProp(md, 'dashaPlanet'));
    const adPlanet = this.translatePlanetName(this.readProp(ad, 'dashaPlanet'));
    const period = area.effectivePeriod
      || this.getPredictionClaimPeriod(ad)
      || this.getPredictionClaimPeriod(md);
    const periodSuffix = period ? ` (${period})` : '';
    const connection = area.houseConnections[0];
    const sourceTheme = this.getConnectionSignificationLabels(this.readProp(connection, 'sourceSignificationKeys'), 1)[0] || areaLabel;
    const targetTheme = this.getConnectionSignificationLabels(this.readProp(connection, 'targetSignificationKeys'), 1)[0] || '';
    const linkPlanet = this.translatePlanetName(this.readProp(connection, 'lordPlanet'));

    const headline = this.storyText(
      `kundali.predictions.plain.headline.${tone}`,
      { area: areaLabel },
      tone === 'challenging'
        ? `${areaLabel} is under pressure in this period.`
        : tone === 'mixed'
          ? `${areaLabel} has both help and friction in this period.`
          : `${areaLabel} has more help than friction in this period.`
    );

    let meaning = this.storyText(
      `kundali.predictions.plain.meaning.${areaKey}.${tone}`,
      { name, area: areaLabel, planet: adPlanet || mdPlanet, sourceTheme, targetTheme, linkPlanet },
      this.getPredictionDefaultMeaning(areaKey, tone, areaLabel)
    );
    const pacePlanet = adPlanet || mdPlanet;
    if (pacePlanet && !meaning.toLowerCase().includes(pacePlanet.toLowerCase())) {
      meaning = `${meaning.replace(/\s+$/, '')} ${this.storyText(
        'kundali.predictions.plain.pacePlanet',
        { planet: pacePlanet },
        `${pacePlanet} is setting the pace.`
      )}`;
    }

    const windowPlanet = adPlanet || mdPlanet;
    const scopeKind: 'window' | 'chart' = windowPlanet && period ? 'window' : 'chart';
    const scope = scopeKind === 'window'
      ? this.storyText(
          'kundali.predictions.reading.window',
          { who: name ? `${name}, ` : '', area: areaLabel, planet: windowPlanet, period },
          `${name ? `${name}, this` : 'This'} ${areaLabel.toLowerCase()} outlook is for ${windowPlanet} (${period}). Notes marked from the birth chart stay true beyond these dates.`
        )
      : this.storyText(
          'kundali.predictions.reading.general',
          { who: name ? `${name}, ` : '', area: areaLabel },
          `${name ? `${name}, this` : 'This'} ${areaLabel.toLowerCase()} reading is general, from the birth chart.`
        );
    const now = '';

    const helps = this.collectPredictionPlainPoints(area.supportClaims, 'help', areaLabel, meaning)
      .slice(0, tone === 'mixed' ? 2 : 1);
    const watch = this.collectPredictionPlainPoints(
      [...area.challengeClaims, ...area.mixedClaims],
      'watch',
      areaLabel,
      `${meaning} ${headline}`
    ).slice(0, tone === 'mixed' ? 2 : 1);

    let chartNote = '';
    if (linkPlanet && targetTheme) {
      chartNote = this.storyText(
        'kundali.predictions.plain.chartNote',
        { planet: linkPlanet, sourceTheme, targetTheme, area: areaLabel },
        `${linkPlanet} connects ${sourceTheme.toLowerCase()} with ${targetTheme.toLowerCase()}, so ${areaLabel.toLowerCase()} results may show up through ${targetTheme.toLowerCase()}.`
      );
    } else if (helps.length && watch.length) {
      chartNote = this.storyText(
        'kundali.predictions.plain.chartNoteMixed',
        { area: areaLabel },
        `The birth chart and the current period do not fully agree, which is why ${areaLabel.toLowerCase()} looks mixed.`
      );
    }

    const areaFit = this.buildAreaFit(area);

    return {
      about: this.storyText(
        `kundali.predictions.plain.about.${areaKey}`,
        { area: areaLabel },
        this.getPredictionDefaultAbout(areaKey)
      ),
      headline,
      meaning,
      now,
      scope,
      scopeKind,
      helps,
      watch,
      chartNote,
      fitTitle: areaFit.fitTitle,
      direction: areaFit.direction,
      fields: areaFit.fields
    };
  }

  private buildAreaFit(area: PredictionAreaDetailCard): { fitTitle: string; direction: string; fields: string[] } {
    const lifeArea = this.firstText(area.lifeArea).toLowerCase();
    if (lifeArea === 'career') {
      return { fitTitle: this.fitTitle('career'), ...this.buildCareerFit(area) };
    }

    if (lifeArea === 'education') {
      return { fitTitle: this.fitTitle('education'), ...this.buildEducationFit(area) };
    }

    return { fitTitle: '', direction: '', fields: [] };
  }

  private fitTitle(lifeArea: 'career' | 'education'): string {
    return lifeArea === 'education'
      ? this.storyText('kundali.predictions.educationFit.title', {}, 'Suitable study')
      : this.storyText('kundali.predictions.careerFit.title', {}, 'Suitable work');
  }

  private buildEducationFit(area: PredictionAreaDetailCard): { direction: string; fields: string[] } {
    const fromClaims = this.educationFitFromClaims(area);
    if (fromClaims.direction || fromClaims.fields.length) {
      return fromClaims;
    }

    return this.educationFitFromChart();
  }

  private educationFitFromClaims(area: PredictionAreaDetailCard): { direction: string; fields: string[] } {
    const claims: any[] = Array.isArray(area.prediction?.claims) ? area.prediction.claims : [];
    const pathKey = this.firstText(this.readProp(
      claims.find((claim: any) => this.firstText(this.readProp(claim, 'key')).startsWith('education_path_')),
      'key'
    ));
    const fields = [...new Set(claims
      .map((claim: any) => this.firstText(this.readProp(claim, 'key')))
      .filter((key: string) => key.startsWith('education_field_'))
      .map((key: string) => this.educationFieldLabel(key.slice('education_field_'.length).toLowerCase()))
      .filter((label: string) => !!label))];

    return { direction: this.educationDirectionLabel(pathKey), fields };
  }

  private educationFitFromChart(): { direction: string; fields: string[] } {
    const planets: any[] = this.planets?.length ? this.planets : (this.kundaliResponse?.planets ?? []);
    const houses: any[] = this.houseScores?.length ? this.houseScores : (this.kundaliResponse?.houses ?? []);
    const planetByName = new Map<string, any>();
    planets.forEach((planet: any) => {
      const name = this.firstText(this.readProp(planet, 'planet')).toLowerCase();
      if (name && !planetByName.has(name)) {
        planetByName.set(name, planet);
      }
    });
    const houseByNumber = new Map<number, any>();
    houses.forEach((house: any) => {
      const number = Number(this.readProp(house, 'houseNumber'));
      if (Number.isFinite(number)) {
        houseByNumber.set(number, house);
      }
    });

    if ((!houseByNumber.has(4) && !houseByNumber.has(5)) || planetByName.size === 0) {
      return { direction: '', fields: [] };
    }

    const houseOf = (planet: any) => Number(this.readProp(planet, 'house'));
    const nameOf = (planet: any) => this.firstText(this.readProp(planet, 'planet')).toLowerCase();
    const lordOf = (houseNumber: number) => {
      const house = houseByNumber.get(houseNumber);
      const name = this.firstText(this.readProp(house, 'houseOwner') || this.readProp(house, 'houseSwamy')).toLowerCase();
      return planetByName.get(name);
    };

    let higher = 0;
    let skill = 0;
    const addLord = (sourceHouse: number, pointsFor: (house: number) => number, side: 'higher' | 'skill') => {
      const lord = lordOf(sourceHouse);
      if (!lord) {
        return;
      }
      const points = pointsFor(houseOf(lord));
      if (points <= 0) {
        return;
      }
      if (side === 'higher') {
        higher += points;
      } else {
        skill += points;
      }
    };

    addLord(5, house => house === 9 ? 3 : (house === 4 || house === 5 ? 2 : 0), 'higher');
    addLord(9, house => house === 4 || house === 5 ? 3 : (house === 9 ? 2 : 0), 'higher');
    addLord(4, house => house === 3 || house === 6 || house === 10 ? 3 : (house === 4 ? 2 : 0), 'skill');
    addLord(3, house => house === 4 || house === 5 ? 2 : 0, 'skill');

    planetByName.forEach(planet => {
      const house = houseOf(planet);
      const name = nameOf(planet);
      if (name === 'jupiter' && (house === 4 || house === 5 || house === 9)) {
        higher += 2;
      }
      if (name === 'mercury' && (house === 3 || house === 4 || house === 5 || house === 6 || house === 10)) {
        skill += 2;
      }
      if (name === 'mars' && (house === 3 || house === 4 || house === 5)) {
        skill += 1;
      }
    });

    const pathKey = Math.max(higher, skill) < 2
      ? ''
      : higher >= skill + 2
        ? 'education_path_higher'
        : skill >= higher + 2
          ? 'education_path_skill'
          : 'education_path_both';

    const weights = new Map<string, number>();
    const consider = (planet: any, weight: number) => {
      const name = nameOf(planet);
      if (!name || weight <= (weights.get(name) ?? 0)) {
        return;
      }
      weights.set(name, weight);
    };
    planetByName.forEach(planet => {
      if ([4, 5, 9].includes(houseOf(planet))) {
        consider(planet, 50);
      }
    });
    [4, 5, 9].forEach(houseNumber => {
      const lord = lordOf(houseNumber);
      if (lord) {
        consider(lord, 40);
      }
    });

    const fields = [...weights.entries()]
      .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
      .slice(0, 3)
      .map(([planet]) => this.educationFieldLabel(planet))
      .filter((label: string) => !!label);

    return { direction: this.educationDirectionLabel(pathKey), fields };
  }

  private educationDirectionLabel(pathKey: string): string {
    const directionFallbacks: Record<string, string> = {
      education_path_higher: 'The chart leans toward higher study, research, or teaching more than a short skill course.',
      education_path_skill: 'The chart leans toward practical skill and applied training more than a long academic path.',
      education_path_both: 'The chart can support both formal study and practical skill. The stronger path depends on which learning houses are active.'
    };
    return directionFallbacks[pathKey]
      ? this.storyText(`kundali.predictions.educationFit.${pathKey}`, {}, directionFallbacks[pathKey])
      : '';
  }

  private educationFieldLabel(planet: string): string {
    const fieldFallbacks: Record<string, string> = {
      sun: 'Administration, political study, and leadership',
      moon: 'Psychology, care, literature, and public learning',
      mars: 'Engineering, technical training, surgery, and sports',
      mercury: 'Languages, accounts, writing, commerce, and technology',
      jupiter: 'Law, teaching, philosophy, and higher knowledge',
      venus: 'Arts, design, music, and media',
      saturn: 'Structure, history, discipline, and long study',
      rahu: 'Research, foreign study, and unconventional subjects',
      ketu: 'Specialized research, mathematics, healing, and analysis'
    };
    const fallback = fieldFallbacks[planet];
    return fallback
      ? this.storyText(`kundali.predictions.educationFit.field.${planet}`, {}, fallback)
      : '';
  }

  private buildCareerFit(area: PredictionAreaDetailCard): { direction: string; fields: string[] } {
    if (this.firstText(area.lifeArea).toLowerCase() !== 'career') {
      return { direction: '', fields: [] };
    }

    const fromClaims = this.careerFitFromClaims(area);
    if (fromClaims.direction || fromClaims.fields.length) {
      return fromClaims;
    }

    return this.careerFitFromChart();
  }

  private careerFitFromClaims(area: PredictionAreaDetailCard): { direction: string; fields: string[] } {
    const claims: any[] = Array.isArray(area.prediction?.claims) ? area.prediction.claims : [];
    const pathKey = this.firstText(this.readProp(
      claims.find((claim: any) => this.firstText(this.readProp(claim, 'key')).startsWith('career_path_')),
      'key'
    ));
    const fields = [...new Set(claims
      .map((claim: any) => this.firstText(this.readProp(claim, 'key')))
      .filter((key: string) => key.startsWith('career_field_'))
      .map((key: string) => key.slice('career_field_'.length).toLowerCase())
      .map((planet: string) => this.careerFieldLabel(planet))
      .filter((label: string) => !!label))];

    return {
      direction: this.careerDirectionLabel(pathKey),
      fields
    };
  }

  private careerFitFromChart(): { direction: string; fields: string[] } {
    const planets: any[] = this.planets?.length ? this.planets : (this.kundaliResponse?.planets ?? []);
    const houses: any[] = this.houseScores?.length ? this.houseScores : (this.kundaliResponse?.houses ?? []);
    const planetByName = new Map<string, any>();
    planets.forEach((planet: any) => {
      const name = this.firstText(this.readProp(planet, 'planet')).toLowerCase();
      if (name && !planetByName.has(name)) {
        planetByName.set(name, planet);
      }
    });
    const houseByNumber = new Map<number, any>();
    houses.forEach((house: any) => {
      const number = Number(this.readProp(house, 'houseNumber'));
      if (Number.isFinite(number)) {
        houseByNumber.set(number, house);
      }
    });

    if (!houseByNumber.has(10) || planetByName.size === 0) {
      return { direction: '', fields: [] };
    }

    const houseOf = (planet: any) => Number(this.readProp(planet, 'house'));
    const nameOf = (planet: any) => this.firstText(this.readProp(planet, 'planet')).toLowerCase();
    const lordOf = (houseNumber: number) => {
      const house = houseByNumber.get(houseNumber);
      const name = this.firstText(this.readProp(house, 'houseOwner') || this.readProp(house, 'houseSwamy')).toLowerCase();
      return planetByName.get(name);
    };
    const service = new Set(['sun', 'saturn', 'mars']);
    const trade = new Set(['mercury', 'venus', 'jupiter']);
    let job = 0;
    let business = 0;

    const tenthLord = lordOf(10);
    if (tenthLord) {
      const house = houseOf(tenthLord);
      if (house === 6) {
        job += 3;
      } else if (house === 7) {
        business += 3;
      } else if (house === 3) {
        business += 2;
      } else if (house === 10 || house === 11) {
        job += 1;
        business += 1;
      }
      if (service.has(nameOf(tenthLord))) {
        job += 1;
      }
      if (trade.has(nameOf(tenthLord))) {
        business += 1;
      }
    }

    const addLord = (sourceHouse: number, pointsFor: (house: number) => number, side: 'job' | 'business') => {
      const lord = lordOf(sourceHouse);
      if (!lord) {
        return;
      }
      const points = pointsFor(houseOf(lord));
      if (points <= 0) {
        return;
      }
      if (side === 'job') {
        job += points;
      } else {
        business += points;
      }
    };

    addLord(6, house => house === 10 ? 3 : (house === 1 || house === 6 ? 2 : 0), 'job');
    addLord(7, house => house === 10 ? 3 : (house === 2 || house === 7 || house === 11 ? 2 : 0), 'business');
    addLord(3, house => house === 7 || house === 10 ? 2 : 0, 'business');

    planetByName.forEach(planet => {
      const house = houseOf(planet);
      const name = nameOf(planet);
      if (house === 10 && service.has(name)) {
        job += 2;
      }
      if (house === 10 && trade.has(name)) {
        business += 2;
      }
      if (house === 6 && service.has(name)) {
        job += 1;
      }
      if (house === 7 && trade.has(name)) {
        business += 2;
      }
      if ((house === 7 || house === 10) && name === 'rahu') {
        business += 1;
      }
    });

    const pathKey = Math.max(job, business) < 2
      ? ''
      : business >= job + 2
        ? 'career_path_business'
        : job >= business + 2
          ? 'career_path_job'
          : 'career_path_both';

    const weights = new Map<string, number>();
    const consider = (planet: any, weight: number) => {
      const name = nameOf(planet);
      if (!name || weight <= (weights.get(name) ?? 0)) {
        return;
      }
      weights.set(name, weight);
    };
    planetByName.forEach(planet => {
      if (houseOf(planet) === 10) {
        consider(planet, 50);
      }
    });
    if (tenthLord) {
      consider(tenthLord, 40);
    }
    planetByName.forEach(planet => {
      const house = houseOf(planet);
      const name = nameOf(planet);
      if ((house === 6 || house === 7) && (service.has(name) || trade.has(name) || name === 'moon' || name === 'rahu' || name === 'ketu')) {
        consider(planet, 20);
      }
    });

    const fields = [...weights.entries()]
      .sort((left, right) => right[1] - left[1] || left[0].localeCompare(right[0]))
      .slice(0, 3)
      .map(([planet]) => this.careerFieldLabel(planet))
      .filter((label: string) => !!label);

    return { direction: this.careerDirectionLabel(pathKey), fields };
  }

  private careerDirectionLabel(pathKey: string): string {
    const directionFallbacks: Record<string, string> = {
      career_path_job: 'The chart leans toward a job or service role more than independent business.',
      career_path_business: 'The chart leans toward business, trade, or self-led work more than a salaried job.',
      career_path_both: 'The chart can support both a job and a business. The stronger path depends on which career houses are active.'
    };
    return directionFallbacks[pathKey]
      ? this.storyText(`kundali.predictions.careerFit.${pathKey}`, {}, directionFallbacks[pathKey])
      : '';
  }

  private careerFieldLabel(planet: string): string {
    const fieldFallbacks: Record<string, string> = {
      sun: 'Administration, government, and leadership',
      moon: 'Public-facing work, care, hospitality, and liquids',
      mars: 'Engineering, defense, surgery, property, and sports',
      mercury: 'Communication, accounts, commerce, writing, and technology',
      jupiter: 'Teaching, law, advisory work, and guidance',
      venus: 'Arts, design, luxury, media, and hospitality',
      saturn: 'Structure, manufacturing, labor, and long service',
      rahu: 'Research, technology, foreign work, and unconventional fields',
      ketu: 'Specialized research, analysis, healing, and craft'
    };
    const fallback = fieldFallbacks[planet];
    return fallback
      ? this.storyText(`kundali.predictions.careerFit.field.${planet}`, {}, fallback)
      : '';
  }

  getPredictionTimeHint(claim: any): string {
    const level = this.firstText(this.readProp(claim, 'dashaLevel')).toLowerCase();
    if (level && level !== 'antardasha') {
      return '';
    }

    const tone = this.getPredictionToneClass(this.readProp(claim, 'tone'));
    const planet = this.translatePlanetName(this.readProp(claim, 'dashaPlanet')) || 'This planet';
    return this.storyText(
      `kundali.predictions.plain.time.${tone}`,
      { planet },
      tone === 'challenging'
        ? `${planet} asks you to go slower.`
        : tone === 'mixed'
          ? `${planet} brings both movement and delay.`
          : `${planet} is helping this area.`
    );
  }

  getPredictionChapterLabel(level: string | null | undefined): string {
    const normalized = this.firstText(level).toLowerCase();
    const key = normalized === 'mahadasha'
      ? 'kundali.predictions.plain.chapter.long'
      : normalized === 'antardasha'
        ? 'kundali.predictions.plain.chapter.current'
        : normalized === 'pratyantardasha'
          ? 'kundali.predictions.plain.chapter.near'
          : 'kundali.predictions.plain.chapter.now';
    return this.storyText(key, {}, this.getDashaLevelLabel(level));
  }

  private collectPredictionPlainPoints(
    claims: any[],
    kind: 'help' | 'watch',
    areaLabel: string,
    alreadySaid = ''
  ): string[] {
    const ranked = [...claims].sort((left, right) =>
      this.getPredictionPointRank(right) - this.getPredictionPointRank(left)
    );
    const seenFamilies = new Set<string>();
    const points: string[] = [];
    let said = this.normalizePredictionText(alreadySaid);

    for (const claim of ranked) {
      const family = this.getPredictionClaimFamily(this.firstText(this.readProp(claim, 'key')));
      if (!family || seenFamilies.has(family)) {
        continue;
      }

      const point = this.getPredictionPlainPoint(claim, kind, areaLabel);
      const normalizedPoint = this.normalizePredictionText(point);
      if (!point || this.predictionTextOverlaps(normalizedPoint, said)) {
        continue;
      }

      seenFamilies.add(family);
      points.push(point);
      said = `${said} ${normalizedPoint}`.trim();
    }

    return points;
  }

  private getPredictionClaimFamily(key: string): string {
    return key
      .toLowerCase()
      .replace(/_(support|pressure|mixed)$/i, '')
      .replace(/_dignity$/i, '')
      .replace(/_lord$/i, '');
  }

  private getPredictionPointRank(claim: any): number {
    const key = this.firstText(this.readProp(claim, 'key')).toLowerCase();
    const params = this.getPredictionClaimParams(claim);
    let rank = 0;
    if (params.planet) {
      rank += 4;
    }
    if (key.includes('_lord_')) {
      rank += 3;
    }
    if (key.includes('_dignity_')) {
      rank += 2;
    }
    if (key.includes('_dasha_')) {
      rank += 1;
    }
    return rank;
  }

  private normalizePredictionText(value: string): string {
    return this.firstText(value)
      .toLowerCase()
      .replace(/[^a-z0-9\s]+/g, ' ')
      .replace(/\b(needs|need|attention|care|patience|careful|caution|slow|slower|pressure|mixed|help|helping|support|supported)\b/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();
  }

  private predictionTextOverlaps(point: string, alreadySaid: string): boolean {
    if (!point) {
      return true;
    }
    if (!alreadySaid) {
      return false;
    }
    if (alreadySaid.includes(point)) {
      return true;
    }

    const pointWords = new Set(point.split(' ').filter(word => word.length > 3));
    if (!pointWords.size) {
      return true;
    }

    let shared = 0;
    for (const word of pointWords) {
      if (alreadySaid.includes(word)) {
        shared += 1;
      }
    }

    return shared / pointWords.size >= 0.55;
  }

  private getPredictionPlainPoint(claim: any, kind: 'help' | 'watch', areaLabel: string): string {
    const key = this.firstText(this.readProp(claim, 'key'));
    const params = this.getPredictionClaimParams(claim);
    const tone = this.getPredictionKeyTone(key) || (kind === 'watch' ? 'pressure' : 'support');
    const pattern = key.includes('_dasha_')
      ? 'dasha'
      : key.includes('_dignity_')
        ? 'dignity'
        : key.includes('_lord_')
          ? 'lord'
          : 'natal';
    const pointKey = `kundali.predictions.plain.point.${pattern}.${tone}`;
    const fallback = this.buildPredictionPlainPointFallback(pattern, tone, params, areaLabel);
    return this.storyText(pointKey, { ...params, area: areaLabel, theme: this.getPredictionPlainTheme(key) }, fallback);
  }

  private getPredictionPlainTheme(key: string): string {
    const family = key
      .replace(/_(support|pressure|mixed)$/i, '')
      .replace(/_dignity$/i, '')
      .replace(/_lord$/i, '');
    const themeKey = `kundali.predictions.plain.theme.${family}`;
    const translated = this.translocoService.translate(themeKey);
    if (translated && translated !== themeKey) {
      return translated;
    }

    return this.getPredictionClaimTheme(key);
  }

  private buildPredictionPlainPointFallback(
    pattern: string,
    tone: string,
    params: PredictionClaimParams,
    areaLabel: string
  ): string {
    const planet = params.planet || params.lord || 'This planet';
    const theme = params.theme || areaLabel.toLowerCase();

    if (pattern === 'dasha') {
      if (tone === 'pressure') {
        return `${planet}'s current period can slow or test ${areaLabel.toLowerCase()}.`;
      }
      if (tone === 'mixed') {
        return `${planet}'s current period is mixed for ${areaLabel.toLowerCase()} — some movement, some delay.`;
      }
      return `${planet}'s current period is helping ${areaLabel.toLowerCase()}.`;
    }

    if (pattern === 'lord') {
      if (tone === 'pressure') {
        return `${planet} is placed in a way that can delay ${theme}.`;
      }
      if (tone === 'mixed') {
        return `${planet} is placed in a way that both helps and complicates ${theme}.`;
      }
      return `${planet} is placed in a way that helps ${theme} show in real life.`;
    }

    if (pattern === 'dignity') {
      return tone === 'pressure'
        ? `${planet} is not at full strength for ${theme}.`
        : `${planet} is strong enough to help ${theme}.`;
    }

    if (tone === 'pressure') {
      return `Your birth chart asks for patience with ${theme}.`;
    }
    if (tone === 'mixed') {
      return `Your birth chart gives mixed signals for ${theme}.`;
    }
    return `Your birth chart supports ${theme}.`;
  }

  private getPredictionDefaultAbout(areaKey: string): string {
    switch (areaKey) {
      case 'finance':
        return 'Money in, savings, and day-to-day financial decisions.';
      case 'relationships':
        return 'Partnership, marriage, and close one-to-one bonds.';
      case 'education':
        return 'Study, skills, exams, and how easily learning sticks.';
      case 'family':
        return 'Home, parents, and family support.';
      case 'property':
        return 'House, land, and your living situation.';
      case 'health':
        return 'Energy, body, and daily vitality.';
      default:
        return 'Work, reputation, and how your effort is seen.';
    }
  }

  private getPredictionDefaultMeaning(areaKey: string, tone: string, areaLabel: string): string {
    if (tone === 'challenging') {
      return `${areaLabel} may feel slower or heavier now. Protect what you already have, finish open work, and avoid rushed moves.`;
    }
    if (tone === 'mixed') {
      return `${areaLabel} has both lift and friction. Lean on what is already working, and stay patient where results lag.`;
    }
    return `${areaLabel} has more backing than usual. This is a better window to be visible, take responsibility, and let effort be noticed.`;
  }

  private getPredictionClaimParams(claim: any): PredictionClaimParams {
    const key = this.firstText(this.readProp(claim, 'key'));
    const evidence = this.readPredictionEvidenceList(claim);
    let sourceHouse = '';
    let targetHouse = '';
    let lordPlanet = '';
    const houses: string[] = [];
    let evidencePlanet = '';

    for (const item of evidence) {
      const lordMatch = item.match(/^house-lord:(\d+):([^:]+):house:(\d+)$/i);
      if (lordMatch) {
        sourceHouse = lordMatch[1];
        lordPlanet = lordMatch[2];
        targetHouse = lordMatch[3];
        houses.push(lordMatch[1], lordMatch[3]);
        continue;
      }

      const houseMatch = item.match(/^house:(\d+)/i);
      if (houseMatch) {
        houses.push(houseMatch[1]);
      }

      const planetMatch = item.match(/^planet:([^:]+)$/i);
      if (planetMatch) {
        evidencePlanet = planetMatch[1];
      }
    }

    const dashaPlanet = this.firstText(this.readProp(claim, 'dashaPlanet'), evidencePlanet);
    const planetName = this.translatePlanetName(this.firstText(lordPlanet, dashaPlanet, evidencePlanet));
    const uniqueHouses = [...new Set(houses.filter(Boolean))].map(house => this.formatOrdinalHouse(house));
    const period = this.getPredictionClaimPeriod(claim);
    const areaKey = key.split('_')[0] || '';
    const areaName = this.predictionLifeAreas.find(area => area.toLowerCase() === areaKey)
      ?? areaKey;
    const sourceLabel = sourceHouse ? this.formatOrdinalHouse(sourceHouse) : '';
    const targetLabel = targetHouse ? this.formatOrdinalHouse(targetHouse) : '';
    const lordName = this.translatePlanetName(lordPlanet) || planetName;
    const housesLabel = uniqueHouses.join(', ');
    const lordPlacement = lordName && sourceLabel && targetLabel
      ? `${lordName} as ${sourceLabel} lord in the ${targetLabel}`
      : lordName;
    const dashaSubject = [
      this.getDashaLevelLabel(this.readProp(claim, 'dashaLevel')),
      planetName && `of ${planetName}`
    ].filter(Boolean).join(' ') || 'Current Dasha';

    return {
      name: this.formatPersonName(this.kundaliForm?.value?.fullName),
      area: this.getPredictionAreaLabel(areaName),
      theme: this.getPredictionClaimTheme(key),
      planet: planetName,
      lord: lordName,
      house: uniqueHouses[0] || '',
      houses: housesLabel,
      place: housesLabel ? ` around ${housesLabel}` : '',
      sourceHouse: sourceLabel,
      targetHouse: targetLabel,
      lordPlacement,
      dashaSubject,
      level: this.getDashaLevelLabel(this.readProp(claim, 'dashaLevel')),
      period,
      periodSuffix: period ? ` (${period})` : ''
    };
  }

  private getPredictionClaimPatternKey(key: string): string {
    const tone = this.getPredictionKeyTone(key);
    if (!tone) {
      return '';
    }

    if (key.includes('_current_outlook_')) {
      return `kundali.predictions.sentence.outlook.${tone}`;
    }
    if (key.includes('_dasha_')) {
      return `kundali.predictions.sentence.dasha.${tone}`;
    }
    if (key.includes('_dignity_')) {
      return `kundali.predictions.sentence.dignity.${tone}`;
    }
    if (key.includes('_lord_')) {
      return `kundali.predictions.sentence.lord.${tone}`;
    }
    if (key.includes('_foundation_')) {
      return `kundali.predictions.sentence.foundation.${tone}`;
    }

    return `kundali.predictions.sentence.natal.${tone}`;
  }

  private getPredictionKeyTone(key: string): 'support' | 'pressure' | 'mixed' | '' {
    if (key.endsWith('_support')) {
      return 'support';
    }
    if (key.endsWith('_pressure')) {
      return 'pressure';
    }
    if (key.endsWith('_mixed')) {
      return 'mixed';
    }
    return '';
  }

  private getPredictionClaimTheme(key: string): string {
    const family = key
      .replace(/_(support|pressure|mixed)$/i, '')
      .replace(/_dignity$/i, '')
      .replace(/_lord$/i, '');
    const themeKey = `kundali.predictions.theme.${family}`;
    const translated = this.translocoService.translate(themeKey);
    if (translated && translated !== themeKey) {
      return translated;
    }

    return this.humanizePredictionKey(family);
  }

  private buildPredictionClaimFallback(key: string, params: PredictionClaimParams): string {
    const tone = this.getPredictionKeyTone(key);
    const area = params.area || this.humanizePredictionKey(key.split('_')[0] || 'chart');
    const planet = params.planet;
    const houses = params.houses;
    const theme = params.theme;
    const periodSuffix = params.periodSuffix || '';
    const level = params.level;

    if (key.includes('_dasha_')) {
      const subject = [level, planet && `of ${planet}`].filter(Boolean).join(' ') || 'Current Dasha';
      if (tone === 'pressure') {
        return `${subject}${periodSuffix} asks for caution in ${area}.`;
      }
      if (tone === 'mixed') {
        return `${subject}${periodSuffix} gives mixed factors for ${area}.`;
      }
      return `${subject}${periodSuffix} supports ${area}.`;
    }

    if (key.includes('_current_outlook_')) {
      if (tone === 'pressure') {
        return `Current ${area} outlook needs attention.`;
      }
      if (tone === 'mixed') {
        return `Current ${area} outlook is mixed.`;
      }
      return `Current ${area} outlook is supportive.`;
    }

    if (key.includes('_lord_')) {
      const lord = params.lord || planet || 'The area lord';
      const placement = params.sourceHouse && params.targetHouse
        ? `, ${params.sourceHouse} lord in the ${params.targetHouse}`
        : houses ? ` (${houses})` : '';
      if (tone === 'pressure') {
        return `${lord}${placement} needs careful handling for ${theme || area}.`;
      }
      if (tone === 'mixed') {
        return `${lord}${placement} gives mixed signals for ${theme || area}.`;
      }
      return `${lord}${placement} supports ${theme || area}.`;
    }

    const place = houses ? ` around ${houses}` : '';
    if (tone === 'pressure') {
      return `${theme || area}${place} needs patience.`;
    }
    if (tone === 'mixed') {
      return `${theme || area}${place} carries both support and pressure.`;
    }
    if (tone === 'support') {
      return `${theme || area}${place} is supported.`;
    }

    return this.humanizePredictionKey(key);
  }

  private readPredictionEvidenceList(claim: any): string[] {
    const value = this.readProp(claim, 'evidenceKeys') ?? this.readProp(claim, 'evidence');
    if (!Array.isArray(value)) {
      return [];
    }

    return value.map(item => this.firstText(item)).filter(Boolean);
  }

  private dedupePredictionClaims(claims: any[]): any[] {
    const seen = new Set<string>();

    return claims.filter(claim => {
      const family = this.firstText(this.readProp(claim, 'key'))
        .toLowerCase()
        .replace(/_(support|pressure|mixed)$/i, '')
        .replace(/_dignity$/i, '')
        .replace(/_lord$/i, '');
      const identity = family || this.firstText(this.readProp(claim, 'key')).toLowerCase();
      if (!identity || seen.has(identity)) {
        return false;
      }

      seen.add(identity);
      return true;
    });
  }

  private lowerFirst(value: string): string {
    const text = this.firstText(value);
    if (!text) {
      return '';
    }

    return text.charAt(0).toLowerCase() + text.slice(1);
  }

  getPredictionStageLabel(stage: string | null | undefined): string {
    const normalized = this.firstText(stage).toLowerCase();
    const key = normalized === 'dasha'
      ? 'kundali.predictions.stage.dasha'
      : normalized === 'consolidated'
        ? 'kundali.predictions.stage.current'
        : 'kundali.predictions.stage.natal';
    return this.translocoService.translate(key);
  }

  getDashaLevelLabel(level: string | null | undefined): string {
    const normalized = this.firstText(level).toLowerCase();
    const key = normalized === 'mahadasha'
      ? 'kundali.predictions.dasha.mahadasha'
      : normalized === 'antardasha'
        ? 'kundali.predictions.dasha.antardasha'
        : normalized === 'pratyantardasha'
          ? 'kundali.predictions.dasha.pratyantardasha'
          : 'kundali.predictions.dasha.current';
    return this.translocoService.translate(key);
  }

  getPredictionClaimPeriod(claim: any): string {
    return this.formatPredictionPeriod(this.readProp(claim, 'startDate'), this.readProp(claim, 'endDate'));
  }

  getHouseConnectionTitle(connection: any): string {
    const sourceHouse = this.formatOrdinalHouse(this.readProp(connection, 'sourceHouse'));
    const targetHouse = this.formatOrdinalHouse(this.readProp(connection, 'targetHouse'));
    const lordPlanet = this.translatePlanetName(this.firstText(this.readProp(connection, 'lordPlanet')));
    return this.translocoService.translate('kundali.predictions.houseConnectionTitle', {
      sourceHouse,
      lordPlanet,
      targetHouse
    });
  }

  getHouseConnectionThemes(connection: any): string {
    const source = this.getConnectionSignificationLabels(this.readProp(connection, 'sourceSignificationKeys'), 1).join(', ');
    const target = this.getConnectionSignificationLabels(this.readProp(connection, 'targetSignificationKeys'), 1).join(', ');
    return [source, target].filter(Boolean).join(' -> ');
  }

  getHouseConnectionLabels(connection: any): string[] {
    return [
      ...this.getConnectionSignificationLabels(this.readProp(connection, 'sourceSignificationKeys'), 1),
      ...this.getConnectionSignificationLabels(this.readProp(connection, 'targetSignificationKeys'), 1)
    ].slice(0, 2);
  }

  private getConnectionSignificationLabels(value: any, count: number): string[] {
    return this.limitList(value, count)
      .map((item: any) => this.getHouseSignificationLabel(item))
      .filter(Boolean);
  }

  getHouseSignificationLabel(value: unknown): string {
    const keyPart = this.firstText(value);
    if (!keyPart) {
      return '';
    }

    const key = 'kundali.predictions.signification.' + keyPart;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : this.humanizePredictionKey(keyPart);
  }

  formatPredictionPeriod(from: string | null | undefined, to: string | null | undefined): string {
    const start = this.formatPredictionDate(from);
    const end = this.formatPredictionDate(to);
    return [start, end].filter(Boolean).join(' - ');
  }

  private formatOrdinalHouse(value: unknown): string {
    const houseNumber = Number(value);
    if (!Number.isInteger(houseNumber) || houseNumber < 1) {
      return this.translocoService.translate('kundali.predictions.house');
    }

    const suffix = houseNumber % 10 === 1 && houseNumber % 100 !== 11
      ? 'st'
      : houseNumber % 10 === 2 && houseNumber % 100 !== 12
        ? 'nd'
        : houseNumber % 10 === 3 && houseNumber % 100 !== 13
          ? 'rd'
          : 'th';

    return houseNumber + suffix;
  }

  private humanizePredictionKey(key: string): string {
    return key
      .replace(/^(career|finance|relationships|education|family|property|health)_/, '')
      .replace(/_/g, ' ')
      .replace(/\b10th\b/g, '10th')
      .replace(/\blord\b/g, 'lord')
      .replace(/\bsupport\b/g, 'supportive factor')
      .replace(/\bpressure\b/g, 'attention point')
      .replace(/\bmixed\b/g, 'mixed factor')
      .replace(/\bdasha\b/g, 'current Dasha')
      .replace(/\bdignity\b/g, 'planet strength')
      .replace(/\b\w/g, char => char.toUpperCase());
  }


  readProp(source: any, key: string): any {
    if (!source || typeof source !== 'object') {
      return undefined;
    }

    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    return source[key] ?? source[pascalKey];
  }

  private readNestedProp(source: any, parentKey: string, childKey: string): any {
    return this.readProp(this.readProp(source, parentKey), childKey);
  }

  private limitList(value: any, count: number): any[] {
    return Array.isArray(value) ? value.slice(0, count) : [];
  }

  onCurrentLifeFocusSaved(event: { id: string; reusedExisting: boolean }): void {
    this.savedKundaliId = event.id;
    this.rememberOpenBirthKey();
    this.saveDraftNotesForSavedKundali(event.id);
    this.saveFailed = false;
    if (event.reusedExisting) {
      this.notifications.info(this.translocoService.translate('currentLifeFocus.usingSavedKundali'));
    } else {
      this.notifications.success(this.translocoService.translate('kundali.saved'));
    }
    window.dispatchEvent(new CustomEvent('kundali-saved'));
    this.cdr.detectChanges();
  }

  readonly unsavedChangesMessage = 'This Kundali chart has not been saved. Leave without saving it?';

  hasUnsavedChanges(): boolean {
    return !!this.kundaliResponse && !this.savedKundaliId;
  }

  @HostListener('window:beforeunload', ['$event'])
  warnBeforeLeavingWithUnsavedKundali(event: BeforeUnloadEvent): void {
    if (!this.hasUnsavedChanges()) {
      return;
    }

    event.preventDefault();
    event.returnValue = '';
  }
  get updatesOpenKundali(): boolean {
    return !!this.savedKundaliId && this.openBirthKey === this.currentBirthKey();
  }

  get saveActionLabelKey(): string {
    if (this.savingKundali) {
      return 'reportAction.saving';
    }

    return this.updatesOpenKundali || !this.savedKundaliId
      ? 'reportAction.save'
      : 'reportAction.saveAgain';
  }

  saveCurrentKundali(onDuplicate?: 'ask' | 'update' | 'create'): void {
    if (!this.kundaliResponse || this.savingKundali) { return; }
    const mode = onDuplicate ?? (this.updatesOpenKundali ? 'update' : 'ask');
    this.savingKundali = true;
    this.saveMessage = '';
    this.saveFailed = false;
    const payload = { ...this.buildKundaliPayload(), chartData: this.kundaliResponse, onDuplicate: mode };
    this.kundaliService.saveKundali(payload).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? saved?.kundaliId ?? this.savedKundaliId;
        this.rememberOpenBirthKey();
        this.duplicateSavePrompt = null;
        if (this.savedKundaliId) {
          this.saveDraftNotesForSavedKundali(this.savedKundaliId);
        }
        this.saveFailed = false;
        this.notifications.success(this.translocoService.translate(mode === 'update' ? 'kundali.duplicate.updated' : 'kundali.saved'));
        this.savingKundali = false;
        window.dispatchEvent(new CustomEvent('kundali-saved'));
        this.cdr.detectChanges();
      },
      error: (error) => {
        if (error?.error?.code === 'KundaliAlreadySaved') {
          this.duplicateSavePrompt = {
            existingId: String(error.error.existingId ?? ''),
            fullName: String(error.error.fullName ?? ''),
            dateOfBirth: String(error.error.dateOfBirth ?? ''),
            timeOfBirth: String(error.error.timeOfBirth ?? ''),
            birthPlace: String(error.error.birthPlace ?? '')
          };
          this.saveFailed = false;
          this.savingKundali = false;
          this.cdr.detectChanges();
          return;
        }

        const message = this.getSaveErrorMessage(error);
        this.saveFailed = true;
        this.notifications.error(message);
        this.savingKundali = false;
        this.cdr.detectChanges();
      }
    });
  }

  confirmDuplicateSave(mode: 'update' | 'create'): void {
    this.saveCurrentKundali(mode);
  }

  openSavedDuplicate(): void {
    const existingId = this.duplicateSavePrompt?.existingId;
    if (!existingId) {
      return;
    }

    this.duplicateSavePrompt = null;
    this.router.navigate(['/app/kundali/saved', existingId]);
  }

  dismissDuplicateSave(): void {
    if (this.savingKundali) {
      return;
    }

    this.duplicateSavePrompt = null;
  }

  openNotes(): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.isNotesOpen = true;
  }

  closeNotes(): void {
    this.isNotesOpen = false;
  }
  private saveDraftNotesForSavedKundali(kundaliId: string): void {
    if (!kundaliId || !this.portalNotes?.hasPendingDraft) {
      return;
    }

    this.portalNotes.saveForContext(kundaliId);
  }
  private getSaveErrorMessage(error: any): string {
    if (error?.error?.code === 'KundaliAlreadySaved') {
      return '';
    }

    if (typeof error?.error === 'string' && error.error.trim()) {
      return error.error;
    }

    if (typeof error?.error?.message === 'string' && error.error.message.trim()) {
      return error.error.message;
    }

    if (error?.status === 409) {
      return 'Saved Kundali limit reached. Please delete an old Kundali or ask admin to increase your limit.';
    }

    return 'Unable to save Kundali.';
  }
  get isShareEmailValid(): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.shareEmail.trim());
  }

  get showShareEmailError(): boolean {
    return this.shareEmailTouched && !this.isShareEmailValid;
  }

  onShareEmailInput(value: string): void {
    this.shareEmail = value;
    this.shareEmailTouched = true;
    this.shareMessage = '';
  }

  onEmailPdfModeChange(value: string): void {
    if (this.isKundaliPdfMode(value)) {
      this.selectedEmailPdfMode = value;
    }
  }

  private isKundaliPdfMode(value: string): value is KundaliPdfMode {
    return this.pdfModes.some(pdfMode => pdfMode.mode === value);
  }


  get normalizedShareWhatsAppNumber(): string {
    const cleaned = this.shareWhatsAppNumber.trim().replace(/[^\d+]/g, '');
    const digits = cleaned.startsWith('+') ? cleaned.slice(1) : cleaned;
    return digits.length === 10 ? `91${digits}` : digits;
  }

  get isShareWhatsAppValid(): boolean {
    return /^\d{10,15}$/.test(this.normalizedShareWhatsAppNumber);
  }

  get showShareWhatsAppError(): boolean {
    return this.shareWhatsAppTouched && !this.isShareWhatsAppValid;
  }

  onShareWhatsAppNumberInput(value: string): void {
    this.shareWhatsAppNumber = value;
    this.shareWhatsAppTouched = true;
    this.shareWhatsAppMessage = '';
  }



  @HostListener('document:click', ['$event'])
  closeReportPickersOnOutsideClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (target?.closest('.report-actions')) {
      return;
    }

    document
      .querySelectorAll<HTMLDetailsElement>('.report-actions details[open]')
      .forEach(picker => picker.removeAttribute('open'));
  }
  onReportPickerToggle(event: Event): void {
    const currentPicker = event.currentTarget as HTMLDetailsElement | null;
    if (!currentPicker?.open) {
      return;
    }

    document
      .querySelectorAll<HTMLDetailsElement>('.report-actions details[open]')
      .forEach(picker => {
        if (picker !== currentPicker) {
          picker.removeAttribute('open');
        }
      });
  }
  async shareReportOnWhatsApp(): Promise<void> {
    this.shareWhatsAppTouched = true;
    this.shareWhatsAppMessage = '';

    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    if (!this.isShareWhatsAppValid || this.shareSending) {
      return;
    }

    if (!this.kundaliResponse) {
      this.shareWhatsAppMessage = this.translocoService.translate('reportAction.generateBeforeShare');
      return;
    }

    const summary = this.buildWhatsAppSummary();
    const fileName = this.getPdfFilename(this.selectedEmailPdfMode);
    this.shareSending = true;
    this.exportingPdf = true;

    let pdfBlob: Blob;
    try {
      pdfBlob = await firstValueFrom(this.kundaliService.generateReportPdf(this.buildReportPdfPayload(this.selectedEmailPdfMode)));
    } catch (error) {
      console.error('WhatsApp report PDF error:', error);
      const message = await this.resolveReportDownloadError(error);
      this.shareWhatsAppMessage = message && message !== '\u2014'
        ? message
        : this.translocoService.translate('reportAction.pdfTimeout');
      return;
    } finally {
      this.shareSending = false;
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
    }

    await this.deliverWhatsAppPdf(pdfBlob, fileName, summary);
  }

  private async deliverWhatsAppPdf(pdfBlob: Blob, fileName: string, summary: string): Promise<void> {
    const file = new File([pdfBlob], fileName, { type: 'application/pdf' });
    const shareData: ShareData = { files: [file], text: summary, title: fileName };
    let canShareFile = false;

    try {
      canShareFile = typeof navigator.share === 'function' && !!navigator.canShare?.(shareData);
    } catch {
      canShareFile = false;
    }

    if (canShareFile) {
      try {
        await navigator.share(shareData);
        this.shareWhatsAppMessage = this.translocoService.translate('reportAction.shareSheetOpened');
        this.reportFeedbackReady = true;
        return;
      } catch (error) {
        if (error instanceof DOMException && error.name === 'AbortError') {
          return;
        }
      }
    }

    this.saveBlob(pdfBlob, fileName);
    const fallback = `${summary}\n\nThe PDF was downloaded. Attach that file in this chat.`;
    const url = `https://wa.me/${this.normalizedShareWhatsAppNumber}?text=${encodeURIComponent(fallback)}`;
    window.open(url, '_blank', 'noopener,noreferrer');
    this.shareWhatsAppMessage = this.translocoService.translate('reportAction.pdfDownloadedWhatsapp');
    this.reportFeedbackReady = true;
  }

  private buildWhatsAppSummary(): string {
    const reportName = this.reportDisplayName !== '-' ? this.reportDisplayName : 'Kundali';
    const summary = this.kundaliResponse?.summary ?? {};
    const moon = summary.moonSign || summary.MoonSign || '';
    const lagna = summary.ascendant || summary.Ascendant || '';
    const dasha = [summary.currentMahadasha || summary.CurrentMahadasha, summary.currentAntardasha || summary.CurrentAntardasha]
      .filter(Boolean)
      .join(' / ');
    const birthDate = this.kundaliForm.value.birthDate || '';

    return [
      `Kundali of ${reportName}.`,
      birthDate ? `Birth date: ${birthDate}` : '',
      lagna ? `Lagna: ${lagna}` : '',
      moon ? `Moon: ${moon}` : '',
      dasha ? `Current period: ${dasha}` : ''
    ].filter(Boolean).join('\n');
  }

  async sendReportByEmail(): Promise<void> {
    this.shareEmailTouched = true;
    this.shareMessage = '';
    this.shareFailed = false;

    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    if (!this.isShareEmailValid || this.shareSending) {
      return;
    }

    if (!this.kundaliResponse) {
      this.shareFailed = true;
      this.notifications.warning('Generate a report before sharing.');
      return;
    }

    this.shareSending = true;
    this.exportingPdf = true;

    try {
      const reportName = this.reportDisplayName !== '-' ? this.reportDisplayName : 'Kundali';
      const birthDate = this.kundaliForm.value.birthDate || '';
      const birthTime = this.kundaliForm.value.birthTime || '';
      const birthPlace = this.kundaliForm.value.birthPlace || '';
      const isCorporateReport = this.selectedEmailPdfMode === 'corporate';
      const reportFormatLabel = this.translocoService.translate(this.pdfModes.find(pdfMode => pdfMode.mode === this.selectedEmailPdfMode)?.labelKey || 'kundali.pageTitle');
      const birthDetails = [birthDate, birthTime, birthPlace].filter(Boolean).join(' | ');
      const subject = isCorporateReport
        ? `Personal Vedic Astrology Advisory Report for ${reportName}`
        : `${reportName} Kundali Report`;
      const message = [
        'Namaste,',
        `Please find attached the ${reportFormatLabel} PDF report for ${reportName}.`,
        birthDetails ? `Birth details: ${birthDetails}` : '',
        this.savedKundaliId ? `Report ID: ${this.savedKundaliId}` : '',
        `Regards,\n${this.reportAstrologerName || 'HoraLife'}`
      ].filter(Boolean).join('\n\n');

      await firstValueFrom(this.kundaliService.sendReportEmail({
        ...this.buildReportPdfPayload(this.selectedEmailPdfMode),
        toEmail: this.shareEmail.trim(),
        subject,
        message
      }));

      this.shareFailed = false;
      this.reportFeedbackReady = true;
      this.notifications.success('Report email submitted for delivery.');
    } catch (error) {
      console.error('Report share email error:', error);
      this.shareFailed = true;
      this.notifications.error(await this.resolveReportEmailError(error));
    } finally {
      this.shareSending = false;
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
      this.cdr.detectChanges();
    }
  }

  private async resolveReportEmailError(error: unknown): Promise<string> {
    const fallbackMessage = 'Unable to send report email right now.';
    const errorName = error && typeof error === 'object' && 'name' in error ? String(error.name) : '';

    if (errorName === 'TimeoutError') {
      return 'Report email is taking longer than expected. Please try again in a moment.';
    }

    if (!error || typeof error !== 'object') {
      return fallbackMessage;
    }

    const status = 'status' in error ? Number((error as { status?: unknown }).status) : 0;
    const responseError = 'error' in error ? (error as { error?: unknown }).error : null;
    const backendMessage = await this.extractReportErrorMessage(responseError);

    if (backendMessage) {
      return backendMessage;
    }

    switch (status) {
      case 0:
        return 'Unable to reach the report email service. Check the API connection and try again.';
      case 400:
        return 'The report email request is incomplete. Please check the email address and try again.';
      case 401:
      case 403:
        return 'Please sign in again before sending the report email.';
      case 413:
        return 'The report PDF is too large to send by email. Please download it instead.';
      case 429:
        return this.buildReportRateLimitMessage(error);
      case 502:
        return 'The email provider could not send the report right now. Please verify SMTP settings or try again later.';
      case 503:
        return 'Report email service is not configured yet.';
      case 504:
        return 'Report email timed out while contacting the email server. Please try again.';
      default:
        return fallbackMessage;
    }
  }
  private buildReportRateLimitMessage(error: unknown): string {
    return 'Too many report requests. Please wait ' + this.formatRetryAfter(error) + ' and try again.';
  }

  private formatRetryAfter(error: unknown): string {
    const retryAfterSeconds = this.getRetryAfterSeconds(error);

    if (!retryAfterSeconds) {
      return 'a few minutes';
    }

    if (retryAfterSeconds < 60) {
      return `${retryAfterSeconds} second${retryAfterSeconds === 1 ? '' : 's'}`;
    }

    const minutes = Math.ceil(retryAfterSeconds / 60);
    return `${minutes} minute${minutes === 1 ? '' : 's'}`;
  }

  private getRetryAfterSeconds(error: unknown): number | null {
    if (!error || typeof error !== 'object' || !('headers' in error)) {
      return null;
    }

    const headers = (error as { headers?: { get?: (name: string) => string | null } }).headers;
    const retryAfter = headers?.get?.('Retry-After');
    const seconds = Number(retryAfter);

    return Number.isFinite(seconds) && seconds > 0 ? Math.ceil(seconds) : null;
  }

  private async extractReportErrorMessage(responseError: unknown): Promise<string> {
    if (!responseError) {
      return '';
    }

    if (responseError instanceof Blob) {
      const message = await this.readReportErrorBlob(responseError, '');
      return message;
    }

    if (typeof responseError === 'string') {
      return responseError.trim();
    }

    if (typeof responseError === 'object') {
      const candidate = responseError as { message?: unknown; title?: unknown; errors?: unknown };
      const message = String(candidate.message || candidate.title || '').trim();

      if (message) {
        return message;
      }

      if (candidate.errors && typeof candidate.errors === 'object') {
        const firstError = Object.values(candidate.errors as Record<string, unknown>)[0];
        if (Array.isArray(firstError) && firstError.length) {
          return String(firstError[0]);
        }
      }
    }

    return '';
  }

  toggleChartPin(): void { this.isChartPinned = !this.isChartPinned; }

  toggleChartDashaExpanded(): void {
    this.chartDashaExpanded = !this.chartDashaExpanded;
    if (this.chartDashaExpanded) {
      this.dashaSideboxOpen = false;
    }
  }

  toggleDashaSidebox(): void {
    if (this.chartDashaExpanded) {
      return;
    }

    this.dashaSideboxOpen = !this.dashaSideboxOpen;
  }

  toggleLagnaDegrees(): void { this.showLagnaDegrees = !this.showLagnaDegrees; }
  toggleDivisionalDegrees(): void { this.showDivisionalDegrees = !this.showDivisionalDegrees; }

  toggleGocharDegrees(): void { this.showGocharDegrees = !this.showGocharDegrees; }

  scrollToReportSection(event: MouseEvent, sectionId: string): void {
    event.preventDefault();

    const target = document.getElementById(sectionId);

    if (!target) {
      return;
    }

    const nextUrl = `${window.location.pathname}${window.location.search}#${sectionId}`;
    window.history.replaceState(window.history.state, '', nextUrl);
    this.activeReportSection = sectionId;
    this.applyActiveReportQuickLink(sectionId);
    target.scrollIntoView({ behavior: 'auto', block: 'start' });
  }

  selectedCoordinates: any = null;

  selectLocation(place: any): void {

    this.kundaliForm.patchValue({
      birthPlace: place.displayName,
      latitude: place.latitude,
      longitude: place.longitude
    });

    this.selectedCoordinates = {
      latitude: place.latitude,
      longitude: place.longitude
    };

    this.locationResults = [];

    console.log(this.selectedCoordinates);
  }
  kundaliForm = this.fb.group({
    fullName: ['', Validators.required],
    surname: [''],
    fatherName: [''],
    motherName: [''],
    gotra: [''],
    gender: ['', Validators.required],
    birthDate: ['', Validators.required],
    birthTime: ['', Validators.required],
    birthPlace: ['', Validators.required],
    latitude: [''],
    longitude: ['']
  });

  formatBirthDate(value: string | null | undefined): string {
    if (!value) {
      return '\u2014';
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

    const normalized = value.replace(/^strength\./, '').trim();

    return normalized === 'Own Sign' ? 'OwnSign' : normalized;
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
    const normalized = this.normalizeStrength(value).toLowerCase();
    const isWeak = normalized.includes('weak')
      || normalized.includes('enemy')
      || normalized.includes('debilitated')
      || normalized.includes('poor')
      || normalized.includes('low');
    const isStrong = !isWeak && (
      normalized.includes('own')
      || normalized.includes('exalted')
      || normalized.includes('strong')
      || normalized.includes('high')
      || normalized.includes('good')
      || normalized.includes('excellent')
    );

    return {
      strong: isStrong,
      neutral: !isStrong && !isWeak && (
        normalized.includes('neutral')
        || normalized.includes('notapplicable')
        || normalized.includes('moderate')
        || normalized.includes('medium')
        || normalized.includes('average')
      ),
      weak: isWeak
    };
  }

  getStrengthToneColor(value: string | null | undefined): string | null {
    const normalized = (value ?? '').toLowerCase();
    if (!normalized) {
      return null;
    }

    if (['weak', 'low', 'poor', 'enemy', 'debilitated'].some(token => normalized.includes(token))) {
      return 'var(--theme-danger)';
    }

    if (['strong', 'high', 'good', 'excellent', 'own', 'exalted'].some(token => normalized.includes(token))) {
      return 'var(--theme-success)';
    }

    if (['moderate', 'medium', 'average', 'neutral'].some(token => normalized.includes(token))) {
      return 'var(--theme-warning)';
    }

    return null;
  }

  formatVirupa(value: number | string | null | undefined): string {
    if (value == null || value === '') {
      return '—';
    }

    const numeric = Number(value);
    return Number.isFinite(numeric) ? numeric.toFixed(1) : '—';
  }

  getPlanetGlyph(planet: string | null | undefined): string {
    return this.planetGlyphMap[planet ?? ''] ?? '\u2022';
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
  translateHouseName(houseNumber: unknown, fallback?: string): string {
    return this.translateHouseValue('name', houseNumber, fallback);
  }

  translateHouseMeaning(houseNumber: unknown, fallback?: string): string {
    return this.translateHouseValue('meaning', houseNumber, fallback);
  }

  private translateHouseValue(kind: 'name' | 'meaning', houseNumber: unknown, fallback?: string): string {
    const number = this.resolveHouseNumber(houseNumber);
    if (!Number.isInteger(number) || number < 1 || number > 12) {
      return fallback || '';
    }

    const key = `house.${kind}.${number}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : fallback || '';
  }

  private resolveHouseNumber(value: unknown): number {
    const direct = Number(value);
    if (Number.isInteger(direct)) {
      return direct;
    }

    const match = String(value ?? '').match(/\d+/);
    return match ? Number(match[0]) : 0;
  }
  translateControlledValue(group: 'strength' | 'severity' | 'tone' | 'activationLevel', value: unknown): string { return this.apiTerms.translateControlledValue(group, value); }
  translatePlanetList(value: any): string {
    const planets = this.normalizePlanetNameList(value);
    return planets.length ? planets.map(planet => this.translatePlanetName(planet)).join(', ') : '';
  }

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

  get primaryChartPlanets(): any[] {
    return this.d1Planets;
  }

  get primaryChartHouses(): any[] {
    return this.d1Houses;
  }

  get primaryChartAscendant(): string {
    return this.d1Ascendant ?? '';
  }

  get primaryChartTitle(): string {
    return 'D1 Rashi';
  }

  get showSelectedDivisionalChart(): boolean {
    return !this.exportingPdf;
  }

  get divisionalChartChoices(): Array<{ id: string; label: string }> {
    return this.divisionalChartOptions.filter(option => option.id !== 'D1');
  }

  get divisionalChartTitle(): string {
    return this.divisionalChartOptions.find(option => option.id === this.activeDivisionalChart)?.label ?? '';
  }

  get divisionalChartPlanets(): any[] {
    return this.selectedDivisionalPlanets;
  }

  get divisionalChartHouses(): any[] {
    return this.selectedDivisionalHouses;
  }

  get divisionalChartAscendant(): string {
    return this.selectedDivisionalAscendant;
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
    return this.firstText(
      insight?.natalAscendant,
      insight?.NatalAscendant,
      this.primaryChartAscendant
    );
  }

  get hasGocharChart(): boolean {
    return this.gocharPlanets.length > 0;
  }

  get gocharCalculatedAt(): string {
    const insight = this.gocharInsight;
    const raw = this.firstText(insight?.calculatedAtUtc, insight?.CalculatedAtUtc);
    return raw ? this.formatPredictionDate(raw) : '';
  }

  get currentMahadashaPeriods(): any[] {
    const now = new Date();
    const current = (this.mahadashaPeriods ?? []).find((period: any) => {
      const start = new Date(period.startDate ?? period.start ?? period.from);
      const end = new Date(period.endDate ?? period.end ?? period.to);
      return !Number.isNaN(start.getTime()) && !Number.isNaN(end.getTime()) && now >= start && now <= end;
    });

    return current ? [current] : [];
  }

  get dashaPlanetScores(): Record<string, number> {
    return buildDashaPlanetScores(this.advancedStrengthTables, this.planets);
  }

  setActiveDivisionalChart(chart: string): void {
    if (chart !== 'D1' && !this.findDivisionalChart(chart)) {
      return;
    }

    this.activeDivisionalChart = chart;
    this.setDefaultHouseSelection();
  }

  hasDivisionalChart(chart: string): boolean {
    return chart === 'D1' || !!this.findDivisionalChart(chart);
  }

  private get selectedDivisionalPlanets(): any[] {
    return this.findDivisionalChart(this.activeDivisionalChart)?.planets
      ?? this.findDivisionalChart(this.activeDivisionalChart)?.Planets
      ?? [];
  }

  private get selectedDivisionalHouses(): any[] {
    return this.findDivisionalChart(this.activeDivisionalChart)?.houses
      ?? this.findDivisionalChart(this.activeDivisionalChart)?.Houses
      ?? [];
  }

  private get selectedDivisionalAscendant(): string {
    const chart = this.findDivisionalChart(this.activeDivisionalChart);
    return chart?.ascendant ?? chart?.Ascendant ?? '';
  }

  private findDivisionalChart(chartType: string): any | null {
    const match = this.divisionalCharts.find(chart =>
      String(chart?.chartType ?? chart?.ChartType ?? '').toUpperCase() === chartType.toUpperCase());
    if (match) {
      return match;
    }

    return chartType.toUpperCase() === 'D9' ? this.d9Chart : null;
  }

  private readDivisionalCharts(response: any): any[] {
    const charts = response?.divisionalCharts ?? response?.DivisionalCharts;
    return Array.isArray(charts) ? charts : [];
  }

  generateKundali(): void {
    this.showChart = false;
    this.dhruvaKundaliContext.clearContext();
    this.selectedHouseData = null;
    this.activeDivisionalChart = 'D9';
    this.currentLifeFocusResetKey++;
    this.generationMessage = '';
    this.generationFailed = false;
    if (this.kundaliForm.invalid) {
      this.kundaliForm.markAllAsTouched();
      this.generationFailed = true;
      this.notifications.warning('Please complete the required birth details before generating the Kundali.');
      return;
    }

    this.clearKundaliLoaderTimer();
    this.loaderZooming = false;
    this.loaderScoreReady = false;
    this.loaderLeaving = false;
    this.kundaliLoaderStartedAt = Date.now();
    this.loading = true;
    const payload = this.buildKundaliPayload();
    this.kundaliService.generateKundali(payload)
      .subscribe({
        next: (response) => {
          console.log('Generated:', response);
          this.kundaliResponse = response;
          this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext({
            ...response,
            fullName: this.kundaliForm.value.fullName,
            dateOfBirth: this.kundaliForm.value.birthDate,
            birthPlace: this.kundaliForm.value.birthPlace,
          }), response);
          console.log(response);
console.log('scoreslog'  , this.kundaliResponse?.scores);
          this.planets = response.planets;
          this.d1Planets = response.planets ?? [];
          this.d1Houses = response.houses ?? [];
          this.summary = response.summary;
          this.d1Ascendant = response.summary?.ascendant;
          this.d9Chart = response.d9Chart ?? null;
          this.divisionalCharts = this.readDivisionalCharts(response);
          this.scores = response.scores;
          this.yogas = this.extractYogas(response);
          this.doshas = this.extractDoshas(response);
          this.openYogaIndex = null;
          this.openDoshaIndex = null;
          this.houseScores = response.houses;  
          this.kundaliResponse = response;
          this.showChart = true;
          this.showBirthDetailsForm = false;
          this.savedKundaliId = null;
          this.openBirthKey = null;
          this.duplicateSavePrompt = null;
          this.saveMessage = '';
          this.generationFailed = false;
          this.latestKundaliStorage.save(payload);
          this.prepareJanmaStory(response);
          this.setDefaultHouseSelection();
          this.revealKundaliAfterLoader();
          console.log('houseScoreslog'  , this.houseScores);
          console.log('houseScoreslog2'  ,  response.houses);
          console.log(this.planets);

          this.dashaApiService.getMahadashaPeriods(payload).subscribe({
            next: (mahaResponse) => {
              console.log('Mahadasha periods:', mahaResponse);
              this.mahadashaPeriods = mahaResponse;
              this.prepareJanmaStory(response);
              this.cdr.detectChanges();
              console.log('Mahadasha periods:', this.mahadashaPeriods);
            },
            error: (error) => {
              console.error('Mahadasha periods error:', error);
              this.generationFailed = true;
              this.notifications.warning('Kundali generated, but the Dasha timeline could not be loaded right now.');
              this.cdr.detectChanges();
            }
          });
        },
        error: (error) => {
          console.error('Kundali generation error:', error);
          this.resetGeneratedKundaliState();
          this.generationFailed = true;
          this.clearKundaliLoaderTimer();
          this.resolveKundaliGenerationError(error).then(message => {
            this.notifications.error(message);
            this.loading = false;
            this.cdr.detectChanges();
          });
        }
      });
  }
  get loaderScoreItems(): Array<{ label: string; value: string }> {
    const scores = this.scores as any;
    if (!scores) {
      return [];
    }

    if (Array.isArray(scores)) {
      return scores
        .filter((item: any) => item?.value != null && item.value !== '')
        .slice(0, 4)
        .map((item: any) => ({
          label: String(item.name ?? 'Score'),
          value: this.formatLoaderScore(item.value)
        }));
    }

    return (['stability', 'overall', 'growth', 'challenge'] as const)
      .filter(key => scores[key] != null && scores[key] !== '' && !Array.isArray(scores[key]))
      .map(key => ({
        label: key.charAt(0).toUpperCase() + key.slice(1),
        value: this.formatLoaderScore(scores[key])
      }));
  }

  private formatLoaderScore(value: unknown): string {
    const numeric = Number(value);
    if (!Number.isFinite(numeric)) {
      return String(value ?? '');
    }

    return Number.isInteger(numeric) ? String(numeric) : String(Math.round(numeric));
  }

  private revealKundaliAfterLoader(): void {
    const drawRemaining = this.kundaliLoaderDrawMs - (Date.now() - this.kundaliLoaderStartedAt);
    const startScores = () => {
      this.kundaliLoaderTimer = null;
      this.loaderZooming = false;
      const scoreCount = this.loaderScoreItems.length;
      this.loaderScoreReady = scoreCount > 0;
      this.cdr.detectChanges();
      const lastScoreStart = scoreCount > 0
        ? this.loaderStageFadeMs + (scoreCount - 1) * this.loaderScoreStepMs
        : 0;
      const hold = scoreCount > 0 ? lastScoreStart + 420 + this.loaderScoreHoldMs : 280;
      this.kundaliLoaderTimer = setTimeout(startFade, hold);
    };
    const startFade = () => {
      this.kundaliLoaderTimer = null;
      this.loaderZooming = false;
      this.loaderLeaving = true;
      this.cdr.detectChanges();
      this.kundaliLoaderTimer = setTimeout(() => {
        this.kundaliLoaderTimer = null;
        this.loaderScoreReady = false;
        this.loaderLeaving = false;
        this.loading = false;
        this.notifications.success('Kundali generated.');
        this.cdr.detectChanges();
      }, this.kundaliLoaderFadeMs);
    };

    if (drawRemaining <= 0) {
      startScores();
      return;
    }

    this.kundaliLoaderTimer = setTimeout(startScores, drawRemaining);
  }

  private clearKundaliLoaderTimer(): void {
    if (this.kundaliLoaderTimer) {
      clearTimeout(this.kundaliLoaderTimer);
      this.kundaliLoaderTimer = null;
    }
    this.loaderZooming = false;
    this.loaderScoreReady = false;
    this.loaderLeaving = false;
  }

  ngOnDestroy(): void {
    this.clearKundaliLoaderTimer();
  }

  private resetGeneratedKundaliState(): void {
    this.kundaliResponse = null;
    this.planets = [];
    this.d1Planets = [];
    this.d1Houses = [];
    this.summary = null;
    this.d1Ascendant = '';
    this.d9Chart = null;
    this.divisionalCharts = [];
    this.scores = [];
    this.yogas = [];
    this.doshas = [];
    this.houseScores = [];
    this.mahadashaPeriods = [];
    this.openYogaIndex = null;
    this.openDoshaIndex = null;
    this.showChart = false;
    this.showBirthDetailsForm = true;
    this.selectedHouseData = null;
    this.savedKundaliId = null;
    this.openBirthKey = null;
    this.duplicateSavePrompt = null;
    this.dhruvaKundaliContext.clearContext();
  }

  private async resolveKundaliGenerationError(error: unknown): Promise<string> {
    const fallbackMessage = 'Unable to generate Kundali right now.';
    const errorName = error && typeof error === 'object' && 'name' in error ? String(error.name) : '';

    if (errorName === 'TimeoutError') {
      return 'Kundali generation is taking longer than expected. Please try again in a moment.';
    }

    if (!error || typeof error !== 'object') {
      return fallbackMessage;
    }

    const status = 'status' in error ? Number((error as { status?: unknown }).status) : 0;
    const responseError = 'error' in error ? (error as { error?: unknown }).error : null;
    const backendMessage = await this.extractReportErrorMessage(responseError);

    if (backendMessage) {
      return backendMessage;
    }

    switch (status) {
      case 0:
        return 'Unable to reach the Kundali service. Check the API connection and try again.';
      case 400:
        return 'The birth details could not be processed. Please check date, time, place, and coordinates.';
      case 401:
      case 403:
        return 'Please sign in again before generating the Kundali.';
      case 429:
        return 'Too many Kundali generation requests. Please wait ' + this.formatRetryAfter(error) + ' and try again.';
      case 500:
        return 'The Kundali service hit an internal error. Please try again after a moment.';
      case 502:
      case 503:
      case 504:
        return 'The Kundali service is temporarily unavailable. Please try again shortly.';
      default:
        return fallbackMessage;
    }
  }

  private loadSavedKundali(id: string): void {
    this.loadingSavedKundali = true;
    this.loading = true;
    this.showChart = false;
    this.showBirthDetailsForm = false;
    this.dhruvaKundaliContext.clearContext();
    this.selectedHouseData = null;
    this.activeDivisionalChart = 'D9';
    this.saveMessage = '';

    this.kundaliService.getSavedKundali(id).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? id;
        this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext(saved), saved?.chartData ?? null);
        this.patchFormFromSavedKundali(saved);
        this.rememberOpenBirthKey();
        this.applyKundaliResponse(saved?.chartData ?? null);
        this.mahadashaPeriods = saved?.chartData?.mahadashaPeriods ?? [];
        this.latestKundaliStorage.save(this.buildKundaliPayload());
        this.prepareJanmaStory(this.kundaliResponse);
        this.loadingSavedKundali = false;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Load saved kundali error:', error);
        this.loadingSavedKundali = false;
        this.loading = false;
        this.showChart = false;
        this.showBirthDetailsForm = true;
        this.notifications.error('Unable to open saved Kundali.');
        this.cdr.detectChanges();
      }
    });
  }

  private patchFormFromSavedKundali(saved: any): void {
    const birthDate = String(saved?.dateOfBirth ?? '').slice(0, 10);
    const birthTime = String(saved?.timeOfBirth ?? '').slice(0, 5);

    this.kundaliForm.patchValue({
      fullName: saved?.fullName ?? '',
      surname: saved?.surname ?? '',
      fatherName: saved?.fatherName ?? '',
      motherName: saved?.motherName ?? '',
      gotra: saved?.gotra ?? '',
      gender: saved?.gender ?? '',
      birthDate,
      birthTime,
      birthPlace: saved?.birthPlace ?? '',
      latitude: saved?.latitude != null ? String(saved.latitude) : '',
      longitude: saved?.longitude != null ? String(saved.longitude) : ''
    });
  }

  private applyKundaliResponse(response: any): void {
    if (!response) {
      this.kundaliResponse = null;
      this.dhruvaKundaliContext.clearContext();
      this.showChart = false;
      this.showBirthDetailsForm = true;
      return;
    }

    this.currentLifeFocusResetKey++;
    this.kundaliResponse = response;
    this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext({
      kundaliId: this.savedKundaliId,
      fullName: this.kundaliForm.value.fullName,
      dateOfBirth: this.kundaliForm.value.birthDate,
      birthPlace: this.kundaliForm.value.birthPlace,
      chartData: response,
    }), response);
    this.planets = response.planets ?? [];
    this.d1Planets = response.planets ?? [];
    this.d1Houses = response.houses ?? [];
    this.summary = response.summary;
    this.d1Ascendant = response.summary?.ascendant;
    this.d9Chart = response.d9Chart ?? null;
    this.divisionalCharts = this.readDivisionalCharts(response);
    this.scores = response.scores;
    this.yogas = this.extractYogas(response);
    this.doshas = this.extractDoshas(response);
    this.openYogaIndex = null;
    this.openDoshaIndex = null;
    this.houseScores = response.houses ?? [];
    this.showChart = true;
    this.showBirthDetailsForm = false;
    this.setDefaultHouseSelection();
  }

  editBirthDetails(): void {
    this.showBirthDetailsForm = true;
    this.cdr.detectChanges();
  }
  setDefaultHouseSelection(): void {
    const availableHouses = this.primaryChartHouses?.length
      ? this.primaryChartHouses
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
            impactAreas: this.normalizeImpactAreas(item.impactAreas ?? item.ImpactAreas ?? item.effects ?? item.Effects ?? []),
            activationPeriods: this.normalizeActivationPeriods(item.activationPeriods ?? item.ActivationPeriods)
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
            remedies: item.remedies ?? item.Remedies ?? [],
            activationPeriods: this.normalizeActivationPeriods(item.activationPeriods ?? item.ActivationPeriods)
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

  private normalizeActivationPeriods(rawActivationPeriods: any): any {
    if (!rawActivationPeriods) {
      return null;
    }

    const source = rawActivationPeriods;

    return {
      currentlyActive: this.normalizeActivationList(source.currentlyActive ?? source.CurrentlyActive),
      upcoming: this.normalizeActivationList(source.upcoming ?? source.Upcoming),
      past: this.normalizeActivationList(source.past ?? source.Past)
    };
  }

  private normalizeActivationList(rawList: any): any[] {
    if (!Array.isArray(rawList)) {
      return [];
    }

    return rawList.filter(Boolean);
  }

  getCurrentActivation(item: any): any | null {
    return item?.activationPeriods?.currentlyActive?.[0] ?? null;
  }

  getNextActivation(item: any): any | null {
    return item?.activationPeriods?.upcoming?.[0] ?? null;
  }

  hasActivationPeriod(item: any): boolean {
    return !!(this.getCurrentActivation(item) || this.getNextActivation(item));
  }

  getActivationStatusLabel(item: any): string {
    return this.getCurrentActivation(item) ? 'Active now' : 'Next activation';
  }

  getDisplayActivation(item: any): any | null {
    return this.getCurrentActivation(item) ?? this.getNextActivation(item);
  }

  hasActivationAccess(item: any): boolean {
    return this.isPremiumUser && !!item?.activationPeriods;
  }

  hasAnyActivationAccess(items: any[]): boolean {
    return (items ?? []).some((item) => this.hasActivationAccess(item));
  }

  getActivationDisplayRows(item: any): any[] {
    const current = this.normalizeActivationList(item?.activationPeriods?.currentlyActive);
    const upcoming = this.normalizeActivationList(item?.activationPeriods?.upcoming);

    return [
      ...current.map((activation: any) => ({ activation, labelKey: 'kundali.activation.activeNow', isCurrent: true })),
      ...upcoming.slice(0, 3).map((activation: any, index: number) => ({
        activation,
        labelKey: index === 0 ? 'kundali.activation.nextActivation' : 'kundali.activation.upcomingActivation',
        isCurrent: false
      }))
    ];
  }

  getActivationPlanetLabel(activation: any, key: 'mahadashaPlanet' | 'antardashaPlanet'): string {
    const value = this.getActivationPlanetRaw(activation, key);
    return value ? this.translatePlanetName(value) : '';
  }

  getActivationPlanetRaw(activation: any, key: 'mahadashaPlanet' | 'antardashaPlanet'): string {
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    return String(activation?.[key] ?? activation?.[pascalKey] ?? '').trim();
  }

  private buildActivationPeriodGroups(items: any[], mode: 'current' | 'upcoming', title: string): ActivationPeriodGroup[] {
    const groups = new Map<string, ActivationPeriodGroup>();

    items.forEach((item, index) => {
      const activation = mode === 'current' ? this.getCurrentActivation(item) : this.getNextActivation(item);

      if (!activation) {
        return;
      }

      const period = this.formatActivationPeriod(activation);
      const dasha = this.formatActivationDashaPeriod(activation);
      const trigger = this.formatActivationTrigger(activation);
      const key = [mode, period, dasha, trigger].join('|');

      if (!groups.has(key)) {
        groups.set(key, {
          title,
          period,
          dasha,
          trigger,
          isCurrent: mode === 'current',
          items: []
        });
      }

      groups.get(key)?.items.push({ item, index, activation });
    });

    return Array.from(groups.values());
  }

  private getNatalOnlyItems(items: any[]): NatalGroupedItem[] {
    const natalItems = items.map((item, index) => ({ item, index }));

    return this.isPremiumUser
      ? natalItems.filter(({ item }) => !this.hasActivationPeriod(item))
      : natalItems;
  }

  formatActivationDate(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return new Intl.DateTimeFormat('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    }).format(date);
  }

  formatActivationPeriod(activation: any): string {
    if (!activation) {
      return '';
    }

    const startDate = activation.startDate ?? activation.StartDate;
    const endDate = activation.endDate ?? activation.EndDate;

    return `${this.formatActivationDate(startDate)} to ${this.formatActivationDate(endDate)}`;
  }

  formatActivationDashaPeriod(activation: any): string {
    if (!activation) {
      return '';
    }

    const maha = activation.mahadashaPlanet ?? activation.MahadashaPlanet;
    const antar = activation.antardashaPlanet ?? activation.AntardashaPlanet;

    return [maha ? `${maha} Mahadasha` : '', antar ? `${antar} Antardasha` : '']
      .filter(Boolean)
      .join(' / ');
  }

  formatActivationTrigger(activation: any): string {
    if (!activation) {
      return '';
    }

    const triggeredBy = activation.triggeredBy ?? activation.TriggeredBy ?? [];
    const maha = activation.mahadashaPlanet ?? activation.MahadashaPlanet;
    const antar = activation.antardashaPlanet ?? activation.AntardashaPlanet;

    if (!Array.isArray(triggeredBy) || !triggeredBy.length) {
      return this.formatActivationDashaPeriod(activation);
    }

    return triggeredBy
      .map((planet: string) => {
        if (maha && String(planet).toLowerCase() === String(maha).toLowerCase()) {
          return `${planet} Mahadasha`;
        }

        if (antar && String(planet).toLowerCase() === String(antar).toLowerCase()) {
          return `${planet} Antardasha`;
        }

        return this.translatePlanetName(planet);
      })
      .join(' / ');
  }
  formatActivationDashaSentence(activation: any): string {
    return this.formatActivationDashaPeriod(activation).replace(' / ', '-');
  }

  formatActivationSentence(item: any, activation: any): string {
    const status = this.getActivationStatusLabel(item).toLowerCase();
    const level = this.getRawActivationLevel(activation).toLowerCase();
    const period = this.formatActivationPeriod(activation);
    const dasha = this.formatActivationDashaSentence(activation);
    const trigger = this.formatActivationTrigger(activation);
    const timingPhrase = status === 'active now' ? 'is occurring' : 'may occur';

    return `A ${level} activation ${timingPhrase} from ${period} during ${dasha}, primarily triggered by the ${trigger}.`;
  }
  getActivationLevel(activation: any): string {
    const value = activation?.activationLevel ?? activation?.ActivationLevel ?? '';
    return this.translateControlledValue('activationLevel', value);
  }

  getRawActivationLevel(activation: any): string {
    return activation?.activationLevel ?? activation?.ActivationLevel ?? '';
  }

  isStrongActivation(activation: any): boolean {
    return this.getRawActivationLevel(activation).toLowerCase() === 'strong';
  }

  getActivationLevelClass(activation: any): Record<string, boolean> {
    const level = this.getRawActivationLevel(activation).toLowerCase();

    return {
      strong: level === 'strong',
      active: level === 'active',
      mild: level === 'mild'
    };
  }

  getDoshaSeverityClass(value: string | null | undefined): Record<string, boolean> {
    const severity = (value ?? '').toLowerCase();

    return {
      high: severity.includes('high'),
      medium: severity.includes('medium'),
      low: severity.includes('low') || !severity
    };
  }

  getYogaStrengthClass(value: string | null | undefined): Record<string, boolean> {
    const strength = (value ?? '').toLowerCase();

    return {
      strong: strength.includes('strong'),
      medium: strength.includes('medium') || strength.includes('moderate') || strength.includes('neutral'),
      weak: strength.includes('weak') || strength.includes('low')
    };
  }

  handleYogaDetailsAction(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.toggleYogaDetails(index);
  }

  handleDoshaDetailsAction(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.toggleDoshaDetails(index);
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

  isPriorityDosha(index: number): boolean {
    return this.doshaAttention.focus?.index === index;
  }

  isPriorityYoga(index: number): boolean {
    return this.yogaAttention.focus?.index === index;
  }

  openYogaFocus(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.openYogaIndex = index;
    document.querySelector('.yoga-item.needs-attention')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  openDoshaFocus(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.openDoshaIndex = index;
    document.querySelector('.dosha-item.needs-attention')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
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


  private getPdfFilename(mode: KundaliPdfMode): string {
    const name = this.slugifyPdfFilenamePart(this.kundaliForm.value.fullName || 'kundali-report');
    const dob = this.slugifyPdfFilenamePart(this.kundaliForm.value.birthDate || 'dob');
    const modeSuffix = mode === 'desktop' ? 'desktop' : mode === 'mobile' ? 'mobile' : mode === 'corporate' ? 'corporate-advisory' : 'print-bw';

    return `${name}-${dob}-${modeSuffix}.pdf`;
  }

  private slugifyPdfFilenamePart(value: string): string {
    return String(value || '')
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '') || 'kundali-report';
  }

  async downloadPdf(mode: KundaliPdfMode = 'desktop'): Promise<void> {
    this.selectedEmailPdfMode = mode;

    if (!this.kundaliResponse) {
      return;
    }

    this.pdfDownloadMessage = '';
    this.pdfDownloadFailed = false;
    this.exportingPdf = true;

    try {
      const fileName = this.getPdfFilename(mode);
      const pdfBlob = await firstValueFrom(this.kundaliService.generateReportPdf(this.buildReportPdfPayload(mode)));
      this.saveBlob(pdfBlob, fileName);
      this.reportFeedbackReady = true;
      this.notifications.success('Report PDF downloaded.');
    } catch (error) {
      console.error('Report PDF download error:', error);
      this.pdfDownloadFailed = true;
      this.notifications.error(await this.resolveReportDownloadError(error));
    }
    finally {
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
    }
  }

  private async resolveReportDownloadError(error: unknown): Promise<string> {
    const fallbackMessage = 'Unable to download the report PDF right now.';
    const errorName = error && typeof error === 'object' && 'name' in error ? String(error.name) : '';

    if (errorName === 'TimeoutError') {
      return '\u2014';
    }

    if (error && typeof error === 'object') {
      const status = 'status' in error ? Number((error as { status?: unknown }).status) : 0;
      const responseError = 'error' in error ? (error as { error?: unknown }).error : null;

      if (status === 429) {
        const limitMessage = await this.extractReportErrorMessage(responseError);
        return limitMessage || this.buildReportRateLimitMessage(error);
      }

      if (!responseError) {
        return fallbackMessage;
      }

      if (responseError instanceof Blob) {
        return await this.readReportErrorBlob(responseError, fallbackMessage);
      }

      if (responseError && typeof responseError === 'object' && 'message' in responseError) {
        return String((responseError as { message?: unknown }).message || fallbackMessage);
      }
    }

    return fallbackMessage;
  }

  private async readReportErrorBlob(errorBlob: Blob, fallbackMessage: string): Promise<string> {
    if (!errorBlob.size) {
      return fallbackMessage;
    }

    try {
      const text = await errorBlob.text();
      const parsed = JSON.parse(text) as { message?: unknown; title?: unknown };
      return String(parsed.message || parsed.title || fallbackMessage);
    } catch {
      return fallbackMessage;
    }
  }

  private buildReportPdfPayload(mode: KundaliPdfMode = 'desktop'): any {
    const branding = this.showPdfBranding && this.activeBranding
      ? {
          astrologerName: this.activeBranding.astrologerName,
          designation: this.activeBranding.designation,
          address: this.activeBranding.address,
          contactNumber: this.activeBranding.contactNumber,
          email: this.activeBranding.email,
          logoUrl: this.activeBranding.logoUrl
        }
      : null;

    return {
      birthProfile: {
        ...this.buildKundaliPayload(),
        fullName: this.kundaliForm.value.fullName || '',
        surname: this.optionalText(this.kundaliForm.value.surname) || '',
        fatherName: this.optionalText(this.kundaliForm.value.fatherName) || '',
        motherName: this.optionalText(this.kundaliForm.value.motherName) || '',
        gotra: this.optionalText(this.kundaliForm.value.gotra) || '',
        birthDate: this.kundaliForm.value.birthDate || '',
        birthTime: this.kundaliForm.value.birthTime || '',
        birthPlace: this.kundaliForm.value.birthPlace || '',
        branding
      },
      report: this.kundaliResponse,
      fileName: this.getPdfFilename(mode),
      mode
    };
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    window.URL.revokeObjectURL(url);
  }

  private prepareJanmaStory(response?: any): void {
    const storyResponse = response ?? this.kundaliResponse;
    const lines = this.buildJanmaStoryLines(storyResponse);
    this.clearJanmaStoryTimers();
    this.displayPersonName = this.reportDisplayName === '-' ? '' : this.reportDisplayName;
    this.janmaStoryLines = lines;
    this.displayedJanmaStoryLines = lines.map(() => '');
    this.janmaStoryHighlightTerms = this.buildJanmaStoryHighlightTerms(storyResponse);
    this.isJanmaStoryRailCollapsed = true;
  }

  private startJanmaStory(response?: any): void {
    if (!this.janmaStoryLines.length) {
      this.prepareJanmaStory(response);
    }

    this.clearJanmaStoryTimers();
    this.displayedJanmaStoryLines = this.janmaStoryLines.map(() => '');
    this.typeJanmaStoryLine(this.janmaStoryLines, 0, 0);
  }

  private resetActiveJanmaStoryPresentation(): void {
    this.clearJanmaStoryTimers();
    this.displayedJanmaStoryLines = this.janmaStoryLines.map(() => '');
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

  toggleJanmaStoryRail(): void {
    this.isJanmaStoryRailCollapsed = !this.isJanmaStoryRailCollapsed;

    if (this.isJanmaStoryRailCollapsed) {
      this.resetActiveJanmaStoryPresentation();
      return;
    }

    this.startJanmaStory(this.kundaliResponse);
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
      this.reportDisplayName === '-' ? '' : this.reportDisplayName,
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

  getSudarshanAreaDefinition(area: string | null | undefined): string {
    const normalized = (area ?? '').toLowerCase();

    if (normalized.includes('kendra')) {
      return 'Core life stability and support.';
    }

    if (normalized.includes('trikona')) {
      return 'Fortune, dharma and opportunity.';
    }

    if (normalized.includes('upachaya')) {
      return 'Growth through effort over time.';
    }

    if (normalized.includes('dusthana')) {
      return 'Pressure areas needing care.';
    }

    if (normalized.includes('planet')) {
      return 'Planet themes behind timing.';
    }

    return 'Repeated chart emphasis.';
  }
  getTopSudarshanHouses(sudarshan: any): any[] {
    return [...(sudarshan?.houseEmphasis ?? [])].sort((a: any, b: any) => Number(b?.score ?? 0) - Number(a?.score ?? 0)).slice(0, 3);
  }
  openHoraverse(): void {
    if (!this.kundaliResponse) { this.horaverseOpenError = 'Generate a Kundali before opening HoraVerse.'; return; }
    try {
      const sessionId = `hora-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
      const formValue = this.kundaliForm.getRawValue();
      const response = this.kundaliResponse;
      const session = {
        version: 1,
        createdAt: new Date().toISOString(),
        birthProfile: {
          name: formValue.fullName ?? '',
          surname: formValue.surname ?? '',
          fatherName: formValue.fatherName ?? '',
          motherName: formValue.motherName ?? '',
          gotra: formValue.gotra ?? '',
          gender: formValue.gender ?? '',
          birthDate: formValue.birthDate ?? '',
          birthTime: formValue.birthTime ?? '',
          birthPlace: formValue.birthPlace ?? '',
          latitude: formValue.latitude ?? null,
          longitude: formValue.longitude ?? null
        },
        report: {
          summary: response?.summary ?? {},
          panchang: response?.panchang ?? {},
          planets: response?.planets ?? response?.planetaryPositions ?? [],
          houses: response?.houses ?? response?.bhavas ?? [],
          scores: response?.scores ?? response?.kundaliScores ?? {},
          currentDasha: response?.currentDasha ?? response?.dasha?.current ?? {}
        },
        mahadashaPeriods: this.mahadashaPeriods ?? [],
        tone: 'mysterious-safe',
        theme: 'default-neuro-cosmic'
      };
      window.localStorage.setItem(`horaverse.session.${sessionId}`, JSON.stringify(session));
      window.localStorage.setItem('horaverse.latestSessionId', sessionId);
      this.horaverseOpenError = '';
      const horaverseWindow = window.open(`/horaverse?session=${encodeURIComponent(sessionId)}`, '_blank');
      if (!horaverseWindow) {
        window.location.href = `/horaverse?session=${encodeURIComponent(sessionId)}`;
      }
    } catch (error) {
      console.error('HoraVerse open failed', error);
      this.horaverseOpenError = 'HoraVerse could not open in this browser.';
    }
  }
}

