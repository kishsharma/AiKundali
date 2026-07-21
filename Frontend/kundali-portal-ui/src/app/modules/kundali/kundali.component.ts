import { ChangeDetectorRef, Component, HostListener, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
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
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { HouseScoreOverview } from "../../components/house-score-overview/house-score-overview";
import { KundaliMetadataInsightsComponent } from "../../components/kundali-metadata-insights/kundali-metadata-insights.component";
import { TransitInsightComponent } from "../../components/transit-insight/transit-insight.component";
import { CurrentLifeFocusComponent } from "../../components/current-life-focus/current-life-focus.component";
import { RemediesRecommendationComponent } from "../../components/remedies-recommendation/remedies-recommendation.component";
import {
  AstrologerBrandingSetting,
  AstrologerBrandingSettingsService
} from '../../core/services/astrologer-branding-settings.service';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';

type KundaliPdfMode = 'desktop' | 'mobile' | 'print-bw' | 'corporate';

const KUNDALI_PDF_MODE_CLASSES: Record<KundaliPdfMode, string> = {
  desktop: 'pdf-mode-desktop',
  mobile: 'pdf-mode-mobile',
  'print-bw': 'pdf-mode-print-bw',
  corporate: 'pdf-mode-desktop'
};

const KUNDALI_PDF_CAPTURE_STYLE_ID = 'kundali-pdf-html2canvas-safe-style';

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
  private route = inject(ActivatedRoute);
  locationResults: any[] = [];
  kundaliResponse: any = null;
  brandingSettings: AstrologerBrandingSetting | null = null;
  houseScores: any[] = [];
  loading = false;
  showChart = false;
  mahadashaPeriods: any[] = [];
  savingKundali = false;
  savedKundaliId: string | null = null;
  currentLifeFocusResetKey = 0;
  saveMessage = '';
  shareEmail = '';
  shareEmailTouched = false;
  shareMessage = '';
  shareSending = false;
  selectedEmailPdfMode: KundaliPdfMode = 'corporate';
  shareWhatsAppNumber = '';
  shareWhatsAppTouched = false;
  shareWhatsAppMessage = '';
  loadingSavedKundali = false;
  horaverseOpenError = '';
  isChartPinned = false;
  displayPersonName = '';
  janmaStoryLines: string[] = [];
  displayedJanmaStoryLines: string[] = [];
  isJanmaStoryRailCollapsed = false;
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
  readonly pdfModes: Array<{ mode: KundaliPdfMode; label: string; description: string }> = [
    {
      mode: 'desktop',
      label: 'Color Desktop',
      description: 'Current colorful report'
    },
    {
      mode: 'mobile',
      label: 'Mobile Friendly',
      description: 'Larger single-column PDF'
    },
    {
      mode: 'print-bw',
      label: 'Black & White Print',
      description: 'High-contrast printer PDF'
    },
    {
      mode: 'corporate',
      label: 'Corporate Advisory',
      description: 'Professional advisory-style report'
    }
  ];

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

  private toNumber(value: unknown): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  onCurrentLifeFocusSaved(kundaliId: string): void {
    this.savedKundaliId = kundaliId;
    this.saveMessage = 'Kundali saved for Current Life Focus.';
    window.dispatchEvent(new CustomEvent('kundali-saved'));
    this.cdr.detectChanges();
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
  onReportPickerToggle(event: Event, ...otherPickers: HTMLDetailsElement[]): void {
    const currentPicker = event.currentTarget as HTMLDetailsElement | null;
    if (!currentPicker?.open) {
      return;
    }

    otherPickers.forEach(picker => picker.removeAttribute('open'));
  }
  shareReportOnWhatsApp(): void {
    this.shareWhatsAppTouched = true;
    this.shareWhatsAppMessage = '';

    if (!this.isShareWhatsAppValid) {
      return;
    }

    if (!this.kundaliResponse) {
      this.shareWhatsAppMessage = 'Generate a report before sharing.';
      return;
    }

    const reportName = this.kundaliForm.value.fullName || 'Kundali';
    const birthDate = this.kundaliForm.value.birthDate || '';
    const message = [
      `Kundali report is ready for ${reportName}.`,
      birthDate ? `Birth date: ${birthDate}` : '',
      this.savedKundaliId ? `Saved report ID: ${this.savedKundaliId}` : '',
      'PDF attachment is available from HoraLife report email/download.'
    ].filter(Boolean).join('\n\n');
    const url = `https://wa.me/${this.normalizedShareWhatsAppNumber}?text=${encodeURIComponent(message)}`;
    window.open(url, '_blank', 'noopener,noreferrer');
    this.shareWhatsAppMessage = 'Opening WhatsApp.';
  }

  async sendReportByEmail(): Promise<void> {
    this.shareEmailTouched = true;
    this.shareMessage = '';

    if (!this.isShareEmailValid || this.shareSending) {
      return;
    }

    if (!this.kundaliResponse) {
      this.shareMessage = 'Generate a report before sharing.';
      return;
    }

    this.shareSending = true;
    this.exportingPdf = true;

    try {
      const reportName = this.kundaliForm.value.fullName || 'Kundali';
      const birthDate = this.kundaliForm.value.birthDate || '';
      const birthTime = this.kundaliForm.value.birthTime || '';
      const birthPlace = this.kundaliForm.value.birthPlace || '';
      const isCorporateReport = this.selectedEmailPdfMode === 'corporate';
      const reportFormatLabel = this.pdfModes.find(pdfMode => pdfMode.mode === this.selectedEmailPdfMode)?.label || 'Kundali';
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

      this.shareMessage = 'Report email sent.';
    } catch (error) {
      console.error('Report share email error:', error);
      this.shareMessage = 'Unable to send report email.';
    } finally {
      this.shareSending = false;
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
      this.cdr.detectChanges();
    }
  }

  private blobToBase64(blob: Blob): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onloadend = () => resolve(String(reader.result).split(',')[1] ?? '');
      reader.onerror = () => reject(reader.error ?? new Error('Unable to read report PDF.'));
      reader.readAsDataURL(blob);
    });
  }

  toggleChartPin(): void { this.isChartPinned = !this.isChartPinned; }

  scrollToReportSection(sectionId: string): void {
    document.getElementById(sectionId)?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

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
    this.currentLifeFocusResetKey++;
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

  private loadSavedKundali(id: string): void {
    this.loadingSavedKundali = true;
    this.loading = true;
    this.showChart = false;
    this.selectedHouseData = null;
    this.activeDivisionalChart = 'd1';
    this.saveMessage = '';

    this.kundaliService.getSavedKundali(id).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? id;
        this.patchFormFromSavedKundali(saved);
        this.applyKundaliResponse(saved?.chartData ?? null);
        this.mahadashaPeriods = saved?.chartData?.mahadashaPeriods ?? [];
        this.latestKundaliStorage.save(this.buildKundaliPayload());
        this.startJanmaStory(this.kundaliResponse);
        this.loadingSavedKundali = false;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Load saved kundali error:', error);
        this.loadingSavedKundali = false;
        this.loading = false;
        this.showChart = false;
        this.saveMessage = 'Unable to open saved Kundali.';
        this.cdr.detectChanges();
      }
    });
  }

  private patchFormFromSavedKundali(saved: any): void {
    const birthDate = String(saved?.dateOfBirth ?? '').slice(0, 10);
    const birthTime = String(saved?.timeOfBirth ?? '').slice(0, 5);

    this.kundaliForm.patchValue({
      fullName: saved?.fullName ?? '',
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
      this.showChart = false;
      return;
    }

    this.currentLifeFocusResetKey++;
    this.kundaliResponse = response;
    this.planets = response.planets ?? [];
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
    this.houseScores = response.houses ?? [];
    this.showChart = true;
    this.setDefaultHouseSelection();
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
    const source = rawActivationPeriods ?? {};

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

    return `${this.formatActivationDate(startDate)} - ${this.formatActivationDate(endDate)}`;
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

        return planet;
      })
      .join(' / ');
  }
  formatActivationDashaSentence(activation: any): string {
    return this.formatActivationDashaPeriod(activation).replace(' / ', '-');
  }

  formatActivationSentence(item: any, activation: any): string {
    const status = this.getActivationStatusLabel(item).toLowerCase();
    const level = this.getActivationLevel(activation).toLowerCase();
    const period = this.formatActivationPeriod(activation).replace(' - ', ' to ');
    const dasha = this.formatActivationDashaSentence(activation);
    const trigger = this.formatActivationTrigger(activation);
    const timingPhrase = status === 'active now' ? 'is occurring' : 'may occur';

    return `A ${level} activation ${timingPhrase} from ${period} during ${dasha}, primarily triggered by the ${trigger}.`;
  }
  getActivationLevel(activation: any): string {
    return activation?.activationLevel ?? activation?.ActivationLevel ?? '';
  }

  getActivationLevelClass(activation: any): Record<string, boolean> {
    const level = this.getActivationLevel(activation).toLowerCase();

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

  private getPdfImageData(canvas: HTMLCanvasElement): string {
    return canvas.toDataURL('image/jpeg', 0.96);
  }

  private getPdfElementSize(element: HTMLElement): { width: number; height: number } {
    const rect = element.getBoundingClientRect();
    const width = Math.ceil(Math.max(element.scrollWidth, element.offsetWidth, rect.width));
    const height = Math.ceil(Math.max(element.scrollHeight, element.offsetHeight, rect.height));

    return { width, height };
  }

  private isPositiveFinite(value: number): boolean {
    return Number.isFinite(value) && value > 0;
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
private async addElementToPdf(
    pdf: jsPDF,
    element: HTMLElement,
    pdfWidth: number,
    pageHeight: number,
    margin: number = 8,
    pageHeightUsed: number = 0,
    spacing: number = 6,
    mode: KundaliPdfMode = 'desktop'
  ): Promise<number> {
    const elementSize = this.getPdfElementSize(element);
    let currentPageHeight = this.isPositiveFinite(pageHeightUsed) ? pageHeightUsed : 0;

    if (!this.isPositiveFinite(elementSize.width) || !this.isPositiveFinite(elementSize.height)) {
      return currentPageHeight;
    }

    const canvas = await html2canvas(element, {
      scale: 2,
      backgroundColor: '#ffffff',
      useCORS: true,
      logging: false,
      windowWidth: elementSize.width,
      width: elementSize.width,
      height: elementSize.height,
      onclone: (clonedDocument: Document) => this.sanitizePdfClone(clonedDocument, mode)
    });

    const usableWidth = pdfWidth - margin * 2;
    const imgWidth = canvas.width;
    const imgHeight = canvas.height;
    const contentHeight = pageHeight - margin * 2;

    if (
      !this.isPositiveFinite(usableWidth) ||
      !this.isPositiveFinite(imgWidth) ||
      !this.isPositiveFinite(imgHeight) ||
      !this.isPositiveFinite(contentHeight)
    ) {
      return currentPageHeight;
    }

    const renderedHeight = (imgHeight * usableWidth) / imgWidth;

    if (!this.isPositiveFinite(renderedHeight)) {
      return currentPageHeight;
    }

    if (renderedHeight > contentHeight) {
      let startY = 0;
      while (startY < imgHeight) {
        const remainingHeight = imgHeight - startY;
        const sliceHeight = Math.min(remainingHeight, Math.ceil((contentHeight * imgWidth) / usableWidth));

        if (!this.isPositiveFinite(sliceHeight)) {
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

        ctx.fillStyle = '#ffffff';
        ctx.fillRect(0, 0, imgWidth, sliceHeight);
        ctx.drawImage(canvas, 0, startY, imgWidth, sliceHeight, 0, 0, imgWidth, sliceHeight);

        const sliceHeightPx = (sliceHeight * usableWidth) / imgWidth;
        
        if (!this.isPositiveFinite(sliceHeightPx)) {
          break;
        }

        pdf.addImage(this.getPdfImageData(sliceCanvas), 'JPEG', margin, margin + currentPageHeight, usableWidth, sliceHeightPx);

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
      this.getPdfImageData(canvas),
      'JPEG',
      margin,
      margin + currentPageHeight,
      usableWidth,
      renderedHeight
    );

    return currentPageHeight + renderedHeight + spacing;
  }

  private applyHtml2CanvasSafeStyles(clonedDocument: Document, mode: KundaliPdfMode): void {
    const isMobile = mode === 'mobile';
    const isPrint = mode === 'print-bw';
    const textColor = isMobile ? '#20140b' : '#000000';
    const mutedColor = isMobile ? '#6d5a3f' : '#333333';
    const lineColor = isPrint ? '#000000' : (isMobile ? '#ead7ae' : '#d9deee');
    const surfaceColor = isMobile ? '#ffffff' : '#ffffff';
    const softSurfaceColor = isMobile ? '#fff3d6' : '#ffffff';
    const accentColor = isMobile ? '#b98218' : '#8a6a1f';
    const svgTextColor = isMobile ? '#3a250f' : '#000000';
    const svgStrokeColor = isPrint ? '#000000' : (isMobile ? '#9a6a16' : '#8a6a1f');
    const svgFillColor = isMobile ? '#fffaf0' : '#ffffff';
    const style = clonedDocument.createElement('style');
    style.textContent = `
      :root,
      body,
      body.pdf-mode,
      body.pdf-mode * {
        --theme-bg: ${surfaceColor} !important;
        --theme-surface: ${surfaceColor} !important;
        --theme-surface-2: ${softSurfaceColor} !important;
        --theme-surface-soft: ${softSurfaceColor} !important;
        --theme-input-bg: ${surfaceColor} !important;
        --theme-text: ${textColor} !important;
        --theme-text-muted: ${mutedColor} !important;
        --theme-line: ${lineColor} !important;
        --theme-line-soft: ${lineColor} !important;
        --theme-accent: ${accentColor} !important;
        --theme-accent-soft: ${softSurfaceColor} !important;
        --theme-info: #2563eb !important;
        --theme-success: #15803d !important;
        --theme-warning: #b45309 !important;
        --theme-danger: #b91c1c !important;
        --theme-shadow: transparent !important;
        --theme-copper: ${accentColor} !important;
        --brass: ${accentColor} !important;
        --brass-soft: ${softSurfaceColor} !important;
        --parchment: ${textColor} !important;
        --parchment-dim: ${mutedColor} !important;
        --ink: ${surfaceColor} !important;
        --ink-2: ${surfaceColor} !important;
        --ink-3: ${softSurfaceColor} !important;
        --line: ${lineColor} !important;
        --line-soft: ${lineColor} !important;
        --planet-color: ${textColor} !important;
      }

      body.pdf-mode *,
      body.pdf-mode *::before,
      body.pdf-mode *::after {
        color: ${textColor} !important;
        border-color: ${lineColor} !important;
        outline-color: ${lineColor} !important;
        text-decoration-color: ${textColor} !important;
        text-shadow: none !important;
        box-shadow: none !important;
        background-image: none !important;
      }

      body.pdf-mode,
      body.pdf-mode app-root,
      body.pdf-mode #kundali-report {
        background-color: ${surfaceColor} !important;
      }

      body.pdf-mode svg,
      body.pdf-mode svg * {
        background-color: transparent !important;
        color: ${svgTextColor} !important;
        fill: ${svgFillColor} !important;
        stroke: ${svgStrokeColor} !important;
      }

      body.pdf-mode svg text,
      body.pdf-mode svg tspan {
        fill: ${svgTextColor} !important;
        stroke: none !important;
      }
    `;
    clonedDocument.head.appendChild(style);
  }

  private sanitizeCssColorFunctions(value: string): string {
    return value
      .replace(/color-mix\([^;]*(?:\)[^;]*)?\)/gi, '#ffffff')
      .replace(/oklab\([^)]*\)/gi, '#000000')
      .replace(/oklch\([^)]*\)/gi, '#000000')
      .replace(/lab\([^)]*\)/gi, '#000000')
      .replace(/lch\([^)]*\)/gi, '#000000')
      .replace(/color\([^)]*\)/gi, '#000000');
  }

  private forceMobilePdfChartCloneLayout(clonedDocument: Document): void {
    const report = clonedDocument.getElementById('kundali-report');
    const chartSection = report?.querySelector('app-section-card');
    const chartGrid = chartSection?.querySelector<HTMLElement>('.chart-with-house-details');
    const left = chartSection?.querySelector<HTMLElement>('.left');
    const right = chartSection?.querySelector<HTMLElement>('.right');
    const d9Card = chartSection?.querySelector<HTMLElement>('.pdf-d9-chart-card');
    const screenHouseDetails = chartSection?.querySelector<HTMLElement>('.screen-house-details');
    const controls = chartSection?.querySelectorAll<HTMLElement>('.chart-control-bar, .chart-style-selector, .chart-pin-button, .division-tabs');

    controls?.forEach(control => control.style.setProperty('display', 'none', 'important'));
    screenHouseDetails?.style.setProperty('display', 'none', 'important');

    if (chartGrid) {
      chartGrid.style.setProperty('display', 'grid', 'important');
      chartGrid.style.setProperty('grid-template-columns', 'repeat(2, minmax(0, 1fr))', 'important');
      chartGrid.style.setProperty('gap', '0.12rem', 'important');
      chartGrid.style.setProperty('align-items', 'start', 'important');
      chartGrid.style.setProperty('overflow', 'visible', 'important');
    }

    [left, right, d9Card].forEach(node => {
      if (!node) { return; }
      node.style.setProperty('display', 'block', 'important');
      node.style.setProperty('visibility', 'visible', 'important');
      node.style.setProperty('width', '100%', 'important');
      node.style.setProperty('max-width', '100%', 'important');
      node.style.setProperty('min-width', '0', 'important');
      node.style.setProperty('min-height', '0', 'important');
      node.style.setProperty('overflow', 'visible', 'important');
    });

    d9Card?.querySelectorAll<HTMLElement>('.card, .card-body, .card-content, .chart-surface, .chart-surface-d9')
      .forEach(node => {
        node.style.setProperty('display', 'block', 'important');
        node.style.setProperty('visibility', 'visible', 'important');
        node.style.setProperty('padding', '0.08rem', 'important');
        node.style.setProperty('margin', '0', 'important');
        node.style.setProperty('min-height', '0', 'important');
        node.style.setProperty('overflow', 'visible', 'important');
      });
  }

  private sanitizePdfClone(clonedDocument: Document, mode: KundaliPdfMode): void {
    this.applyHtml2CanvasSafeStyles(clonedDocument, mode);

    if (mode === 'mobile') {
      this.forceMobilePdfChartCloneLayout(clonedDocument);
    }

    const isMobile = mode === 'mobile';
    const isPrint = mode === 'print-bw';
    const textColor = isMobile ? '#20140b' : '#000000';
    const lineColor = isPrint ? '#000000' : (isMobile ? '#ead7ae' : '#d9deee');
    const rootBackground = isMobile ? '#fff7ed' : '#ffffff';
    const svgFillColor = isMobile ? '#fffaf0' : '#ffffff';
    const svgStrokeColor = isPrint ? '#000000' : (isMobile ? '#9a6a16' : '#000000');
    const svgTextColor = isMobile ? '#3a250f' : '#000000';

    const unsupportedColorPattern = /(color-mix|oklab|oklch|lab\(|lch\(|color\()/i;

    clonedDocument.querySelectorAll<HTMLElement | SVGElement>('*').forEach((node) => {
      const inlineStyle = node.getAttribute('style');
      if (inlineStyle && unsupportedColorPattern.test(inlineStyle)) {
        node.setAttribute('style', this.sanitizeCssColorFunctions(inlineStyle));
      }

      const style = node.style;
      const tagName = node.tagName.toLowerCase();
      const isSvgPaintNode = ['svg', 'path', 'polygon', 'line', 'circle', 'rect', 'text', 'tspan', 'g'].includes(tagName);

      style.setProperty('color', textColor, 'important');
      style.setProperty('background-image', 'none', 'important');
      style.setProperty('box-shadow', 'none', 'important');
      style.setProperty('text-shadow', 'none', 'important');
      style.setProperty('outline-color', lineColor, 'important');
      style.setProperty('border-color', lineColor, 'important');
      style.setProperty('border-top-color', lineColor, 'important');
      style.setProperty('border-right-color', lineColor, 'important');
      style.setProperty('border-bottom-color', lineColor, 'important');
      style.setProperty('border-left-color', lineColor, 'important');
      style.setProperty('text-decoration-color', textColor, 'important');
      style.setProperty('caret-color', textColor, 'important');

      if (node === clonedDocument.body || tagName === 'app-root') {
        style.setProperty('background-color', rootBackground, 'important');
      } else if (isPrint) {
        style.setProperty('background-color', 'transparent', 'important');
      }

      if (isSvgPaintNode) {
        style.setProperty('stroke', tagName === 'text' || tagName === 'tspan' ? 'none' : svgStrokeColor, 'important');
        style.setProperty('fill', tagName === 'text' || tagName === 'tspan' ? svgTextColor : svgFillColor, 'important');
      }
    });

    clonedDocument.body.style.setProperty('background-color', rootBackground, 'important');
    clonedDocument.body.style.setProperty('color', textColor, 'important');
  }

  private getHtml2CanvasSafeCss(): string {
    return [
      'html body.pdf-mode, html body.pdf-mode app-root, html body.pdf-mode #kundali-report { background-color: #ffffff !important; color: #000000 !important; }',
      'html body.pdf-mode #kundali-report, html body.pdf-mode #kundali-report * { --theme-bg: #ffffff !important; --theme-surface: #ffffff !important; --theme-surface-2: #ffffff !important; --theme-surface-soft: #ffffff !important; --theme-input-bg: #ffffff !important; --theme-text: #000000 !important; --theme-text-muted: #333333 !important; --theme-line: #d9deee !important; --theme-line-soft: #e6e9f4 !important; --theme-accent: #8a6a1f !important; --theme-accent-soft: #e7d8a8 !important; --theme-info: #1d4ed8 !important; --theme-success: #166534 !important; --theme-warning: #92400e !important; --theme-danger: #991b1b !important; --theme-shadow: transparent !important; --theme-copper: #9a5b35 !important; --brass: #8a6a1f !important; --brass-soft: #e7d8a8 !important; --parchment: #000000 !important; --parchment-dim: #333333 !important; --ink: #ffffff !important; --ink-2: #ffffff !important; --ink-3: #ffffff !important; --line: #d9deee !important; --line-soft: #e6e9f4 !important; --planet-color: #000000 !important; color: #000000 !important; background-color: transparent !important; background-image: none !important; border-color: #d9deee !important; outline-color: #d9deee !important; text-decoration-color: #000000 !important; text-shadow: none !important; box-shadow: none !important; }',
      'html body.pdf-mode #kundali-report, html body.pdf-mode #kundali-report .report-header, html body.pdf-mode #kundali-report .report-footer, html body.pdf-mode #kundali-report .section-card, html body.pdf-mode #kundali-report .card, html body.pdf-mode #kundali-report .planet-table, html body.pdf-mode #kundali-report .planet-table-wrapper { background-color: #ffffff !important; }',
      'html body.pdf-mode #kundali-report *::before, html body.pdf-mode #kundali-report *::after { color: #000000 !important; background-color: transparent !important; background-image: none !important; border-color: #d9deee !important; outline-color: #d9deee !important; text-decoration-color: #000000 !important; text-shadow: none !important; box-shadow: none !important; }',
      'html body.pdf-mode #kundali-report svg, html body.pdf-mode #kundali-report svg * { background-color: transparent !important; color: #000000 !important; fill: #ffffff !important; stroke: #000000 !important; }',
      'html body.pdf-mode #kundali-report svg text, html body.pdf-mode #kundali-report svg tspan { fill: #000000 !important; stroke: none !important; }',
      'html body.pdf-mode-mobile #kundali-report, html body.pdf-mode-mobile #kundali-report * { --theme-bg: #fff7ed !important; --theme-surface: #ffffff !important; --theme-surface-2: #fff8e7 !important; --theme-surface-soft: #fff3d6 !important; --theme-input-bg: #fffaf0 !important; --theme-text: #20140b !important; --theme-text-muted: #6d5a3f !important; --theme-line: #ead7ae !important; --theme-line-soft: #f3e6c9 !important; --theme-accent: #b98218 !important; --theme-accent-soft: #f3db9b !important; --theme-info: #2563eb !important; --theme-success: #15803d !important; --theme-warning: #b45309 !important; --theme-danger: #b91c1c !important; --theme-shadow: transparent !important; --theme-copper: #b86b3f !important; --brass: #b98218 !important; --brass-soft: #f3db9b !important; --parchment: #20140b !important; --parchment-dim: #6d5a3f !important; --ink: #fffaf0 !important; --ink-2: #ffffff !important; --ink-3: #fff8e7 !important; --line: #ead7ae !important; --line-soft: #f3e6c9 !important; color: #20140b !important; border-color: #ead7ae !important; }',
      'html body.pdf-mode-mobile #kundali-report .section-card, html body.pdf-mode-mobile #kundali-report .card, html body.pdf-mode-mobile #kundali-report .dashboard-card, html body.pdf-mode-mobile #kundali-report .planet-table-wrapper, html body.pdf-mode-mobile #kundali-report .yoga-item, html body.pdf-mode-mobile #kundali-report .dosha-item, html body.pdf-mode-mobile #kundali-report .score-wrapper, html body.pdf-mode-mobile #kundali-report .house-card, html body.pdf-mode-mobile #kundali-report .metadata-card { background-color: #ffffff !important; color: #20140b !important; border-color: #ead7ae !important; }',
      'html body.pdf-mode-mobile #kundali-report .report-header, html body.pdf-mode-mobile #kundali-report .report-footer, html body.pdf-mode-mobile #kundali-report .planet-table th, html body.pdf-mode-mobile #kundali-report .antardasha-table th { background-color: #fff3d6 !important; color: #3a250f !important; border-color: #d8b46b !important; }',
      'html body.pdf-mode-mobile #kundali-report .badge, html body.pdf-mode-mobile #kundali-report .strength-pill, html body.pdf-mode-mobile #kundali-report .aspect-pill, html body.pdf-mode-mobile #kundali-report .meta-chip, html body.pdf-mode-mobile #kundali-report .tone-pill, html body.pdf-mode-mobile #kundali-report .status, html body.pdf-mode-mobile #kundali-report .activation-level { background-color: #fff3d6 !important; color: #7a4f0f !important; border-color: #d8b46b !important; }',
      'html body.pdf-mode-mobile #kundali-report .planet-icon { background-color: #fff7ed !important; color: #b98218 !important; border-color: #d8b46b !important; }',
      'html body.pdf-mode-mobile #kundali-report svg, html body.pdf-mode-mobile #kundali-report svg * { color: #7a4f0f !important; fill: #fffaf0 !important; stroke: #9a6a16 !important; }',
      'html body.pdf-mode-mobile #kundali-report svg text, html body.pdf-mode-mobile #kundali-report svg tspan { fill: #4b3a22 !important; stroke: none !important; }',
      'html body.pdf-mode-print-bw #kundali-report, html body.pdf-mode-print-bw #kundali-report * { color: #000000 !important; background-image: none !important; box-shadow: none !important; text-shadow: none !important; border-color: #000000 !important; }',
      'html body.pdf-mode-print-bw #kundali-report svg, html body.pdf-mode-print-bw #kundali-report svg * { color: #000000 !important; fill: #ffffff !important; stroke: #000000 !important; }',
      'html body.pdf-mode-print-bw #kundali-report svg text, html body.pdf-mode-print-bw #kundali-report svg tspan { fill: #000000 !important; stroke: none !important; }'
    ].join('\n');
  }

  private applyPdfCaptureSafeStyles(targetDocument: Document): void {
    if (targetDocument.getElementById(KUNDALI_PDF_CAPTURE_STYLE_ID)) {
      return;
    }

    const style = targetDocument.createElement('style');
    style.id = KUNDALI_PDF_CAPTURE_STYLE_ID;
    style.textContent = this.getHtml2CanvasSafeCss();
    targetDocument.head.appendChild(style);
  }

  private clearPdfCaptureSafeStyles(targetDocument: Document): void {
    targetDocument.getElementById(KUNDALI_PDF_CAPTURE_STYLE_ID)?.remove();
  }
  private applyPdfMode(report: HTMLElement, mode: KundaliPdfMode): void {
    const modeClass = KUNDALI_PDF_MODE_CLASSES[mode];

    this.applyPdfCaptureSafeStyles(document);
    document.body.classList.add('pdf-mode', 'kundali-layout', modeClass);
    report.classList.add('pdf-mode', modeClass);
    report.setAttribute('data-pdf-mode', mode);
    document.body.setAttribute('data-pdf-mode', mode);
  }

  private clearPdfMode(report: HTMLElement): void {
    const modeClasses = Object.values(KUNDALI_PDF_MODE_CLASSES);

    document.body.classList.remove('pdf-mode', 'kundali-layout', ...modeClasses);
    report.classList.remove('pdf-mode', ...modeClasses);
    report.removeAttribute('data-pdf-mode');
    document.body.removeAttribute('data-pdf-mode');
    this.clearPdfCaptureSafeStyles(document);
  }

  private async buildReportPdf(mode: KundaliPdfMode = 'desktop'): Promise<{ pdf: jsPDF; fileName: string }> {
    const report = document.getElementById('kundali-report');

    if (!report) {
      throw new Error('Kundali report is not available.');
    }

    try {
      this.applyPdfMode(report, mode);
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
        if (mode === 'mobile') {
          detailGrid.querySelectorAll<HTMLElement>(':scope > app-section-card').forEach(section => elementsToRender.push(section));
        } else {
          elementsToRender.push(detailGrid);
        }
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
        if (mode === 'mobile') {
          kundaliscore.querySelectorAll<HTMLElement>(':scope > app-section-card').forEach(section => elementsToRender.push(section));
        } else {
          elementsToRender.push(kundaliscore);
        }
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
        pageHeightUsed = await this.addElementToPdf(pdf, element, pdfWidth, pdfHeight, mode === 'mobile' ? 3 : 5, pageHeightUsed, mode === 'mobile' ? 0.8 : 3, mode);
      }

      return { pdf, fileName: this.getPdfFilename(mode) };
    }
    finally {
      document.getElementById('divtabs')?.classList.remove('displaynone');
      this.clearPdfMode(report);
      this.cdr.detectChanges();
    }
  }

  async downloadPdf(mode: KundaliPdfMode = 'desktop'): Promise<void> {
    if (!this.kundaliResponse) {
      return;
    }

    this.exportingPdf = true;

    try {
      const fileName = this.getPdfFilename(mode);
      const pdfBlob = await firstValueFrom(this.kundaliService.generateReportPdf(this.buildReportPdfPayload(mode)));
      this.saveBlob(pdfBlob, fileName);
    }
    finally {
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
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

  private startJanmaStory(response?: any): void {
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

  toggleJanmaStoryRail(): void {
    this.isJanmaStoryRailCollapsed = !this.isJanmaStoryRailCollapsed;
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
      const formValue = this.kundaliForm.getRawValue();
      const response = this.kundaliResponse;
      const session = {
        version: 1,
        createdAt: new Date().toISOString(),
        birthProfile: {
          name: formValue.fullName ?? '',
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





