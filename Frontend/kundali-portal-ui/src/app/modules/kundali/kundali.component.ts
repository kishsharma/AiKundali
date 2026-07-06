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
    HouseDetailsCard,
    KundaliSummary,
    PanchangCardComponent,
    KundaliScoreCard,
    MahadashaAntardasha,
    TranslocoPipe,
    HouseScoreOverview
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
  locationResults: any[] = [];
  kundaliResponse: any = null;
  houseScores: any[] = [];
  loading = false;
  showChart = false;
  mahadashaPeriods: any[] = [];
  planets: any[] = [];
  summary: any;
  scores: any[] = [];
  yogas: any[] = [];
  selectedHouseData: any;
  activeChartType: 'north' | 'south' | 'western' = 'north';
  today: Date = new Date();
exportingPdf = false;
  ngOnInit(): void {
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

  generateKundali(): void {
    this.showChart = false;
    this.selectedHouseData = null;
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
          this.summary = response.summary;
          this.scores = response.scores;
          this.yogas = this.extractYogas(response);
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
    const availableHouses = this.kundaliResponse?.houses ?? this.houseScores ?? [];
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
            description: item.description
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
      height: element.scrollHeight
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

      const yogaSection = document.querySelector('#kundali-report > app-section-card.full-row') as HTMLElement | null;
      if (yogaSection) {
        elementsToRender.push(yogaSection);
      }

      const planetPositionsSection = document.querySelector('#kundali-report > .results-sections > app-section-card.full-row') as HTMLElement | null;
      if (planetPositionsSection) {
        elementsToRender.push(planetPositionsSection);
      }
      const kundaliscore = document.querySelector('#kundali-report > .results-sections > .kundaliscore-grid') as HTMLElement | null;
      if (kundaliscore) {
        elementsToRender.push(kundaliscore);
      }

      const scoreSection = document.querySelector('#kundali-report > .results-sections > .detail-grid') as HTMLElement | null;
      if (scoreSection) {
        elementsToRender.push(scoreSection);
      }

    

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
      document.body.classList.remove('pdf-mode');
      this.cdr.detectChanges();
      setTimeout(() => {
        this.exportingPdf = false;
        this.cdr.detectChanges();
      }, 150);
    }
  }

}
