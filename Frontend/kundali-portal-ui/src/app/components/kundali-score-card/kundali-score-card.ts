import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { ScoreDetailsModalComponent } from '../score-details-modal/score-details-modal';

type ScoreType = 'overall' | 'stability' | 'growth' | 'challenge';

@Component({
  selector: 'app-kundali-score-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, ScoreDetailsModalComponent],
  templateUrl: './kundali-score-card.html',
  styleUrls: ['../../styles/shared/_cards.scss', './kundali-score-card.scss'],
})
export class KundaliScoreCard {
  @Input() scores: any = null;

  selectedScoreType: ScoreType | null = null;
  isModalOpen = false;

  get scoreEntries(): Array<{ name: string; value: string | number; type: ScoreType }> {
    if (!this.scores) {
      return [];
    }

    if (Array.isArray(this.scores)) {
      return this.scores.map((score: any) => ({
        name: score.name ?? 'Score',
        value: score.value ?? '',
        type: 'overall' as ScoreType
      }));
    }

    return Object.entries(this.scores)
      .filter(([key, value]) => !Array.isArray(value) && ['overall', 'stability', 'growth', 'challenge'].includes(key))
      .map(([key, value]) => ({
        name: this.formatLabel(key),
        value: value as string | number,
        type: key as ScoreType
      }));
  }

  private formatLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase());
  }

  openScoreDetails(scoreType: ScoreType): void {
    this.selectedScoreType = scoreType;
    this.isModalOpen = true;
  }

  closeScoreDetails(): void {
    this.isModalOpen = false;
    setTimeout(() => {
      this.selectedScoreType = null;
    }, 300);
  }

  getScoreBreakdown(scoreType: ScoreType): any[] {
    return this.scores?.scoreBreakdown?.[scoreType] || [];
  }

  getFactors(scoreType: ScoreType): string[] {
    const factorKey = `${scoreType}Factors`;
    return this.scores?.[factorKey] || [];
  }

  getScoreValue(scoreType: ScoreType): number {
    return this.scores?.[scoreType] || 0;
  }
}
