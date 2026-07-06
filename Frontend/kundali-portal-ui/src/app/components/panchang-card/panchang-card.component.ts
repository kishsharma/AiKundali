import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';


@Component({
  selector: 'app-panchang-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './panchang-card.component.html',
  styleUrls: ['./panchang-card.component.scss'],
})
export class PanchangCardComponent {
  constructor(private translocoService: TranslocoService) {}
  @Input() panchang: any;
}
