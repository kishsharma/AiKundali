import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DhruvaChatChart, DhruvaChatMessage, DhruvaNarrationService } from '../../core/services/dhruva-narration.service';

@Component({
  selector: 'app-dhruva-chats',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dhruva-chats.component.html',
  styleUrl: './dhruva-chats.component.scss',
})
export class DhruvaChatsComponent implements OnInit {
  private readonly dhruva = inject(DhruvaNarrationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly changeDetector = inject(ChangeDetectorRef);

  charts: DhruvaChatChart[] = [];
  messages: DhruvaChatMessage[] = [];
  selectedId = '';
  loadingCharts = true;
  loadingMessages = false;
  errorMessage = '';

  ngOnInit(): void {
    this.selectedId = this.route.snapshot.queryParamMap.get('chart') || '';
    this.dhruva.listChatCharts().subscribe({
      next: (charts) => {
        this.charts = charts ?? [];
        this.loadingCharts = false;
        if (!this.selectedId && this.charts.length === 1) {
          this.selectChart(this.charts[0]);
        } else if (this.selectedId) {
          this.loadMessages(this.selectedId);
        }
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.loadingCharts = false;
        this.errorMessage = 'Dhruva chats could not be loaded.';
        this.changeDetector.markForCheck();
      },
    });
  }

  selectChart(chart: DhruvaChatChart): void {
    if (this.selectedId === chart.kundaliId && this.messages.length) {
      return;
    }

    this.selectedId = chart.kundaliId;
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { chart: chart.kundaliId },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.loadMessages(chart.kundaliId);
  }

  selectedChart(): DhruvaChatChart | null {
    return this.charts.find((chart) => chart.kundaliId === this.selectedId) ?? null;
  }

  chartMeta(chart: DhruvaChatChart): string {
    return [this.formatDate(chart.dateOfBirth), chart.birthPlace].filter(Boolean).join(' · ');
  }

  formatTime(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    });
  }

  private loadMessages(kundaliId: string): void {
    this.loadingMessages = true;
    this.errorMessage = '';
    this.dhruva.listChatMessages(kundaliId).subscribe({
      next: (messages) => {
        this.messages = messages ?? [];
        this.loadingMessages = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.messages = [];
        this.loadingMessages = false;
        this.errorMessage = 'This conversation could not be loaded.';
        this.changeDetector.markForCheck();
      },
    });
  }

  private formatDate(value: string): string {
    const date = new Date(`${value}T00:00:00`);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
