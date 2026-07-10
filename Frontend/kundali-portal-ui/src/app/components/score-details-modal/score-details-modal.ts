import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';

interface ScoreBreakdownItem {
  label: string;
  score: number;
  weight: number;
  contribution: number;
}

@Component({
  selector: 'app-score-details-modal',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './score-details-modal.html',
  styleUrls: ['./score-details-modal.scss']
})
export class ScoreDetailsModalComponent {
  @Input() isOpen = false;
  @Input() scoreType: 'overall' | 'stability' | 'growth' | 'challenge' = 'overall';
  @Input() scoreLabel = '';
  @Input() scoreValue: number = 0;
  @Input() scoreBreakdown: ScoreBreakdownItem[] = [];
  @Input() factors: string[] = [];
  @Output() close = new EventEmitter<void>();

  getScoreLabel(): string {
    if (this.scoreLabel) {
      return this.scoreLabel;
    }

    const labels: Record<string, string> = {
      'overall': 'kundali.overallScore',
      'stability': 'kundali.stabilityScore',
      'growth': 'kundali.growthScore',
      'challenge': 'kundali.challengeScore'
    };
    return labels[this.scoreType] || this.scoreType;
  }

  isTranslatedScoreLabel(): boolean {
    return !this.scoreLabel;
  }

  getScoreColor(score: number): string {
    if (score >= 70) return '#16a34a';
    if (score >= 50) return '#f59e0b';
    return '#dc2626';
  }

  hexToRgb(hex: string): string {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex);
    if (result) {
      return `${parseInt(result[1], 16)}, ${parseInt(result[2], 16)}, ${parseInt(result[3], 16)}`;
    }
    return '109, 40, 217';
  }

  getScorePercentage(item: ScoreBreakdownItem): string {
    return (item.weight * 100).toFixed(1);
  }

  getContributionPercentage(item: ScoreBreakdownItem, total: number): string {
    return ((item.contribution / total) * 100).toFixed(1);
  }

  getTotalContribution(): number {
    return this.scoreBreakdown.reduce((sum, item) => sum + item.contribution, 0);
  }

  closeModal(): void {
    this.close.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.closeModal();
    }
  }
}
