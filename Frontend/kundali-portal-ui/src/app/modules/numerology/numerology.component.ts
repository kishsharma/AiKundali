import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';
import { AstrologerBrandingSetting, AstrologerBrandingSettingsService } from '../../core/services/astrologer-branding-settings.service';
import { ThemeService } from '../../core/services/theme.service';
import { AstrologyIconComponent } from '../../shared/components/astrology-icon.component';
import { PortalWheelPickerComponent } from '../../shared/components/portal-wheel-picker.component';
import { NotificationService } from '../../shared/services/notification.service';
import { NumerologyLoShu, NumerologyResponse, NumerologyService } from './numerology.service';

@Component({
  selector: 'app-numerology',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoPipe, AstrologyIconComponent, PortalWheelPickerComponent],
  templateUrl: './numerology.component.html',
  styleUrl: './numerology.component.scss',
})
export class NumerologyComponent implements OnInit, OnDestroy {
  private readonly numerologyService = inject(NumerologyService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly brandingSettingsService = inject(AstrologerBrandingSettingsService);
  private readonly themeService = inject(ThemeService);
  private readonly transloco = inject(TranslocoService);
  private readonly notifications = inject(NotificationService);
  private brandingSettings: AstrologerBrandingSetting | null = null;
  /** Flip to true when prepared-by branding should show again. */
  readonly showReportBrandingHeader = false;
  isSaving = false;
  isDownloading = false;
  private readonly colourMap: Record<string, string> = {
    blue: '#4776b4', brown: '#8b6248', cream: '#f1e5c8', gold: '#c79a35',
    'golden yellow': '#e0ad32', green: '#4f8b62', grey: '#8a9099',
    'light blue': '#91b8d6', 'light green': '#92bd92', orange: '#df8439',
    pink: '#d883a2', red: '#c95b5b', violet: '#8065a8', white: '#f7f5ef',
    yellow: '#dfbc45',
  };
  private readonly personalYearTone: Record<number, string> = {
    1: 'a year to start things and lead',
    2: 'a quieter year for patience and partnerships',
    3: 'a year for expression, learning, and growth',
    4: 'a year to build steadily and stay practical',
    5: 'a year of change, movement, and variety',
    6: 'a year focused on home, care, and responsibility',
    7: 'a quieter, reflective year',
    8: 'a year for work, structure, and results',
    9: 'a year to finish cycles and give back'
  };

  dateOfBirth = '';
  fullName = '';
  mobileNumber = '';
  businessName = '';
  readonly currentYear = new Date().getFullYear();
  readonly maxDate = new Date().toISOString().slice(0, 10);
  readonly loShuRows = [
    { key: 'mental', numbers: [4, 9, 2] },
    { key: 'emotional', numbers: [3, 5, 7] },
    { key: 'practical', numbers: [8, 1, 6] },
  ] as const;
  result: NumerologyResponse | null = null;
  revealResult: NumerologyResponse | null = null;
  revealStage: 'idle' | 'dob' | 'digits' | 'shuffle' | 'resolved' = 'idle';
  errorMessage = '';
  isLoading = false;
  private revealTimers: number[] = [];
  private readonly storageKey = 'kundali_numerology_reading';

  get formattedDateOfBirth(): string {
    const [year, month, day] = this.dateOfBirth.split('-');
    return day && month && year ? `${day} · ${month} · ${year}` : this.dateOfBirth;
  }

  get birthDigits(): string[] {
    const [year, month, day] = this.dateOfBirth.split('-');
    return `${day ?? ''}${month ?? ''}${year ?? ''}`.replace(/\D/g, '').split('');
  }

  get activeReportBranding(): AstrologerBrandingSetting | null {
    const settings = this.brandingSettings;
    return settings?.isActive && settings.showOnPdfReport ? settings : null;
  }

  get reportBrandLogoSrc(): string {
    return this.themeService.mode() === 'dark'
      ? 'assets/brand/horalife/horalife-icon-white.svg'
      : 'assets/brand/horalife/horalife-icon-color.svg';
  }

  get reportAstrologerName(): string {
    return this.activeReportBranding?.astrologerName
      || this.transloco.translate('kundali.reportAuthorName');
  }

  get reportAstrologerDesignation(): string {
    return this.activeReportBranding
      ? this.activeReportBranding.designation || ''
      : this.transloco.translate('kundali.reportAuthorRole');
  }

  get reportAstrologerContact(): string {
    const branding = this.activeReportBranding;
    if (!branding) {
      return this.transloco.translate('kundali.reportAuthorContact');
    }

    return [branding.contactNumber, branding.email, branding.address].filter(Boolean).join(' | ');
  }

  get reportSubjectLabel(): string {
    const name = this.fullName.trim() || this.result?.name?.trim();
    if (name) {
      return name;
    }

    return this.formattedDateOfBirth || 'this reading';
  }

  ngOnInit(): void {
    this.brandingSettingsService.getMine().subscribe({
      next: settings => {
        this.brandingSettings = settings;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.brandingSettings = null;
        this.changeDetector.markForCheck();
      }
    });

    const saved = this.readSavedReading();
    if (!saved) {
      return;
    }

    this.dateOfBirth = saved.dateOfBirth;
    this.fullName = saved.fullName;
    this.mobileNumber = saved.mobileNumber;
    this.businessName = saved.businessName;
    this.result = saved.result;
  }

  sharedInfluences(report: NumerologyResponse): { numbers: number[]; days: string[]; colours: string[] } | null {
    const mulank = report.mulankDetails;
    const bhagyank = report.bhagyankDetails;
    if (!mulank || !bhagyank) {
      return null;
    }

    const numbers = this.shared(mulank.favourableNumbers ?? [], bhagyank.favourableNumbers ?? []);
    const days = this.shared(mulank.favourableDays ?? [], bhagyank.favourableDays ?? []);
    const colours = this.shared(mulank.favourableColours ?? [], bhagyank.favourableColours ?? []);
    return numbers.length || days.length || colours.length ? { numbers, days, colours } : null;
  }

  saveReading(): void {
    if (!this.result || this.isSaving) {
      return;
    }

    this.isSaving = true;
    try {
      this.writeSavedReading(this.result);
      this.notifications.success('Numerology reading saved on this device.');
    } catch {
      this.notifications.error('Unable to save the reading right now.');
    } finally {
      this.isSaving = false;
      this.changeDetector.detectChanges();
    }
  }

  async downloadReading(): Promise<void> {
    if (!this.result || this.isDownloading) {
      return;
    }

    const report = document.getElementById('numerology-report');
    if (!report) {
      this.notifications.error('Generate a reading before downloading.');
      return;
    }

    this.isDownloading = true;
    this.changeDetector.detectChanges();
    document.body.classList.add('numerology-print-mode');

    try {
      await new Promise(resolve => setTimeout(resolve, 120));
      const canvas = await html2canvas(report, {
        scale: 2,
        backgroundColor: '#ffffff',
        useCORS: true,
        logging: false,
        ignoreElements: element => element.classList.contains('results-actions')
      });

      const pdf = new jsPDF({ orientation: 'p', unit: 'mm', format: 'a4', compress: true });
      const margin = 10;
      const pageWidth = pdf.internal.pageSize.getWidth();
      const pageHeight = pdf.internal.pageSize.getHeight();
      const usableWidth = pageWidth - margin * 2;
      const usableHeight = pageHeight - margin * 2;
      const renderedHeight = (canvas.height * usableWidth) / canvas.width;

      if (renderedHeight <= usableHeight) {
        pdf.addImage(canvas.toDataURL('image/png'), 'PNG', margin, margin, usableWidth, renderedHeight);
      } else {
        let offsetY = 0;
        let pageIndex = 0;
        while (offsetY < canvas.height) {
          if (pageIndex > 0) {
            pdf.addPage();
          }

          const sliceHeight = Math.min(
            canvas.height - offsetY,
            Math.ceil((usableHeight * canvas.width) / usableWidth)
          );
          const slice = document.createElement('canvas');
          slice.width = canvas.width;
          slice.height = sliceHeight;
          const context = slice.getContext('2d');
          if (!context) {
            break;
          }

          context.fillStyle = '#ffffff';
          context.fillRect(0, 0, slice.width, slice.height);
          context.drawImage(canvas, 0, offsetY, canvas.width, sliceHeight, 0, 0, canvas.width, sliceHeight);
          const sliceHeightMm = (sliceHeight * usableWidth) / canvas.width;
          pdf.addImage(slice.toDataURL('image/png'), 'PNG', margin, margin, usableWidth, sliceHeightMm);
          offsetY += sliceHeight;
          pageIndex += 1;
        }
      }

      pdf.save(this.getDownloadFileName());
      this.notifications.success('Numerology PDF downloaded.');
    } catch (error) {
      console.error('Numerology PDF export failed:', error);
      this.notifications.error('Unable to download the reading right now.');
    } finally {
      document.body.classList.remove('numerology-print-mode');
      this.isDownloading = false;
      this.changeDetector.detectChanges();
    }
  }

  printReading(): void {
    const onAfterPrint = () => {
      document.body.classList.remove('numerology-print-mode');
      window.removeEventListener('afterprint', onAfterPrint);
    };

    document.body.classList.add('numerology-print-mode');
    window.addEventListener('afterprint', onAfterPrint);
    window.print();
  }

  private getDownloadFileName(): string {
    const label = (this.fullName.trim() || this.dateOfBirth || 'reading')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-|-$/g, '');
    return `horalife-numerology-${label || 'reading'}.pdf`;
  }

