import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-kundali-score-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './kundali-score-card.html',
  styleUrls: ['../../styles/shared/_cards.scss'],
})
export class KundaliScoreCard {
  @Input() scores: any = null;

  get scoreEntries(): Array<{ name: string; value: string | number }> {
    if (!this.scores) {
      return [];
    }

    if (Array.isArray(this.scores)) {
      return this.scores.map((score: any) => ({
        name: score.name ?? 'Score',
        value: score.value ?? ''
      }));
    }

    return Object.entries(this.scores)
      .filter(([_, value]) => !Array.isArray(value))
      .map(([key, value]) => ({
        name: this.formatLabel(key),
        value: value as string | number
      }));
  }

  private formatLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase());
  }
}
