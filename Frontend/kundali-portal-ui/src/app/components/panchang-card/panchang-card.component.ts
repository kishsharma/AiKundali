import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';

@Component({
  selector: 'app-panchang-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './panchang-card.component.html',
  styleUrls: ['./panchang-card.component.scss'],
})
export class PanchangCardComponent {
  @Input() panchang: any;

  constructor(private readonly apiTerms: ApiTermTranslationService) {}

  translatePanchang(field: 'maas' | 'paksha' | 'tithi' | 'nakshatra' | 'yoga' | 'karana' | 'vaar', value: unknown): string {
    return this.apiTerms.translatePanchangValue(field, value);
  }
}