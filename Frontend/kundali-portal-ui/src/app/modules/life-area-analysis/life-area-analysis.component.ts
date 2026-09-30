import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { KundaliService } from '../kundali/kundali.service';
import {
  LifeAreaAnalysisService,
  LifeAreaCatalogItem,
  LifeAreaReport
} from './life-area-analysis.service';

interface SavedChartOption {
  id: string;
  fullName: string;
  displayName?: string;
}

@Component({
  selector: 'app-life-area-analysis',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, TranslocoPipe, PageHeaderComponent],
  templateUrl: './life-area-analysis.component.html',
  styleUrl: './life-area-analysis.component.scss'
})
export class LifeAreaAnalysisComponent implements OnInit {
  private readonly analysisApi = inject(LifeAreaAnalysisService);
  private readonly kundaliApi = inject(KundaliService);
  private readonly transloco = inject(TranslocoService);

  charts: SavedChartOption[] = [];
  areas: LifeAreaCatalogItem[] = [];
  selectedChartId = '';
  selectedArea = '';
  report: LifeAreaReport | null = null;
  insight = '';
  insightState: 'idle' | 'loading' | 'ready' | 'locked' | 'unavailable' = 'idle';
  loadingCharts = true;
  loadingAreas = true;
  loadingReport = false;
  errorMessage = '';
  private insightRequest = 0;

  ngOnInit(): void {
    const locale = this.transloco.getActiveLang();
    this.kundaliApi.getSavedKundalis().subscribe({
      next: (charts: SavedChartOption[]) => {
        this.charts = (Array.isArray(charts) ? charts : []).map(chart => ({
          ...chart,
          id: String((chart as SavedChartOption & { Id?: string }).id ?? (chart as SavedChartOption & { Id?: string }).Id ?? '')
        })).filter(chart => chart.id);
        this.selectedChartId = this.charts[0]?.id ?? '';
        this.loadingCharts = false;
      },
      error: () => {
        this.loadingCharts = false;
        this.errorMessage = this.transloco.translate('lifeArea.loadFailed');
      }
    });
    this.analysisApi.getAreas(locale).subscribe({
      next: (areas) => {
        this.areas = (Array.isArray(areas) ? areas : [])
          .map(area => this.normalizeArea(area))
          .filter(area => area.key && area.key !== 'vehicles');
        this.loadingAreas = false;
      },
      error: () => {
        this.loadingAreas = false;
        this.errorMessage = this.transloco.translate('lifeArea.loadFailed');
      }
    });
  }

  selectArea(key: string): void {
    if (!this.selectedChartId || !key) {
      return;
    }

    const request = ++this.insightRequest;
    this.selectedArea = key;
    this.report = null;
    this.insight = '';
    this.insightState = 'idle';
    this.errorMessage = '';
    this.loadingReport = true;
    this.analysisApi.getReport(this.selectedChartId, key, this.transloco.getActiveLang()).subscribe({
      next: (report) => {
        if (request !== this.insightRequest) {
          return;
        }
        this.report = report;
        this.loadingReport = false;
      },
      error: () => {
        if (request !== this.insightRequest) {
          return;
        }
        this.loadingReport = false;
        this.errorMessage = this.transloco.translate('lifeArea.reportFailed');
      }
    });
  }

  onChartChange(): void {
    this.insightRequest++;
    this.loadingReport = false;
    this.report = null;
    this.insight = '';
    this.insightState = 'idle';
    this.selectedArea = '';
    this.errorMessage = '';
  }

  writeInsight(): void {
    if (!this.selectedChartId || !this.selectedArea || !this.report || this.insightState === 'loading') {
      return;
    }

    this.loadInsight(this.insightRequest, this.selectedArea);
  }