  calculate(): void {
    if (!this.dateOfBirth || this.dateOfBirth > this.maxDate || this.isLoading) {
      return;
    }

    this.clearRevealTimers();
    this.errorMessage = '';
    this.result = null;
    this.revealResult = null;
    this.isLoading = true;
    this.revealStage = 'dob';
    const revealStartedAt = Date.now();

    this.scheduleReveal(() => this.revealStage = 'digits', 220);
    this.scheduleReveal(() => this.revealStage = 'shuffle', 520);

    this.numerologyService.calculate(this.dateOfBirth, this.fullName, this.mobileNumber, this.businessName).subscribe({
      next: result => {
        const resolveDelay = Math.max(0, 800 - (Date.now() - revealStartedAt));
        this.scheduleReveal(() => {
          this.revealResult = result;
          this.revealStage = 'resolved';
          this.scheduleReveal(() => {
            this.result = result;
            this.isLoading = false;
            this.revealStage = 'idle';
            this.writeSavedReading(result);
          }, 340);
        }, resolveDelay);
      },
      error: error => {
        this.clearRevealTimers();
        this.isLoading = false;
        this.revealStage = 'idle';
        this.errorMessage = error?.error?.message
          || (typeof error?.error === 'string' ? error.error : '')
          || 'Unable to calculate numerology right now. Please try again.';
        this.changeDetector.detectChanges();
      },
    });
  }

