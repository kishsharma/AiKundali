import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
@Component({
  selector: 'app-house-details-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './house-details-card.html',
  styleUrls: ['./house-details-card.scss'],
})
export class HouseDetailsCard {
  @Input() houseData: any;

  constructor(private readonly translocoService: TranslocoService, private readonly apiTerms: ApiTermTranslationService) {}

  get aspectedByText(): string {
    const planets = this.normalizePlanetList(this.houseData?.aspectedBy);

    if (!planets.length) {
      return this.translocoService.translate('common.none');
    }

    return planets
      .map(planet => this.apiTerms.translatePlanet(planet))
      .join(', ');
  }

  get hasAspects(): boolean {
    return this.normalizePlanetList(this.houseData?.aspectedBy).length > 0;
  }

  translateSign(value: unknown): string {
    return this.apiTerms.translateSign(value);
  }

  translatePlanet(value: unknown): string {
    return this.apiTerms.translatePlanet(value);
  }

  private normalizePlanetList(value: any): string[] {
    if (!value) {
      return [];
    }

    if (Array.isArray(value)) {
      return value.map(item => String(item).trim()).filter(Boolean);
    }

    if (typeof value === 'string') {
      return value.split(',').map(item => item.trim()).filter(Boolean);
    }

    return [];
  }
}
