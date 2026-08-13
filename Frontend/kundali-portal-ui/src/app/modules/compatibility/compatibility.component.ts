import { ChangeDetectorRef, Component, HostListener, ViewChild, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';
import { AstrologyIconComponent } from '../../shared/components/astrology-icon.component';
import { ScoreDetailsModalComponent } from '../../components/score-details-modal/score-details-modal';
import { CompatibilityApiService, CompatibilityRequest, MatchRequestPerson, SavedCompatibilityDetail } from '../../core/services/compatibility-api.service';
import { BirthChart, CompatibilityDoshaItem, CompatibilityResponse, CompatibilityScoreComponent } from '../../shared/models/compatibility.model';
import { LocationService } from '../../core/services/location.service';
import { firstValueFrom, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { NotificationService } from '../../shared/services/notification.service';
import { ThemeService } from '../../core/services/theme.service';
import { SavedChartSelectorComponent } from '../../shared/components/saved-chart-selector/saved-chart-selector.component';
import { SavedChartMapperService, SavedKundaliDetail } from '../../shared/services/saved-chart-mapper.service';
import { PortalNotesComponent } from '../../shared/components/portal-notes/portal-notes.component';

interface PersonSummary {
  role: 'Boy' | 'Girl';
  name: string;
  gender: string;
  moonSign: string;
  nakshatra: string;
  ascendant: string;
  birthDate: string;
  birthTime: string;
  birthPlace: string;
}

interface CompatibilityAspectCard {
  name: string;
  score: number;
  grade: string;
  tone: string;
  summary: string;
  confidence: number;
  factors: string[];
  scoreBreakdown: CompatibilityScoreComponent[];
}

interface CompatibilityDoshaCard extends CompatibilityDoshaItem {
  detail: string;
  whoHas: string;
  isDetected: boolean;
}

@Component({
  selector: 'app-compatibility',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, NorthIndianChartComponent, AstrologyIconComponent, ScoreDetailsModalComponent, TranslocoPipe, SavedChartSelectorComponent, PortalNotesComponent],
  templateUrl: './compatibility.component.html',
  styleUrls: ['./compatibility.component.scss'],
})
export class CompatibilityComponent {
  @ViewChild(PortalNotesComponent) private portalNotes?: PortalNotesComponent;

  private fb = inject(FormBuilder);
  private compatibilityApi = inject(CompatibilityApiService);
  private route = inject(ActivatedRoute);
  private locationService = inject(LocationService);
  private cdr = inject(ChangeDetectorRef);
  private transloco = inject(TranslocoService);
  private notifications = inject(NotificationService);
  private readonly themeService = inject(ThemeService);
  private readonly savedChartMapper = inject(SavedChartMapperService);

  get reportBrandLogoSrc(): string {
    return this.themeService.mode() === 'dark'
      ? 'assets/brand/horalife/horalife-icon-white.svg'
      : 'assets/brand/horalife/horalife-icon-color.svg';
  }
  loading = false;
  exportingPdf = false;
  savingCompatibility = false;
  shareEmail = '';
  shareEmailTouched = false;
  shareSending = false;
  shareFailed = false;
  loadingSavedCompatibility = false;
  savedCompatibilityId: string | null = null;
  isNotesOpen = false;
  selectedBoyChartId: string | null = null;
  selectedGirlChartId: string | null = null;
  showReport=false;
  showBirthDetailsForm = true;
  errorMessage = '';
  boyLocationResults: any[] = [];
  girlLocationResults: any[] = [];
  result: CompatibilityResponse | null = null;
  personAhouses: any = null;
  personBhouses: any = null;
  personAplanetPositions: any = null;
  personBplanetPositions: any = null;
  personAchart: BirthChart | null = null;
  personBchart: BirthChart | null = null;
  selectedCompatibilityAspect: CompatibilityAspectCard | null = null;
  isCompatibilityScoreModalOpen = false;
  selectedDosha: CompatibilityDoshaCard | null = null;
  isDoshaModalOpen = false;
  //result: any | null = null;
  readonly form = this.fb.group({
    boyName: ['Kishor Sharma', Validators.required],
    boyBirthDate: ['1986-10-06', Validators.required],
    boyBirthTime: ['11:45', Validators.required],
    boyBirthPlace: ['Hingoli, Maharashtra, India', Validators.required],
    boyLatitude: [19.5431163, Validators.required],
    boyLongitude: [77.1739432, Validators.required],
    girlName: ['Pooja Vyas', Validators.required],
    girlBirthDate: ['1991-12-01', Validators.required],
    girlBirthTime: ['02:15', Validators.required],
    girlBirthPlace: ['Aurangabad, Maharashtra, India', Validators.required],
    girlLatitude: [19.8762, Validators.required],
    girlLongitude: [75.3433, Validators.required],
  });

  ngOnInit(): void {
    this.form.controls.boyBirthPlace.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((value) => value && value.length >= 3 ? this.locationService.searchLocation(value) : of([]))
      )
      .subscribe((results) => this.boyLocationResults = results);

    this.form.controls.girlBirthPlace.valueChanges
      .pipe(
        debounceTime(500),
        distinctUntilChanged(),
        switchMap((value) => value && value.length >= 3 ? this.locationService.searchLocation(value) : of([]))
      )
      .subscribe((results) => this.girlLocationResults = results);

    this.route.paramMap.subscribe(params => {
      const savedId = params.get('id');
      if (savedId) {
        this.loadSavedCompatibility(savedId);
      }
    });
  }

  selectLocation(role: 'boy' | 'girl', place: any): void {
    this.form.patchValue({
      [`${role}BirthPlace`]: place.display_name,
      [`${role}Latitude`]: Number(place.lat),
      [`${role}Longitude`]: Number(place.lon),
    });

    if (role === 'boy') {
      this.boyLocationResults = [];
    } else {
      this.girlLocationResults = [];
    }
  }

  onSavedChartSelected(role: 'boy' | 'girl', chart: SavedKundaliDetail): void {
    const fallbackGender = role === 'boy' ? 'Male' : 'Female';
    const person = this.savedChartMapper.toMatchPerson(chart, fallbackGender);

    this.form.patchValue({
      [`${role}Name`]: person.name,
      [`${role}BirthDate`]: this.savedChartMapper.toDateInput(chart),
      [`${role}BirthTime`]: this.savedChartMapper.toTimeInput(chart),
      [`${role}BirthPlace`]: person.birthPlace,
      [`${role}Latitude`]: person.latitude,
      [`${role}Longitude`]: person.longitude,
    });

    if (role === 'boy') {
      this.selectedBoyChartId = chart.id;
      this.boyLocationResults = [];
    } else {
      this.selectedGirlChartId = chart.id;
      this.girlLocationResults = [];
    }

    this.savedCompatibilityId = null;
    this.result = null;
    this.showReport = false;
    this.showBirthDetailsForm = true;
    this.personAhouses = null;
    this.personBhouses = null;
    this.personAplanetPositions = null;
    this.personBplanetPositions = null;
    this.personAchart = null;
    this.personBchart = null;
    this.cdr.markForCheck();
  }

  editBirthDetails(): void {
    this.showBirthDetailsForm = true;
    this.cdr.markForCheck();
  }
  readonly kutaTones = ['orange', 'blue', 'purple', 'pink', 'green', 'orange', 'blue', 'purple'];

  get marriagePeriods() {
    return [
      { period: this.t('compatibility.periodMayAug2025'), grade: this.t('compatibility.veryAuspicious'), tone: 'green' },
      { period: this.t('compatibility.periodJanApr2026'), grade: this.t('compatibility.auspicious'), tone: 'blue' },
      { period: this.t('compatibility.periodSepDec2026'), grade: this.t('compatibility.good'), tone: 'purple' },
      { period: this.t('compatibility.periodMarJun2027'), grade: this.t('compatibility.auspicious'), tone: 'pink' },
    ];
  }

  get boy(): PersonSummary {
    return {
      role: 'Boy',
      name: this.form.value.boyName || 'Rahul Sharma',
      gender: 'Male',
      moonSign: 'Leo (सिंह)',
      nakshatra: 'Magha',
      ascendant: 'Aries (मेष)',
      birthDate: this.formatDate(this.form.value.boyBirthDate),
      birthTime: this.form.value.boyBirthTime || '11:45',
      birthPlace: this.form.value.boyBirthPlace || 'Hingoli, Maharashtra, India',
    };
  }

  get girl(): PersonSummary {
    return {
      role: 'Girl',
      name: this.form.value.girlName || 'Priya Verma',
      gender: 'Female',
      moonSign: 'Taurus (वृषभ)',
      nakshatra: 'Rohini',
      ascendant: 'Cancer (कर्क)',
      birthDate: this.formatDate(this.form.value.girlBirthDate),
      birthTime: this.form.value.girlBirthTime || '09:20',
      birthPlace: this.form.value.girlBirthPlace || 'Aurangabad, Maharashtra, India',
    };
  }

  get overallScore(): number {
    return this.result?.overallCompatibilityScore ?? 78;
  }

  get scoreLabel(): string {
    if (this.overallScore >= 75) return this.t('compatibility.excellentMatch');
    if (this.overallScore >= 60) return this.t('compatibility.goodMatch');
    if (this.overallScore >= 45) return this.t('compatibility.moderateMatch');
    return this.t('compatibility.needsCarefulReview');
  }

  get totalKutaScore(): number {
    return this.result?.ashtakoota?.totalScore ?? 0;
  }

  get totalKutaLabel(): string {
    const score = this.totalKutaScore;
    if (score >= 28) return this.t('compatibility.excellentCompatibility');
    if (score >= 22) return this.t('compatibility.goodCompatibility');
    if (score >= 18) return this.t('compatibility.goodCompatibility');
    return this.t('compatibility.needsReview');
  }

  get kutaScores() {
    return (this.result?.ashtakoota?.scores ?? []).map((item, index) => ({
      name: this.translateCompatibilityTerm(item.name),
      score: item.score,
      max: item.maxScore,
      grade: this.translateCompatibilityTerm(item.grade),
      detail: item.detail,
      tone: this.kutaTones[index % this.kutaTones.length],
    }));
  }

  get compatibilityAspects() {
    const fallback = [
      { name: this.t('compatibility.emotionalCompatibility'), score: 82, grade: this.t('compatibility.veryStrong'), tone: 'green', summary: this.t('compatibility.sampleEmotionalScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
      { name: this.t('compatibility.communication'), score: 75, grade: this.t('compatibility.strong'), tone: 'blue', summary: this.t('compatibility.sampleCommunicationScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
      { name: this.t('compatibility.financialHarmony'), score: 70, grade: this.t('compatibility.good'), tone: 'purple', summary: this.t('compatibility.sampleFinancialScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
      { name: this.t('compatibility.physicalCompatibility'), score: 80, grade: this.t('compatibility.veryStrong'), tone: 'pink', summary: this.t('compatibility.sampleAttractionScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
      { name: this.t('compatibility.spiritualAlignment'), score: 85, grade: this.t('compatibility.excellent'), tone: 'orange', summary: this.t('compatibility.sampleSpiritualScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
      { name: this.t('compatibility.familyHarmony'), score: 78, grade: this.t('compatibility.strong'), tone: 'blue', summary: this.t('compatibility.sampleFamilyScore'), confidence: 0, factors: [this.t('compatibility.generateReportForFactors')], scoreBreakdown: [] },
    ];

    if (!this.result?.dimensions?.length) {
      return fallback;
    }

    return this.result.dimensions.map((dimension, index) => ({
      name: this.translateCompatibilityTerm(dimension.name),
      score: dimension.score,
      grade: this.getGrade(dimension.score),
      tone: fallback[index % fallback.length].tone,
      summary: dimension.summary,
      confidence: dimension.confidence,
      factors: dimension.factors ?? [],
      scoreBreakdown: dimension.scoreBreakdown ?? [],
    }));
  }

  get doshas(): CompatibilityDoshaCard[] {
    return (this.result?.doshaAnalysis?.items ?? []).map((dosha) => ({
      ...dosha,
      name: this.translateCompatibilityTerm(dosha.name),
      status: this.translateCompatibilityTerm(dosha.status),
      severity: this.translateCompatibilityTerm(dosha.severity),
      factors: dosha.factors ?? [],
      remedies: dosha.remedies ?? [],
      references: dosha.references ?? [],
      scoreBreakdown: dosha.scoreBreakdown ?? [],
      detail: dosha.reason || this.t('compatibility.noDoshaPressure'),
      whoHas: this.getDoshaOwnership(dosha),
      isDetected: this.isDoshaDetected(dosha),
    }));
  }

  getOverallDoshaAssessmentLabel(score: number | null | undefined): string {
    const value = Number(score ?? 0);
    if (value <= 20) return 'Low concern';
    if (value <= 50) return 'Moderate concern';
    if (value <= 75) return 'Needs careful review';
    return 'High concern';
  }

  getDoshaStatusLabel(dosha: CompatibilityDoshaCard): string {
    if (!dosha.isDetected) {
      return 'Status: Not detected';
    }

    const presence = dosha.personA?.isPresent && dosha.personB?.isPresent
      ? 'Present in both charts'
      : 'Present in one chart';
    return `Status: ${presence}`;
  }

  getDoshaImpactLabel(dosha: CompatibilityDoshaCard): string {
    return dosha.isDetected
      ? `Compatibility impact: ${dosha.severity}`
      : `Compatibility impact: ${this.t('compatibility.none')}`;
  }

  getDoshaAffectedPersonLabel(dosha: CompatibilityDoshaCard): string {
    return dosha.isDetected
      ? `Affected person: ${dosha.whoHas}`
      : `Affected person: ${this.t('compatibility.none')}`;
  }

  get doshaRemedies(): string[] {
    return this.result?.doshaAnalysis?.doshaRemedies ?? [];
  }

  get doshaRemedyTitle(): string {
    const status = this.result?.doshaAnalysis?.overallDoshaStatus;
    if (!status || status === 'No Dosha') {
      return this.t('compatibility.noActiveDoshaRemediesRequired');
    }

    return `${this.translateCompatibilityTerm(status)}. ${this.t('compatibility.suggestedRemedies')}`;
  }

  get insights() {
    const strengths = this.result?.dominantStrengths?.length
      ? this.result.dominantStrengths
      : [this.t('compatibility.defaultStrengthText')];
    const challenges = this.result?.dominantChallenges?.length
      ? this.result.dominantChallenges
      : [this.t('compatibility.defaultChallengeText')];

    return [
      { title: this.t('compatibility.relationshipStrengths'), text: strengths[0], tone: 'green' },
      { title: this.t('compatibility.potentialChallenges'), text: challenges[0], tone: 'orange' },
      { title: this.t('compatibility.marriageStability'), text: this.t('compatibility.marriageStabilityText'), tone: 'blue' },
      { title: this.t('compatibility.careerSupport'), text: this.t('compatibility.careerSupportText'), tone: 'purple' },
      { title: this.t('compatibility.emotionalBonding'), text: this.result?.summary ?? this.t('compatibility.emotionalBondingText'), tone: 'pink' },
      { title: this.t('compatibility.familyLife'), text: this.t('compatibility.familyLifeText'), tone: 'orange' },
    ];
  }

  analyzeCompatibility(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.cdr.detectChanges();
      return;
    }

    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.compatibilityApi.match({
      personA: this.toPersonRequest('boy'),
      personB: this.toPersonRequest('girl'),
    }).subscribe({
      next: (result) => {
        this.savedCompatibilityId = null;
        this.applyCompatibilityResult(result);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.errorMessage = error.message || this.t('compatibility.calculateError');
        this.notifications.error(this.errorMessage);
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  saveCompatibility(): void {
    if (!this.result || this.savingCompatibility) {
      return;
    }

    this.savingCompatibility = true;
    this.errorMessage = '';
    this.compatibilityApi.saveCompatibility({
      request: this.buildCompatibilityRequest(),
      result: this.result,
    }).subscribe({
      next: (saved) => {
        this.savedCompatibilityId = saved?.id ?? this.savedCompatibilityId;
        this.savingCompatibility = false;
        this.notifications.success('Compatibility saved.');
        window.dispatchEvent(new CustomEvent('compatibility-saved'));
        this.cdr.detectChanges();
      },
      error: (error) => {
        const message = error?.message || 'Unable to save compatibility.';
        this.errorMessage = message;
        this.savingCompatibility = false;
        this.notifications.error(message);
        this.cdr.detectChanges();
      }
    });
  }
  openNotes(): void {
    this.isNotesOpen = true;
  }

  closeNotes(): void {
    this.isNotesOpen = false;
  }

  private saveDraftNotesForSavedCompatibility(compatibilityId: string): void {
    if (!compatibilityId || !this.portalNotes?.hasPendingDraft) {
      return;
    }

    this.portalNotes.saveForContext(compatibilityId);
  }
  openCompatibilityFactors(aspect: CompatibilityAspectCard): void {
    this.selectedCompatibilityAspect = aspect;
    this.isCompatibilityScoreModalOpen = true;
  }

  closeCompatibilityFactors(): void {
    this.isCompatibilityScoreModalOpen = false;
    setTimeout(() => {
      this.selectedCompatibilityAspect = null;
    }, 300);
  }

  openDoshaFactors(dosha: CompatibilityDoshaCard): void {
    this.selectedDosha = dosha;
    this.isDoshaModalOpen = true;
  }

  closeDoshaFactors(): void {
    this.isDoshaModalOpen = false;
    setTimeout(() => {
      this.selectedDosha = null;
    }, 300);
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
    this.shareFailed = false;
  }

  @HostListener('document:click', ['$event'])
  closeReportPickersOnOutsideClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (target?.closest('.compatibility-actions')) {
      return;
    }

    document
      .querySelectorAll<HTMLDetailsElement>('.compatibility-actions details[open]')
      .forEach(picker => picker.removeAttribute('open'));
  }

  onReportPickerToggle(event: Event): void {
    const currentPicker = event.currentTarget as HTMLDetailsElement | null;
    if (!currentPicker?.open) {
      return;
    }

    document
      .querySelectorAll<HTMLDetailsElement>('.compatibility-actions details[open]')
      .forEach(picker => {
        if (picker !== currentPicker) {
          picker.removeAttribute('open');
        }
      });
  }

  async sendCompatibilityReportByEmail(): Promise<void> {
    this.shareEmailTouched = true;
    this.shareFailed = false;

    if (!this.isShareEmailValid || this.shareSending) {
      return;
    }

    if (!this.result || !this.showReport) {
      this.shareFailed = true;
      this.notifications.warning('Generate a compatibility report before sharing.');
      return;
    }

    this.shareSending = true;
    this.exportingPdf = true;

    try {
      const pdf = await this.buildCompatibilityPdfDocument();
      const fileName = this.getCompatibilityPdfFileName();
      const subject = `HoraLife Compatibility Report - ${this.boy.name} & ${this.girl.name}`;
      const message = [
        'Namaste,',
        `Please find attached the compatibility PDF report for ${this.boy.name} and ${this.girl.name}.`,
        `Overall compatibility: ${this.overallScore}% (${this.scoreLabel})`,
        `Ashtakoota score: ${this.totalKutaScore} / 36 (${this.totalKutaLabel})`,
        this.savedCompatibilityId ? `Saved match ID: ${this.savedCompatibilityId}` : '',
        'Regards,\nHoraLife Precision Vedic Astrology'
      ].filter(Boolean).join('\n\n');

      await firstValueFrom(this.compatibilityApi.sendReportEmail({
        toEmail: this.shareEmail.trim(),
        subject,
        message,
        fileName,
        mode: 'compatibility',
        attachmentFileName: fileName,
        attachmentContentType: 'application/pdf',
        attachmentBase64: pdf.output('datauristring'),
        birthProfile: this.buildCompatibilityEmailProfile(),
        report: this.result,
      }));

      this.shareFailed = false;
      this.notifications.success('Compatibility report email submitted for delivery.');
      document
        .querySelectorAll<HTMLDetailsElement>('.compatibility-actions details[open]')
        .forEach(picker => picker.removeAttribute('open'));
    } catch (error) {
      console.error('Compatibility report email error:', error);
      this.shareFailed = true;
      this.notifications.error(await this.resolveReportEmailError(error));
    } finally {
      this.shareSending = false;
      this.resetPdfExportState();
    }
  }

  shareCompatibilityOnWhatsApp(): void {
    if (!this.showReport) {
      return;
    }

    const text = this.getCompatibilityShareText();
    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank', 'noopener,noreferrer');
  }

  private getCompatibilityShareText(): string {
    return [
      `HoraLife Compatibility Report`,
      `${this.boy.name} & ${this.girl.name}`,
      `Overall compatibility: ${this.overallScore}% (${this.scoreLabel})`,
      `Ashtakoota score: ${this.totalKutaScore} / 36 (${this.totalKutaLabel})`,
      this.result?.summary ? `Summary: ${this.result.summary}` : '',
      '',
      'Prepared with HoraLife Precision Vedic Astrology.'
    ].filter(Boolean).join('\n');
  }

  private buildCompatibilityEmailProfile(): Record<string, unknown> {
    return {
      reportType: 'Compatibility',
      personA: this.toPersonRequest('boy'),
      personB: this.toPersonRequest('girl'),
      displayName: `${this.boy.name} & ${this.girl.name}`,
      savedCompatibilityId: this.savedCompatibilityId,
      generatedAt: new Date().toISOString(),
    };
  }

  private async resolveReportEmailError(error: unknown): Promise<string> {
    const fallbackMessage = 'Unable to send compatibility report email right now.';
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
        return 'The compatibility PDF is too large to send by email. Please download it instead.';
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

  private async extractReportErrorMessage(responseError: unknown): Promise<string> {
    if (!responseError) {
      return '';
    }

    if (responseError instanceof Blob) {
      try {
        const text = await responseError.text();
        const parsed = JSON.parse(text) as { message?: unknown; title?: unknown };
        return String(parsed.message || parsed.title || '').trim();
      } catch {
        return '';
      }
    }

    if (typeof responseError === 'string') {
      return responseError.trim();
    }

    if (typeof responseError === 'object' && 'message' in responseError) {
      return String((responseError as { message?: unknown }).message || '').trim();
    }

    return '';
  }

  async downloadPdf(): Promise<void> {
    if (!this.showReport || this.exportingPdf) {
      return;
    }

    this.exportingPdf = true;
    const exportWatchdog = window.setTimeout(() => this.resetPdfExportState(), 30000);

    try {
      const pdf = await this.buildCompatibilityPdfDocument();
      pdf.save(this.getCompatibilityPdfFileName());
      this.notifications.success('Compatibility PDF downloaded.');
    } catch (error) {
      console.error('Compatibility PDF export failed:', error);
      this.errorMessage = this.t('compatibility.pdfError');
      this.notifications.error(this.errorMessage);
    } finally {
      window.clearTimeout(exportWatchdog);
      this.resetPdfExportState();
    }
  }

  private async buildCompatibilityPdfDocument(): Promise<jsPDF> {
    const report = document.getElementById('compatibility-report');

    if (!report || !this.showReport) {
      throw new Error('Generate a compatibility report before exporting.');
    }

    this.isCompatibilityScoreModalOpen = false;
    this.isDoshaModalOpen = false;
    this.selectedCompatibilityAspect = null;
    this.selectedDosha = null;
    this.cdr.detectChanges();

    await new Promise((resolve) => setTimeout(resolve, 350));
    await this.waitForPdfFonts();
    document.body.classList.add('compatibility-pdf-mode');
    this.cdr.detectChanges();
    await this.waitForPdfFonts();
    await new Promise((resolve) => setTimeout(resolve, 180));

    const pdf = new jsPDF({
      orientation: 'p',
      unit: 'mm',
      format: 'a4',
      compress: false,
    });

    const pdfWidth = pdf.internal.pageSize.getWidth();
    const pdfHeight = pdf.internal.pageSize.getHeight();
    const elementsToRender = Array.from(
      report.querySelectorAll<HTMLElement>(':scope > .compatibility-report-intro, :scope > .hero-report, :scope > .report-section, :scope > .report-note')
    );

    let pageHeightUsed = 0;
    for (const element of elementsToRender) {
      pageHeightUsed = await this.addElementToPdf(pdf, element, pdfWidth, pdfHeight, 6, pageHeightUsed, 4);
    }

    return pdf;
  }

  private resetPdfExportState(): void {
    document.body.classList.remove('compatibility-pdf-mode');
    this.exportingPdf = false;
    this.cdr.detectChanges();
  }

  private getPdfImageData(canvas: HTMLCanvasElement): string {
    return canvas.toDataURL('image/png');
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

  private async waitForPdfFonts(): Promise<void> {
    const fonts = document.fonts;
    if (!fonts) {
      return;
    }

    try {
      await fonts.ready;
    } catch {
      // Continue export with fallback fonts if the browser reports a font loading issue.
    }
  }
  private applyHtml2CanvasSafeStyles(clonedDocument: Document): void {
    const style = clonedDocument.createElement('style');
    style.textContent = `
      :root, body, body.compatibility-pdf-mode {
        --theme-bg: #fffaf5 !important;
        --theme-surface: #ffffff !important;
        --theme-surface-2: #fffaf7 !important;
        --theme-input-bg: #ffffff !important;
        --theme-text: #2a170f !important;
        --theme-text-muted: #765c4f !important;
        --theme-line: #f0d9bf !important;
        --theme-line-soft: #f7e9d8 !important;
        --theme-accent: #d65f00 !important;
        --theme-info: #2563eb !important;
        --theme-success: #16a34a !important;
        --theme-warning: #f28c12 !important;
        --theme-danger: #e84d74 !important;
        --theme-shadow: transparent !important;
      }

      body.compatibility-pdf-mode,
      body.compatibility-pdf-mode app-root,
      body.compatibility-pdf-mode .compatibility-page {
        background: #fffaf5 !important;
        color: #2a170f !important;
      }

      body.compatibility-pdf-mode .report-shell,
      body.compatibility-pdf-mode .report-section,
      body.compatibility-pdf-mode .hero-report {
        background: #ffffff !important;
        color: #2a170f !important;
        box-shadow: none !important;
      }

      body.compatibility-pdf-mode .compatibility-report-intro.premium-report-header {
        border-top-color: #e15f3d !important;
        background: linear-gradient(180deg, #ffffff 0%, #fffaf7 100%) !important;
      }

      body.compatibility-pdf-mode .compatibility-brand-copy h1,
      body.compatibility-pdf-mode .input-heading h2,
      body.compatibility-pdf-mode .report-toolbar h1,
      body.compatibility-pdf-mode .report-section h2,
      body.compatibility-pdf-mode .person-form h3 {
        color: #5b1f0c !important;
      }

      body.compatibility-pdf-mode .compatibility-brand-copy h1 span:last-child,
      body.compatibility-pdf-mode .premium-report-header .compatibility-owner-statement strong,
      body.compatibility-pdf-mode .report-kicker {
        color: #e15f3d !important;
      }

      body.compatibility-pdf-mode .compatibility-brand-copy small,
      body.compatibility-pdf-mode .premium-report-header .report-pandit-block span:not(.report-kicker),
      body.compatibility-pdf-mode .premium-report-header .report-pandit-block p,
      body.compatibility-pdf-mode .profile-card dt,
      body.compatibility-pdf-mode .kuta-card small,
      body.compatibility-pdf-mode .dosha-card p,
      body.compatibility-pdf-mode .insight-card p {
        color: #765c4f !important;
      }

      body.compatibility-pdf-mode .profile-card,
      body.compatibility-pdf-mode .kuta-card,
      body.compatibility-pdf-mode .aspect-card,
      body.compatibility-pdf-mode .dosha-card,
      body.compatibility-pdf-mode .insight-card,
      body.compatibility-pdf-mode .period-card,
      body.compatibility-pdf-mode .total-score-row,
      body.compatibility-pdf-mode .dosha-summary,
      body.compatibility-pdf-mode .chart-comparison {
        border-color: #f0d9bf !important;
        color: #2a170f !important;
        box-shadow: none !important;
      }

      body.compatibility-pdf-mode .profile-card {
        background: linear-gradient(135deg, #fff8ec, #ffffff) !important;
      }

      body.compatibility-pdf-mode .profile-card--girl {
        background: linear-gradient(135deg, #fff6f8, #ffffff) !important;
      }

      body.compatibility-pdf-mode .tone-orange { color: #f28c12 !important; background-color: #fff8ec !important; border-color: #f3c987 !important; }
      body.compatibility-pdf-mode .tone-blue { color: #2563eb !important; background-color: #f6f9ff !important; border-color: #cbdcff !important; }
      body.compatibility-pdf-mode .tone-green { color: #16a34a !important; background-color: #f5fff8 !important; border-color: #c9efd4 !important; }
      body.compatibility-pdf-mode .tone-pink { color: #e84d74 !important; background-color: #fff6f8 !important; border-color: #ffd0dc !important; }
      body.compatibility-pdf-mode .tone-purple { color: #8b5cf6 !important; background-color: #fbf7ff !important; border-color: #dec9ff !important; }

      body.compatibility-pdf-mode svg,
      body.compatibility-pdf-mode svg * {
        background-color: transparent !important;
      }

      body.compatibility-pdf-mode svg text,
      body.compatibility-pdf-mode svg tspan {
        fill: #2a170f !important;
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

  private sanitizePdfClone(clonedDocument: Document): void {
    clonedDocument.body.removeAttribute('data-theme');
    clonedDocument.body.classList.remove('dark-theme', 'theme-dark');
    clonedDocument.documentElement.classList.remove('dark-theme', 'theme-dark');
    clonedDocument.querySelectorAll<HTMLElement>('.dark-theme, .theme-dark').forEach((node) => {
      node.classList.remove('dark-theme', 'theme-dark');
    });

    this.applyHtml2CanvasSafeStyles(clonedDocument);
    const unsupportedColorPattern = /(color-mix|oklab|oklch|lab\(|lch\(|color\()/i;

    clonedDocument.querySelectorAll<HTMLElement | SVGElement>('*').forEach((node) => {
      const inlineStyle = node.getAttribute('style');
      if (inlineStyle && unsupportedColorPattern.test(inlineStyle)) {
        node.setAttribute('style', this.sanitizeCssColorFunctions(inlineStyle));
      }

      node.style.setProperty('box-shadow', 'none', 'important');
      node.style.setProperty('text-shadow', 'none', 'important');
    });

    clonedDocument.body.style.setProperty('background-color', '#fffaf5', 'important');
    clonedDocument.body.style.setProperty('color', '#2a170f', 'important');
  }
  private async addElementToPdf(
    pdf: jsPDF,
    element: HTMLElement,
    pdfWidth: number,
    pageHeight: number,
    margin: number,
    pageHeightUsed: number,
    spacing: number
  ): Promise<number> {
    const elementSize = this.getPdfElementSize(element);
    let currentPageHeight = this.isPositiveFinite(pageHeightUsed) ? pageHeightUsed : 0;

    if (!this.isPositiveFinite(elementSize.width) || !this.isPositiveFinite(elementSize.height)) {
      return currentPageHeight;
    }

    const canvas = await html2canvas(element, {
      scale: 4,
      backgroundColor: '#ffffff',
      useCORS: true,
      logging: false,
      windowWidth: elementSize.width,
      width: elementSize.width,
      height: elementSize.height,
      onclone: (clonedDocument: Document) => this.sanitizePdfClone(clonedDocument)
    });

    const usableWidth = pdfWidth - margin * 2;
    const contentHeight = pageHeight - margin * 2;

    if (!this.isPositiveFinite(usableWidth) || !this.isPositiveFinite(contentHeight) || !this.isPositiveFinite(canvas.width) || !this.isPositiveFinite(canvas.height)) {
      return currentPageHeight;
    }

    const renderedHeight = (canvas.height * usableWidth) / canvas.width;

    if (!this.isPositiveFinite(renderedHeight)) {
      return currentPageHeight;
    }

    if (renderedHeight > contentHeight) {
      let startY = 0;
      while (startY < canvas.height) {
        const remainingHeight = canvas.height - startY;
        const sliceHeight = Math.min(remainingHeight, Math.ceil((contentHeight * canvas.width) / usableWidth));

        if (!this.isPositiveFinite(sliceHeight)) {
          break;
        }

        if (currentPageHeight > 0) {
          pdf.addPage();
          currentPageHeight = 0;
        }

        const sliceCanvas = document.createElement('canvas');
        sliceCanvas.width = canvas.width;
        sliceCanvas.height = sliceHeight;
        const context = sliceCanvas.getContext('2d');

        if (!context) {
          break;
        }

        context.fillStyle = '#ffffff';
        context.fillRect(0, 0, canvas.width, sliceHeight);
        context.drawImage(canvas, 0, startY, canvas.width, sliceHeight, 0, 0, canvas.width, sliceHeight);
        const sliceHeightMm = (sliceHeight * usableWidth) / canvas.width;

        if (!this.isPositiveFinite(sliceHeightMm)) {
          break;
        }

        pdf.addImage(this.getPdfImageData(sliceCanvas), 'PNG', margin, margin + currentPageHeight, usableWidth, sliceHeightMm);

        currentPageHeight = sliceHeightMm;
        startY += sliceHeight;
      }

      return currentPageHeight;
    }

    if (currentPageHeight + renderedHeight + spacing > contentHeight) {
      pdf.addPage();
      currentPageHeight = 0;
    }

    pdf.addImage(this.getPdfImageData(canvas), 'PNG', margin, margin + currentPageHeight, usableWidth, renderedHeight);

    return currentPageHeight + renderedHeight + spacing;
  }

  private getCompatibilityPdfFileName(): string {
    const boyName = this.slugify(this.boy.name || 'boy');
    const girlName = this.slugify(this.girl.name || 'girl');
    return `compatibility-${boyName}-${girlName}.pdf`;
  }

  private slugify(value: string): string {
    const slug = value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    return slug || 'report';
  }

  private loadSavedCompatibility(id: string): void {
    this.loadingSavedCompatibility = true;
    this.loading = true;
    this.errorMessage = '';
    this.cdr.detectChanges();

    this.compatibilityApi.getCompatibilityAnalysis(id).subscribe({
      next: (saved) => {
        this.savedCompatibilityId = saved?.id ?? id;
        this.patchFormFromSavedCompatibility(saved);
        this.applyCompatibilityResult(saved?.result ?? null);
        this.loadingSavedCompatibility = false;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        const message = error?.message || 'Unable to open saved compatibility.';
        this.errorMessage = message;
        this.loadingSavedCompatibility = false;
        this.loading = false;
        this.notifications.error(message);
        this.cdr.detectChanges();
      }
    });
  }

  private patchFormFromSavedCompatibility(saved: SavedCompatibilityDetail): void {
    const personA = saved?.request?.personA;
    const personB = saved?.request?.personB;

    this.form.patchValue({
      boyName: personA?.name ?? saved?.personAName ?? '',
      boyBirthDate: this.toDateInput(personA?.birthDateTime ?? saved?.personADateOfBirth),
      boyBirthTime: this.toTimeInput(personA?.birthDateTime ?? saved?.personATimeOfBirth),
      boyBirthPlace: personA?.birthPlace ?? saved?.personABirthPlace ?? '',
      boyLatitude: Number(personA?.latitude ?? saved?.personALatitude ?? 0),
      boyLongitude: Number(personA?.longitude ?? saved?.personALongitude ?? 0),
      girlName: personB?.name ?? saved?.personBName ?? '',
      girlBirthDate: this.toDateInput(personB?.birthDateTime ?? saved?.personBDateOfBirth),
      girlBirthTime: this.toTimeInput(personB?.birthDateTime ?? saved?.personBTimeOfBirth),
      girlBirthPlace: personB?.birthPlace ?? saved?.personBBirthPlace ?? '',
      girlLatitude: Number(personB?.latitude ?? saved?.personBLatitude ?? 0),
      girlLongitude: Number(personB?.longitude ?? saved?.personBLongitude ?? 0),
    }, { emitEvent: false });

    this.boyLocationResults = [];
    this.girlLocationResults = [];
  }

  private applyCompatibilityResult(result: CompatibilityResponse | null): void {
    this.result = result;
    this.selectedCompatibilityAspect = null;
    this.isCompatibilityScoreModalOpen = false;
    this.selectedDosha = null;
    this.isDoshaModalOpen = false;
    this.showReport = !!result;
    if (result) {
      this.showBirthDetailsForm = false;
    }
    this.personAhouses = result?.personAchart?.houses || null;
    this.personBhouses = result?.personBchart?.houses || null;
    this.personAplanetPositions = result?.personAchart?.planetPositions || null;
    this.personBplanetPositions = result?.personBchart?.planetPositions || null;
    this.personAchart = result?.personAchart || null;
    this.personBchart = result?.personBchart || null;
  }

  private toDateInput(value: string | null | undefined): string {
    return String(value ?? '').slice(0, 10);
  }

  private toTimeInput(value: string | null | undefined): string {
    const text = String(value ?? '');
    if (text.includes('T')) {
      return text.slice(11, 16);
    }
    return text.slice(0, 5);
  }

  private buildCompatibilityRequest(): CompatibilityRequest {
    return {
      personA: this.toPersonRequest('boy'),
      personB: this.toPersonRequest('girl'),
    };
  }

  private toPersonRequest(prefix: 'boy' | 'girl'): MatchRequestPerson {
    const value = this.form.value;
    const birthDate = value[`${prefix}BirthDate`];
    const birthTime = value[`${prefix}BirthTime`];

    return {
      name: String(value[`${prefix}Name`] ?? ''),
      gender: prefix === 'boy' ? 'Male' : 'Female',
      birthDateTime: `${birthDate}T${birthTime}:00`,
      birthPlace: String(value[`${prefix}BirthPlace`] ?? ''),
      latitude: Number(value[`${prefix}Latitude`] ?? 0),
      longitude: Number(value[`${prefix}Longitude`] ?? 0),
      timeZoneOffset: 5.5,
    };
  }

  getMoonSign(chart: BirthChart | null): string {
    const moon = chart?.planetPositions?.find((item) => this.isMoonPlanet(item.planet));
    return this.formatSign(moon?.sign);
  }

  getNakshatra(chart: BirthChart | null): string {
    return chart?.panchang?.nakshatra || '—';
  }

  getAscendant(chart: BirthChart | null): string {
    return this.formatSign(chart?.ascendantSign ?? chart?.ascendantSignAndNumber);
  }

  getNakshatraDisplay(chart: BirthChart | null): string {
    const nakshatra = chart?.panchang?.nakshatra;
    const pada = chart?.panchang?.pada;
    const akshara = chart?.panchang?.akshara;

    if (!nakshatra) return '—';

    const padaText = pada ? ` ${this.t('compatibility.pada')} ${pada}` : '';
    const aksharaText = akshara ? ` (${akshara})` : '';
    return `${nakshatra}${padaText}${aksharaText}`;
  }

  translateCompatibilityTerm(value: string | number | null | undefined): string {
    if (value === null || value === undefined || value === '') {
      return '?';
    }

    const raw = String(value).trim();
    const key = this.compatibilityTermKeys[this.normalizeCompatibilityTerm(raw)];

    if (!key) {
      return raw;
    }

    const translated = this.transloco.translate(key);
    return translated === key ? raw : translated;
  }

  private readonly compatibilityTermKeys: Record<string, string> = {
    varna: 'compatibility.term.varna',
    vasya: 'compatibility.term.vasya',
    tara: 'compatibility.term.tara',
    yoni: 'compatibility.term.yoni',
    grahamaitri: 'compatibility.term.grahaMaitri',
    gana: 'compatibility.term.gana',
    bhakoot: 'compatibility.term.bhakoot',
    nadi: 'compatibility.term.nadi',
    excellent: 'compatibility.term.excellent',
    average: 'compatibility.term.average',
    challenging: 'compatibility.term.challenging',
    good: 'compatibility.term.good',
    strong: 'compatibility.term.strong',
    verystrong: 'compatibility.term.veryStrong',
    needswork: 'compatibility.term.needsWork',
    emotionalharmony: 'compatibility.term.emotionalHarmony',
    emotionalcompatibility: 'compatibility.emotionalCompatibility',
    communicationcompatibility: 'compatibility.term.communicationCompatibility',
    communication: 'compatibility.communication',
    marriagestability: 'compatibility.marriageStability',
    attractionromance: 'compatibility.term.attractionRomance',
    attractionandromance: 'compatibility.term.attractionRomance',
    physicalcompatibility: 'compatibility.physicalCompatibility',
    spiritualcompatibility: 'compatibility.term.spiritualCompatibility',
    spiritualalignment: 'compatibility.spiritualAlignment',
    growthtogether: 'compatibility.term.growthTogether',
    financialharmony: 'compatibility.financialHarmony',
    familyharmony: 'compatibility.familyHarmony',
    nodosha: 'compatibility.term.noDosha',
    mangaldosha: 'compatibility.term.mangalDosha',
    kujadosha: 'compatibility.term.mangalDosha',
    nadidosha: 'compatibility.term.nadiDosha',
    bhakootdosha: 'compatibility.term.bhakootDosha',
    ganadosha: 'compatibility.term.ganaDosha',
    grahamaitridosha: 'compatibility.term.grahaMaitriDosha',
    chandragrahamdosha: 'compatibility.term.chandraGrahanDosha',
    chandragrahandosha: 'compatibility.term.chandraGrahanDosha',
    chandragrahanadosha: 'compatibility.term.chandraGrahanDosha',
    present: 'compatibility.term.present',
    absent: 'compatibility.term.absent',
    low: 'compatibility.term.low',
    medium: 'compatibility.term.medium',
    high: 'compatibility.term.high',
    none: 'compatibility.none',
    both: 'compatibility.both',
    boy: 'compatibility.boy',
    girl: 'compatibility.girl'
  };

  private normalizeCompatibilityTerm(value: string): string {
    return value.replace(/&/g, 'and').replace(/[^a-z0-9]/gi, '').toLowerCase();
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) return '—';
    const [year, month, day] = value.split('-').map(Number);
    const locale = this.transloco.getActiveLang() === 'hi' ? 'hi-IN' : 'en-GB';
    return new Date(year, month - 1, day).toLocaleDateString(locale, {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  private formatSign(value: number | string | null | undefined): string {
    const signsByNumber: Record<number, string> = {
      1: 'sign.Aries',
      2: 'sign.Taurus',
      3: 'sign.Gemini',
      4: 'sign.Cancer',
      5: 'sign.Leo',
      6: 'sign.Virgo',
      7: 'sign.Libra',
      8: 'sign.Scorpio',
      9: 'sign.Sagittarius',
      10: 'sign.Capricorn',
      11: 'sign.Aquarius',
      12: 'sign.Pisces',
    };

    const signsByName: Record<string, string> = {
      aries: 'sign.Aries',
      taurus: 'sign.Taurus',
      gemini: 'sign.Gemini',
      cancer: 'sign.Cancer',
      leo: 'sign.Leo',
      virgo: 'sign.Virgo',
      libra: 'sign.Libra',
      scorpio: 'sign.Scorpio',
      sagittarius: 'sign.Sagittarius',
      capricorn: 'sign.Capricorn',
      aquarius: 'sign.Aquarius',
      pisces: 'sign.Pisces',
    };

    if (value === null || value === undefined || value === '') {
      return '—';
    }

    if (typeof value === 'number') {
      const key = signsByNumber[value];
      return key ? this.t(key) : '—';
    }

    const normalized = value.trim();
    const leadingNumber = normalized.match(/^\d+/)?.[0];
    const numberKey = leadingNumber ? signsByNumber[Number(leadingNumber)] : signsByNumber[Number(normalized)];

    if (numberKey) {
      return this.t(numberKey);
    }

    const signName = normalized
      .replace(/^\d+\s*-\s*/, '')
      .replace(/[^a-z]/gi, '')
      .toLowerCase();
    const nameKey = signsByName[signName];

    return nameKey ? this.t(nameKey) : normalized;
  }

  private isMoonPlanet(value: number | string | null | undefined): boolean {
    if (typeof value === 'number') {
      return value === 1;
    }

    return String(value ?? '').trim().toLowerCase() === 'moon';
  }

  private getGrade(score: number): string {
    if (score >= 82) return this.t('compatibility.excellent');
    if (score >= 75) return this.t('compatibility.veryStrong');
    if (score >= 65) return this.t('compatibility.strong');
    if (score >= 50) return this.t('compatibility.good');
    return this.t('compatibility.needsWork');
  }

  private getDoshaOwnership(dosha: CompatibilityDoshaItem): string {
    const boyHas = dosha.personA?.isPresent;
    const girlHas = dosha.personB?.isPresent;

    if (boyHas && girlHas) return this.t('compatibility.both');
    if (boyHas) return this.t('compatibility.boy');
    if (girlHas) return this.t('compatibility.girl');
    return this.t('compatibility.none');
  }

  private isDoshaDetected(dosha: CompatibilityDoshaItem): boolean {
    return Boolean(dosha.personA?.isPresent || dosha.personB?.isPresent);
  }

  private t(key: string, params?: Record<string, string | number>): string {
    return this.transloco.translate(key, params);
  }
}


