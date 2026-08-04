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
import { mapDhruvaKundaliContext } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.mapper';
import { DhruvaKundaliContextService } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.service';
import { RemediesRecommendationComponent } from "../../components/remedies-recommendation/remedies-recommendation.component";
import {
  AstrologerBrandingSetting,
  AstrologerBrandingSettingsService
} from '../../core/services/astrologer-branding-settings.service';
import { NotificationService } from '../../shared/services/notification.service';
import { ThemeService } from '../../core/services/theme.service';

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
  private dhruvaKundaliContext = inject(DhruvaKundaliContextService);
  private apiTerms = inject(ApiTermTranslationService);
  private brandingSettingsService = inject(AstrologerBrandingSettingsService);
  private readonly notifications = inject(NotificationService);
  private readonly themeService = inject(ThemeService);
  private route = inject(ActivatedRoute);
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
  isJanmaStoryRailCollapsed = true;
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
  activeReportSection = '';

  get hasReportStatus(): boolean {
    return !!(this.saveMessage || this.generationMessage || this.pdfDownloadMessage || this.shareMessage);
  }

  get currentYogaActivationGroups(): ActivationPeriodGroup[] {
    return this.buildActivationPeriodGroups(this.yogas, 'current', 'Current Yoga Activation');
  }

  get upcomingYogaActivationGroups(): ActivationPeriodGroup[] {
    return this.buildActivationPeriodGroups(this.yogas, 'upcoming', 'Upcoming Yoga Activation');
  }

  get natalYogaItems(): NatalGroupedItem[] {
    return this.getNatalOnlyItems(this.yogas);
  }

  get currentDoshaActivationGroups(): ActivationPeriodGroup[] {
    return this.buildActivationPeriodGroups(this.doshas, 'current', 'Current Dosha Activation');
  }

  get upcomingDoshaActivationGroups(): ActivationPeriodGroup[] {
    return this.buildActivationPeriodGroups(this.doshas, 'upcoming', 'Upcoming Dosha Activation');
  }

  get natalDoshaItems(): NatalGroupedItem[] {
    return this.getNatalOnlyItems(this.doshas);
  }
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
    aries: { symbol: 'ðŸ', indianName: 'Mesha', englishName: 'Aries' },
    mesha: { symbol: 'ðŸ', indianName: 'Mesha', englishName: 'Aries' },
    taurus: { symbol: 'ðŸ‚', indianName: 'Vrishabha', englishName: 'Taurus' },
    vrishabha: { symbol: 'ðŸ‚', indianName: 'Vrishabha', englishName: 'Taurus' },
    gemini: { symbol: 'ðŸ‘¥', indianName: 'Mithuna', englishName: 'Gemini' },
    mithuna: { symbol: 'ðŸ‘¥', indianName: 'Mithuna', englishName: 'Gemini' },
    cancer: { symbol: 'ðŸ¦€', indianName: 'Karka', englishName: 'Cancer' },
    karka: { symbol: 'ðŸ¦€', indianName: 'Karka', englishName: 'Cancer' },
    karkataka: { symbol: 'ðŸ¦€', indianName: 'Karka', englishName: 'Cancer' },
    leo: { symbol: 'ðŸ¦', indianName: 'Simha', englishName: 'Leo' },
    simha: { symbol: 'ðŸ¦', indianName: 'Simha', englishName: 'Leo' },
    sinh: { symbol: 'ðŸ¦', indianName: 'Simha', englishName: 'Leo' },
    virgo: { symbol: 'ðŸŒ¾', indianName: 'Kanya', englishName: 'Virgo' },
    kanya: { symbol: 'ðŸŒ¾', indianName: 'Kanya', englishName: 'Virgo' },
    libra: { symbol: 'âš–', indianName: 'Tula', englishName: 'Libra' },
    tula: { symbol: 'âš–', indianName: 'Tula', englishName: 'Libra' },
    scorpio: { symbol: 'ðŸ¦‚', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrischika: { symbol: 'ðŸ¦‚', indianName: 'Vrischika', englishName: 'Scorpio' },
    vrishchika: { symbol: 'ðŸ¦‚', indianName: 'Vrischika', englishName: 'Scorpio' },
    sagittarius: { symbol: 'ðŸ¹', indianName: 'Dhanu', englishName: 'Sagittarius' },
    dhanu: { symbol: 'ðŸ¹', indianName: 'Dhanu', englishName: 'Sagittarius' },
    capricorn: { symbol: 'ðŸ', indianName: 'Makara', englishName: 'Capricorn' },
    makara: { symbol: 'ðŸ', indianName: 'Makara', englishName: 'Capricorn' },
    aquarius: { symbol: 'ðŸº', indianName: 'Kumbha', englishName: 'Aquarius' },
    kumbha: { symbol: 'ðŸº', indianName: 'Kumbha', englishName: 'Aquarius' },
    pisces: { symbol: 'ðŸŸðŸŸ', indianName: 'Meena', englishName: 'Pisces' },
    meena: { symbol: 'ðŸŸðŸŸ', indianName: 'Meena', englishName: 'Pisces' },
    meen: { symbol: 'ðŸŸðŸŸ', indianName: 'Meena', englishName: 'Pisces' }
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
    this.saveFailed = false;
    this.notifications.success('Kundali saved for Current Life Focus.');
    window.dispatchEvent(new CustomEvent('kundali-saved'));
    this.cdr.detectChanges();
  }

  saveCurrentKundali(): void {
    if (!this.kundaliResponse || this.savingKundali) { return; }
    this.savingKundali = true;
    this.saveMessage = '';
    this.saveFailed = false;
    const payload = { ...this.buildKundaliPayload(), chartData: this.kundaliResponse };
    this.kundaliService.saveKundali(payload).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? saved?.kundaliId ?? this.savedKundaliId;
        this.saveFailed = false;
        this.notifications.success('Kundali saved.');
        this.savingKundali = false;
        window.dispatchEvent(new CustomEvent('kundali-saved'));
        this.cdr.detectChanges();
      },
      error: (error) => {
        const message = this.getSaveErrorMessage(error);
        this.saveFailed = true;
        this.notifications.error(message);
        this.savingKundali = false;
        this.cdr.detectChanges();
      }
    });
  }

  private getSaveErrorMessage(error: any): string {
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
    this.shareFailed = false;

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

      this.shareFailed = false;
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
      return 'â€”';
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
    this.dhruvaKundaliContext.clearContext();
    this.selectedHouseData = null;
    this.activeDivisionalChart = 'd1';
    this.currentLifeFocusResetKey++;
    this.generationMessage = '';
    this.generationFailed = false;
    if (this.kundaliForm.invalid) {
      this.kundaliForm.markAllAsTouched();
      this.generationFailed = true;
      this.notifications.warning('Please complete the required birth details before generating the Kundali.');
      return;
    }

    this.loading = true;
    const payload = this.buildKundaliPayload();
    this.kundaliService.generateKundali(payload)
      .subscribe({
        next: (response) => {
          console.log('Generated:', response);
          this.kundaliResponse = response;
          this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext(response));
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
          this.showBirthDetailsForm = false;
          this.savedKundaliId = null;
          this.saveMessage = '';
          this.notifications.success('Kundali generated.');
          this.generationFailed = false;
          this.latestKundaliStorage.save(payload);
          this.prepareJanmaStory(response);
          this.setDefaultHouseSelection();
          this.cdr.detectChanges();
          console.log('houseScoreslog'  , this.houseScores);
          console.log('houseScoreslog2'  ,  response.houses);
          console.log(this.planets);

          this.dashaApiService.getMahadashaPeriods(payload).subscribe({
            next: (mahaResponse) => {
              console.log('Mahadasha periods:', mahaResponse);
              this.mahadashaPeriods = mahaResponse;
              this.prepareJanmaStory(response);
              this.loading = false;
              this.cdr.detectChanges();
              console.log('Mahadasha periods:', this.mahadashaPeriods);
            },
            error: (error) => {
              console.error('Mahadasha periods error:', error);
              this.generationFailed = true;
              this.notifications.warning('Kundali generated, but the Dasha timeline could not be loaded right now.');
              this.loading = false;
              this.cdr.detectChanges();
            }
          });
        },
        error: (error) => {
          console.error('Kundali generation error:', error);
          this.resetGeneratedKundaliState();
          this.generationFailed = true;
          this.resolveKundaliGenerationError(error).then(message => {
            this.notifications.error(message);
            this.loading = false;
            this.cdr.detectChanges();
          });
        }
      });
  }
  private resetGeneratedKundaliState(): void {
    this.kundaliResponse = null;
    this.planets = [];
    this.d1Planets = [];
    this.d1Houses = [];
    this.summary = null;
    this.d1Ascendant = '';
    this.d9Chart = null;
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
    this.activeDivisionalChart = 'd1';
    this.saveMessage = '';

    this.kundaliService.getSavedKundali(id).subscribe({
      next: (saved) => {
        this.savedKundaliId = saved?.id ?? id;
        this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext(saved));
        this.patchFormFromSavedKundali(saved);
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
    this.dhruvaKundaliContext.setContext(mapDhruvaKundaliContext({ kundaliId: this.savedKundaliId, chartData: response }));
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
    this.showBirthDetailsForm = false;
    this.setDefaultHouseSelection();
  }

  editBirthDetails(): void {
    this.showBirthDetailsForm = true;
    this.cdr.detectChanges();
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
    return items
      .map((item, index) => ({ item, index }))
      .filter(({ item }) => !this.hasActivationPeriod(item));
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
    const period = this.formatActivationPeriod(activation);
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
      return 'Report PDF generation is taking longer than expected. Please try again in a moment.';
    }

    if (error && typeof error === 'object') {
      const status = 'status' in error ? Number((error as { status?: unknown }).status) : 0;

      if (status === 429) {
        return this.buildReportRateLimitMessage(error);
      }

      if (!('error' in error)) {
        return fallbackMessage;
      }

      const responseError = (error as { error?: unknown }).error;

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
    this.displayPersonName = this.formatPersonName(this.kundaliForm.value.fullName);
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