  ngOnDestroy(): void {
    this.clearRevealTimers();
  }

  colourSwatch(colour: string): string | null {
    return this.colourMap[colour.trim().toLowerCase()] ?? null;
  }

  repeatedLabel(grid: NumerologyLoShu): string {
    if (!grid.repeated?.length) {
      return 'None';
    }

    return grid.repeated.map(item => `${item.number} × ${item.count}`).join(', ');
  }

  getLoShuPlanet(number: number): string {
    const planets: Record<number, string> = {
      1: 'Sun', 2: 'Moon', 3: 'Jupiter', 4: 'Rahu', 5: 'Mercury',
      6: 'Venus', 7: 'Ketu', 8: 'Saturn', 9: 'Mars',
    };
    return planets[number] ?? '';
  }

  getLoShuRowCount(grid: NumerologyLoShu, numbers: readonly number[]): number {
    return numbers.reduce((count, number) => {
      const cell = grid.cells.find((item) => item.number === number);
      return count + (cell && cell.count > 0 ? 1 : 0);
    }, 0);
  }

  simpleReading(report: NumerologyResponse): Array<{ title: string; body: string }> {
    const translate = (key: string, params: Record<string, string | number> = {}) =>
      this.transloco.translate(`numerology.answers.${key}`, params);
    const planet = (name: string) => this.transloco.translate(`planet.${name}`);
    const items: Array<{ title: string; body: string }> = [
      {
        title: translate('day.title'),
        body: translate('day.body', { number: report.mulank, planet: planet(report.mulankPlanet) })
      },
      {
        title: translate('path.title'),
        body: translate('path.body', { number: report.bhagyank, planet: planet(report.bhagyankPlanet) })
      },
      {
        title: translate('pair.title'),
        body: translate(`pair.${this.combinationKey(report.combinationType)}`)
      },
      {
        title: translate('strengths.title'),
        body: translate('strengths.body')
      },
      {
        title: translate('challenges.title'),
        body: translate('challenges.body')
      }
    ];

    if (report.loShu) {
      items.push({
        title: translate('grid.title'),
        body: this.localizedGridAnswer(report.loShu, translate)
      });
    }

    if (report.personalYear) {
      const year = report.personalYear;
      const next = report.nextPersonalYear;
      items.push({
        title: translate('year.title'),
        body: translate(next ? 'year.bodyWithNext' : 'year.body', {
          year: year.year,
          number: year.number,
          planet: planet(year.planet),
          tone: translate(`year.tone.${year.number}`),
          nextYear: next?.year ?? '',
          nextNumber: next?.number ?? '',
          nextPlanet: next ? planet(next.planet) : '',
          nextTone: next ? translate(`year.tone.${next.number}`) : ''
        })
      });
    }

    const shared = this.sharedInfluences(report);
    if (shared && (shared.days.length || shared.colours.length || shared.numbers.length)) {
      items.push({
        title: translate('supports.title'),
        body: translate('supports.body')
      });
    }

    if (report.nameNumber) {
      items.push({
        title: translate('name.title'),
        body: this.localizedCheckLine(translate, 'name', report.nameMulankMatch, report.nameBhagyankMatch)
      });
    }

    if (report.mobile) {
      items.push({
        title: translate('mobile.title'),
        body: this.localizedCheckLine(translate, 'mobile', report.mobile.mulankMatch, report.mobile.bhagyankMatch)
      });
    }

    if (report.business) {
      items.push({
        title: translate('business.title'),
        body: this.localizedCheckLine(translate, 'business', report.business.mulankMatch, report.business.bhagyankMatch)
      });
    }

    return items;
  }