  private normalizeArea(area: LifeAreaCatalogItem & {
    Key?: string;
    Name?: string;
    PrimaryCharts?: string[];
    SupportingCharts?: string[];
    RequiredHouses?: number[];
    RequiredPlanets?: string[];
  }): LifeAreaCatalogItem {
    return {
      key: area.key ?? area.Key ?? '',
      name: area.name ?? area.Name ?? '',
      primaryCharts: area.primaryCharts ?? area.PrimaryCharts ?? [],
      supportingCharts: area.supportingCharts ?? area.SupportingCharts ?? [],
      requiredHouses: area.requiredHouses ?? area.RequiredHouses ?? [],
      requiredPlanets: area.requiredPlanets ?? area.RequiredPlanets ?? []
    };
  }

  private loadInsight(request: number, area: string): void {
    this.insightState = 'loading';
    this.analysisApi.getInsight(this.selectedChartId, area, this.transloco.getActiveLang()).subscribe({
      next: (response) => {
        if (request !== this.insightRequest) {
          return;
        }
        this.insight = response.insight ?? '';
        this.insightState = this.insight ? 'ready' : 'unavailable';
      },
      error: (error: { status?: number }) => {
        if (request !== this.insightRequest) {
          return;
        }
        this.insight = '';
        this.insightState = error?.status === 403 ? 'locked' : 'unavailable';
      }
    });
  }

  chartLabel(chart: SavedChartOption): string {
    return chart.displayName || chart.fullName;
  }

  areaMark(key: string): string {
    return this.areaMarks[key] ?? this.areaMarks['personality'];
  }

  private readonly areaMarks: Record<string, string> = {
    personality: 'M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4zm-7 8a7 7 0 0 1 14 0',
    health: 'M12 20s-7-4.4-7-9a4 4 0 0 1 7-2 4 4 0 0 1 7 2c0 4.6-7 9-7 9z',
    wealth: 'M12 3v18M8 7.5c0-1.4 1.8-2.5 4-2.5s4 1.1 4 2.5-1.8 2.5-4 2.5-4 1.1-4 2.5 1.8 2.5 4 2.5 4-1.1 4-2.5',
    career: 'M9 7V5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2v2M4 7h16v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2z',
    business: 'M4 10h16v9H4zM8 10V7a4 4 0 0 1 8 0v3',
    marriage: 'M8.5 12.5a3.2 3.2 0 1 1 0-5 3.2 3.2 0 0 1 0 5zm7 0a3.2 3.2 0 1 0 0-5 3.2 3.2 0 0 0 0 5zM8.5 13.2c-2.6.5-4.5 2.2-4.5 4.3h7m1 0h8c0-2.1-1.9-3.8-4.5-4.3',
    children: 'M8 11a3 3 0 1 0-3-3 3 3 0 0 0 3 3zm8 1a2.5 2.5 0 1 0-2.5-2.5A2.5 2.5 0 0 0 16 12zM3 19a5 5 0 0 1 10 0m2 0a4 4 0 0 1 6.5-3.1',
    property: 'M4 11 12 4l8 7v8a1 1 0 0 1-1 1h-5v-5H10v5H5a1 1 0 0 1-1-1z',
    education: 'M3 9 12 4l9 5-9 5zm3 3.2V17l6 3 6-3v-4.8',
    parents: 'M7 11a3 3 0 1 0-3-3 3 3 0 0 0 3 3zm10 0a3 3 0 1 0-3-3 3 3 0 0 0 3 3zM2 19a5 5 0 0 1 10 0m0 0a5 5 0 0 1 10 0',
    vehicles: 'M4 15h16l-1.5-5H6zm1 0v2m14-2v2M7 15v-2m10 2v-2',
    spirituality: 'M12 3v3m0 12v3M3 12h3m12 0h3M6 6l2 2m8 8 2 2m0-12-2 2M8 16l-2 2M12 8a4 4 0 1 0 4 4 4 4 0 0 0-4-4z',
    strengthAndResilience: 'M12 3 5 6v5c0 4.2 2.8 7.4 7 9 4.2-1.6 7-4.8 7-9V6z',
    adversity: 'M4 17h16M8 17a4 4 0 0 1 8 0M12 7v2m5 1-1 1M7 10l1 1',
    ancestry: 'M12 21V10m0 0c0-3 2-5 5-5m-5 5c0-3-2-5-5-5M8 21h8'
  };
}
