import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-kundali-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './kundali-summary.html',
  styleUrls: ['../../styles/shared/_cards.scss'],
})
export class KundaliSummary {
  @Input() summary: any;
}
