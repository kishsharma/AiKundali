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
export class HouseScoreOverview
implements OnChanges {
constructor(private translocoService: TranslocoService) {}
  @Input() houseCards: any[] = [];
  houses: any[] = [];

  houseNames: any = {

  1: 'house.lagna',
  2: 'house.wealth',
  3: 'house.courage',
  4: 'house.happiness',
  5: 'house.children',
  6: 'house.enemies',
  7: 'house.marriage',
  8: 'house.longevity',
  9: 'house.fortune',
  10: 'house.career',
  11: 'house.gains',
  12: 'house.liberation'

};

  ngOnChanges(): void {
console.log('Received houseCards:', this.houseCards);
    this.houseCards =
      this.houseCards.map(h => ({

        ...h,

        displayName:
          this.houseNames[h.houseNumber],

        status:
          this.getStatus(h.score)

      }));
      console.log('Processed houseCards with display names and statuses:', this.houseCards);

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

}