import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
@Component({
  selector: 'app-house-details-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './house-details-card.html',
  styleUrls: ['./house-details-card.scss'],
})
export class HouseDetailsCard {
  @Input() houseData: any;

  constructor(private readonly translocoService: TranslocoService) {}

  get aspectedByText(): string {
    const planets = this.normalizePlanetList(this.houseData?.aspectedBy);

    if (!planets.length) {
      return this.translocoService.translate('common.none');
    }

    return planets
      .map(planet => this.translatePlanetName(planet))
      .join(', ');
  }

  get hasAspects(): boolean {
    return this.normalizePlanetList(this.houseData?.aspectedBy).length > 0;
  }

  private translatePlanetName(planet: string): string {
    const key = `planet.${planet}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : planet;
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