  combinationPlainLabel(type: string): string {
    return this.transloco.translate(`numerology.answers.pair.${this.combinationKey(type)}`);
  }

  combinationKey(type: string): 'reinforcing' | 'complementary' | 'mixed' | 'default' {
    const normalized = type.trim().toLowerCase();
    if (normalized === 'reinforcing') return 'reinforcing';
    if (normalized === 'complementary') return 'complementary';
    if (normalized === 'mixed') return 'mixed';
    return 'default';
  }

  private localizedGridAnswer(
    grid: NumerologyLoShu,
    translate: (key: string, params?: Record<string, string | number>) => string
  ): string {
    const missing = grid.missing?.length
      ? translate('grid.missing', { numbers: grid.missing.join(', ') })
      : translate('grid.noMissing');
    const repeated = grid.repeated?.length
      ? translate('grid.repeated', { numbers: grid.repeated.map(item => `${item.number} × ${item.count}`).join(', ') })
      : translate('grid.noRepeated');
    return `${missing} ${repeated} ${translate('grid.guidance')}`;
  }

  private localizedCheckLine(
    translate: (key: string, params?: Record<string, string | number>) => string,
    subjectKey: 'name' | 'mobile' | 'business',
    birthMatch: string | null | undefined,
    pathMatch: string | null | undefined
  ): string {
    const matchKey = (value: string | null | undefined) =>
      value?.toLowerCase() === 'supports' ? 'supports' : 'differs';
    return translate('check.body', {
      subject: translate(`${subjectKey}.subject`),
      birthMatch: translate(`check.${matchKey(birthMatch)}`),
      pathMatch: translate(`check.${matchKey(pathMatch)}`)
    });
  }

