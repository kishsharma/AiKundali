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
  @Input() compact = false;

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

  get houseName(): string {
    return this.translateHouseKey('name', this.houseData?.houseNumber) || this.houseData?.houseName || '';
  }

  get houseMeaning(): string {
    return this.translateHouseKey('meaning', this.houseData?.houseNumber);
  }

  translateSign(value: unknown): string {
    return this.apiTerms.translateSign(value);
  }

  translatePlanet(value: unknown): string {
    return this.apiTerms.translatePlanet(value);
  }

  private translateHouseKey(kind: 'name' | 'meaning', houseNumber: unknown): string {
    const number = Number(houseNumber);
    if (!Number.isInteger(number) || number < 1 || number > 12) {
      return '';
    }

    const key = `house.${kind}.${number}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : '';
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