import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { NorthIndianChartComponent } from '../north-indian-chart/north-indian-chart.component';

@Component({
  selector: 'app-transit-insight',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, NorthIndianChartComponent],
  templateUrl: './transit-insight.component.html',
  styleUrls: ['./transit-insight.component.scss']
})
export class TransitInsightComponent {
  @Input() transitInsight: any;
  @Input() housesData: any[] = [];

  constructor(private readonly translocoService: TranslocoService) {}

  get activations(): any[] {
    return this.transitInsight?.activations ?? [];
  }

  get dashaActivation(): any | null {
    return this.activations.find(item => item?.isMahadashaPlanet || item?.isAntardashaPlanet) ?? null;
  }

  get highlightedActivation(): any | null {
    return this.activations.find(item =>
      item?.isMahadashaPlanet ||
      item?.isAntardashaPlanet ||
      item?.activatesNatalMahadashaPlanet ||
      item?.activatesNatalAntardashaPlanet ||
      item?.isHouseLordMahadashaPlanet ||
      item?.isHouseLordAntardashaPlanet ||
      this.natalPlanetsForActivation(item).length
    ) ?? this.activations[0] ?? null;
  }

  get rows(): any[] {
    return this.activations.slice(0, 9);
  }

  translatePlanet(planet: string | null | undefined): string {
    return this.translateControlledValue('planet', planet);
  }

  translateSign(sign: string | null | undefined): string {
    return this.translateControlledValue('sign', sign);
  }

  translateHouseFocus(house: number | null | undefined, fallback: string | null | undefined): string {
    if (!house) {
      return fallback || '';
    }

    const key = `transit.houseFocus.${house}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : (fallback || '');
  }

  dashaRoleKey(activation: any): string {
    if (activation?.isMahadashaPlanet && activation?.isAntardashaPlanet) {
      return 'transit.mahadashaAndAntardashaPlanet';
    }

    if (activation?.isMahadashaPlanet) {
      return 'transit.mahadashaPlanet';
    }

    if (activation?.isAntardashaPlanet) {
      return 'transit.antardashaPlanet';
    }

    return 'transit.activePlanet';
  }

  natalActivationText(activation: any): string {
    const planets = this.natalPlanetsForActivation(activation);

    if (!planets.length) {
      return this.translocoService.translate('transit.noNatalPlanet');
    }

    return planets
      .map((planet: string) => this.translatePlanet(planet))
      .join(', ');
  }

  hasNatalActivation(activation: any): boolean {
    return this.natalPlanetsForActivation(activation).length > 0;
  }

  natalPlanetsForActivation(activation: any): string[] {
    const directPlanets = this.normalizePlanetList(activation?.natalPlanetsInHouse);
    if (directPlanets.length) {
      return directPlanets;
    }

    const natalHouse = Number(activation?.natalHouse);
    if (!natalHouse) {
      return [];
    }

    const house = this.housesData?.find((item: any) => Number(item?.houseNumber) === natalHouse);
    return this.normalizePlanetList(house?.occupants);
  }

  dashaLinkLabels(activation: any): string[] {
    const labels: string[] = [];

    if (activation?.isMahadashaPlanet || activation?.isAntardashaPlanet) {
      labels.push(this.translocoService.translate('transit.transitDashaPlanet'));
    }

    if (activation?.activatesNatalMahadashaPlanet || activation?.activatesNatalAntardashaPlanet) {
      labels.push(this.translocoService.translate('transit.activatesNatalDashaPlanet'));
    }

    if (activation?.isHouseLordMahadashaPlanet || activation?.isHouseLordAntardashaPlanet) {
      labels.push(this.translocoService.translate('transit.houseLordDashaPlanet'));
    }

    return labels.length ? labels : [this.translocoService.translate('transit.noDirectDashaLink')];
  }

  formatCalculatedAt(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleString(undefined, {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private translateControlledValue(prefix: string, value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    const key = `${prefix}.${value}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : value;
  }

  private normalizePlanetList(value: any): string[] {
    if (!value) {
      return [];
    }

    if (Array.isArray(value)) {
      return value
        .map(item => this.normalizePlanetName(item))
        .filter(Boolean);
    }

    if (typeof value === 'string') {
      return value
        .split(',')
        .map(item => this.normalizePlanetName(item))
        .filter(Boolean);
    }

    return [];
  }

  private normalizePlanetName(value: any): string {
    if (value == null) {
      return '';
    }

    if (typeof value === 'string') {
      return value.trim();
    }

    return String(value).trim();
  }
}