  yearTone(number: number): string {
    return this.personalYearTone[number] || 'a year shaped by its ruling planet';
  }

  private combinationLean(type: string): string {
    if (type === 'Reinforcing') {
      return 'These two numbers support each other.';
    }

    if (type === 'Complementary') {
      return 'These two numbers pull in different directions and can balance each other.';
    }

    if (type === 'Mixed') {
      return 'These two numbers bring mixed strengths and friction.';
    }

    return type ? `This pair reads as ${type}.` : '';
  }

  private gridAnswer(grid?: NumerologyLoShu | null): string {
    if (!grid) {
      return 'The date grid is not available for this reading.';
    }

    const missing = grid.missing?.length
      ? `Missing numbers: ${grid.missing.join(', ')}.`
      : 'No numbers from 1 to 9 are missing.';
    const repeated = grid.repeated?.length
      ? `Repeated numbers: ${grid.repeated.map(item => `${item.number} appears ${item.count} times`).join('; ')}.`
      : 'No number repeats.';
    return `${missing} ${repeated}`;
  }

  private simpleCheckLine(
    subject: string,
    mulankMatch: string | null | undefined,
    bhagyankMatch: string | null | undefined,
    tip: string | null
  ): string {
    const birth = mulankMatch === 'Supports' ? 'supports' : 'differs from';
    const path = bhagyankMatch === 'Supports' ? 'supports' : 'differs from';
    const base = `Your ${subject} ${birth} your birth number and ${path} your life path.`;
    return tip && (mulankMatch === 'Differs' || bhagyankMatch === 'Differs')
      ? `${base} Tip: ${tip}`
      : base;
  }

  private unique<T extends string | number>(values: T[]): T[] {
    return [...new Set(values)];
  }

  private shared<T extends string | number>(left: T[], right: T[]): T[] {
    const rightSet = new Set(right);
    return this.unique(left.filter(value => rightSet.has(value)));
  }

  private joinList(values: Array<string | number> | null | undefined): string {
    const items = (values ?? []).map(value => String(value)).filter(Boolean);
    if (items.length === 0) {
      return 'none listed';
    }

    if (items.length === 1) {
      return items[0];
    }

    return `${items.slice(0, -1).join(', ')} and ${items[items.length - 1]}`;
  }

  private readSavedReading(): {
    dateOfBirth: string;
    fullName: string;
    mobileNumber: string;
    businessName: string;
    result: NumerologyResponse;
  } | null {
    try {
      const raw = localStorage.getItem(this.storageKey);
      if (!raw) {
        return null;
      }

      const saved = JSON.parse(raw) as {
        dateOfBirth?: string;
        fullName?: string;
        mobileNumber?: string;
        businessName?: string;
        result?: NumerologyResponse;
      };
      if (!saved.dateOfBirth || !saved.result?.mulank) {
        return null;
      }

      return {
        dateOfBirth: saved.dateOfBirth,
        fullName: saved.fullName ?? '',
        mobileNumber: saved.mobileNumber ?? '',
        businessName: saved.businessName ?? '',
        result: saved.result
      };
    } catch {
      return null;
    }
  }

  private writeSavedReading(result: NumerologyResponse): void {
    localStorage.setItem(this.storageKey, JSON.stringify({
      dateOfBirth: this.dateOfBirth,
      fullName: this.fullName.trim(),
      mobileNumber: this.mobileNumber.trim(),
      businessName: this.businessName.trim(),
      result
    }));
  }

  private scheduleReveal(action: () => void, delay: number): void {
    const timer = window.setTimeout(() => {
      action();
      this.changeDetector.detectChanges();
    }, delay);
    this.revealTimers.push(timer);
  }

  private clearRevealTimers(): void {
    this.revealTimers.forEach(timer => window.clearTimeout(timer));
    this.revealTimers = [];
  }
}
