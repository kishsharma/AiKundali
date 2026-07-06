import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
@Component({
  selector: 'app-house-details-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './house-details-card.html',
  styleUrls: ['./house-details-card.scss'],
})
export class HouseDetailsCard {
  @Input() houseData: any;
}
