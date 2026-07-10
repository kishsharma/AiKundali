import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KundaliService } from './kundali.service';
import { PageHeaderComponent } from "../../shared/components/page-header.component";
import { SectionCardComponent } from "../../shared/components/section-card.component";
import { of } from 'rxjs';
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
    TransitInsightComponent
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
  private brandingSettingsService = inject(AstrologerBrandingSettingsService);
  locationResults: any[] = [];
  kundaliResponse: any = null;
  brandingSettings: AstrologerBrandingSetting | null = null;
  houseScores: any[] = [];
  loading = false;
  showChart = false;
  mahadashaPeriods: any[] = [];
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

  private translatePlanetName(planet: string): string {
    const key = `planet.${planet}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : planet;
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
    const payload = {
      name: this.kundaliForm.value.fullName,
      gender: this.kundaliForm.value.gender,
      birthDateTime: `${this.kundaliForm.value.birthDate}T${this.kundaliForm.value.birthTime}:00`,
      birthplace: this.kundaliForm.value.birthPlace,
      latitude: this.kundaliForm.value.latitude,
      longitude: this.kundaliForm.value.longitude,
      timeZoneOffset: 5.30
    };
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
          this.setDefaultHouseSelection();
          this.cdr.detectChanges();
          console.log('houseScoreslog'  , this.houseScores);
          console.log('houseScoreslog2'  ,  response.houses);
          console.log(this.planets);

          this.dashaApiService.getMahadashaPeriods(payload).subscribe({
            next: (mahaResponse) => {
              console.log('Mahadasha periods:', mahaResponse);
              this.mahadashaPeriods = mahaResponse;
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

}
