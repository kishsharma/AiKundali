import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
@Component({
  selector: 'app-kundali-summary',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './kundali-summary.html',
  styleUrls: ['./kundali-summary.scss'],
})
export class KundaliSummary {
  constructor(private translocoService: TranslocoService) {}
  @Input() summary: any;
  @Input() kundaliFormValue: any;
}
