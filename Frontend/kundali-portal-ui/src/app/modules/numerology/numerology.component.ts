import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoService } from '@jsverse/transloco';
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
  imports: [CommonModule, FormsModule, AstrologyIconComponent, PortalWheelPickerComponent],
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

  simpleReading(report: NumerologyResponse): Array<{ title: string; body: string }> {
    const items: Array<{ title: string; body: string }> = [
      {
        title: 'Who you are day to day',
        body: `Your everyday number is ${report.mulank} (${report.mulankPlanet}). ${report.mulankDetails?.summary || 'This shapes how you act in daily choices.'}`
      },
      {
        title: 'Your longer path',
        body: `Your life-path number is ${report.bhagyank} (${report.bhagyankPlanet}). ${report.bhagyankDetails?.summary || 'This points to the longer direction of your life.'}`
      },
      {
        title: 'How they work together',
        body: `${this.combinationLean(report.combinationType)} ${report.combinationSummary}`.trim()
      },
      {
        title: 'Strengths to use',
        body: `Lean on ${this.joinList(report.combinationStrengths)}.`
      },
      {
        title: 'Habits that need care',
        body: `Watch for ${this.joinList(report.combinationChallenges)}.`
      }
    ];

    if (report.loShu) {
      items.push({
        title: 'What your date grid shows',
        body: `${this.gridAnswer(report.loShu)} Missing numbers are qualities to build with care. Repeated numbers show up strongly.`
      });
    }

    if (report.personalYear) {
      const year = report.personalYear;
      const next = report.nextPersonalYear;
      items.push({
        title: 'This year and next',
        body: [
          `${year.year} is a ${year.number} year (${year.planet}) — ${this.yearTone(year.number)}.`,
          next ? `${next.year} is a ${next.number} year (${next.planet}) — ${this.yearTone(next.number)}.` : ''
        ].filter(Boolean).join(' ')
      });
    }

    const shared = this.sharedInfluences(report);
    if (shared && (shared.days.length || shared.colours.length || shared.numbers.length)) {
      const parts = [
        shared.days.length ? `days like ${this.joinList(shared.days)}` : '',
        shared.colours.length ? `colours like ${this.joinList(shared.colours)}` : '',
        shared.numbers.length ? `numbers like ${this.joinList(shared.numbers)}` : ''
      ].filter(Boolean);
      items.push({
        title: 'Easy supports',
        body: `Lean on ${parts.join(', ')} when both birth numbers agree.`
      });
    }

    if (report.nameNumber) {
      items.push({
        title: 'Your name',
        body: this.simpleCheckLine(
          'name',
          report.nameMulankMatch,
          report.nameBhagyankMatch,
          null
        )
      });
    }

    if (report.mobile) {
      items.push({
        title: 'Your mobile',
        body: this.simpleCheckLine(
          'mobile number',
          report.mobile.mulankMatch,
          report.mobile.bhagyankMatch,
          report.mobile.suggestions?.[0] ?? null
        )
      });
    }

    if (report.business) {
      items.push({
        title: 'Your business name',
        body: this.simpleCheckLine(
          'business name',
          report.business.mulankMatch,
          report.business.bhagyankMatch,
          report.business.suggestions?.[0] ?? null
        )
      });
    }

    return items;
  }

  combinationPlainLabel(type: string): string {
    if (type === 'Reinforcing') {
      return 'numbers support each other';
    }

    if (type === 'Complementary') {
      return 'they pull in different ways and can balance';
    }

    if (type === 'Mixed') {
      return 'mixed strengths and friction';
    }

    return type || 'pair reading';
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