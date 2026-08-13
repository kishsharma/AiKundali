import { Component, Input, OnChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { TranslocoModule } from '@jsverse/transloco';
@Component({
  selector: 'app-house-score-overview',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, TranslocoModule],
  templateUrl: './house-score-overview.html',
  styleUrls: ['./house-score-overview.scss'],
})
export class HouseScoreOverview implements OnChanges {
  constructor(private translocoService: TranslocoService) {}

  @Input() houseCards: any[] = [];
  houses: any[] = [];

  ngOnChanges(): void {
    this.houseCards = this.houseCards.map(h => {
      const houseNumber = this.resolveHouseNumber(h);

      return {
        ...h,
        houseNumber,
        displayName: this.translateHouseValue('name', houseNumber, h.houseName || h.displayName),
        meaning: this.translateHouseValue('meaning', houseNumber, h.meaning),
        status: this.getStatus(h.score)
      };
    });
  }

  getStatus(score: number): string {
    if (score >= 80) {
      return 'status.excellent';
    }

    if (score >= 65) {
      return 'status.strong';
    }

    if (score >= 50) {
      return 'status.supportive';
    }

    if (score >= 35) {
      return 'status.challenging';
    }

    return 'status.weak';
  }

  private translateHouseValue(kind: 'name' | 'meaning', houseNumber: number, fallback?: string): string {
    if (!Number.isInteger(houseNumber) || houseNumber < 1 || houseNumber > 12) {
      return fallback || '';
    }

    const key = `house.${kind}.${houseNumber}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : fallback || '';
  }

  private resolveHouseNumber(house: any): number {
    const direct = Number(house?.houseNumber ?? house?.house);
    if (Number.isInteger(direct)) {
      return direct;
    }

    const match = String(house?.houseNumber ?? house?.house ?? '').match(/\d+/);
    return match ? Number(match[0]) : 0;
  }
}