import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';

type ChartCell = {
  sign: number;
  label: string;
  row: number;
  col: number;
};

@Component({
  selector: 'app-south-indian-chart',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './south-indian-chart.component.html',
  styleUrls: ['./south-indian-chart.component.scss']
})
export class SouthIndianChartComponent implements OnChanges {
  @Input() planets: any[] = [];
  @Input() ascendant: string = '';
  @Input() housesData: any[] = [];
  @Output() houseSelected = new EventEmitter<any>();

  hoveredHouse: number | null = null;
  selectedHouse: number | null = null;
  selectedHouseData: any = null;
  ascendantSign: number | null = null;

  readonly cells: ChartCell[] = [
    { sign: 12, label: 'Pisces', row: 1, col: 1 },
    { sign: 1, label: 'Aries', row: 1, col: 2 },
    { sign: 2, label: 'Taurus', row: 1, col: 3 },
    { sign: 3, label: 'Gemini', row: 1, col: 4 },
    { sign: 11, label: 'Aquarius', row: 2, col: 1 },
    { sign: 4, label: 'Cancer', row: 2, col: 4 },
    { sign: 10, label: 'Capricorn', row: 3, col: 1 },
    { sign: 5, label: 'Leo', row: 3, col: 4 },
    { sign: 9, label: 'Sagittarius', row: 4, col: 1 },
    { sign: 8, label: 'Scorpio', row: 4, col: 2 },
    { sign: 7, label: 'Libra', row: 4, col: 3 },
    { sign: 6, label: 'Virgo', row: 4, col: 4 }
  ];

  private readonly shortNames: Record<string, string> = {
    Sun: 'Su',
    Moon: 'Mo',
    Mars: 'Ma',
    Mercury: 'Me',
    Jupiter: 'Ju',
    Venus: 'Ve',
    Saturn: 'Sa',
    Rahu: 'Ra',
    Ketu: 'Ke',
    Uranus: 'Ur',
    Neptune: 'Ne'
  };

  private readonly signNames: Record<string, number> = {
    aries: 1,
    taurus: 2,
    gemini: 3,
    cancer: 4,
    leo: 5,
    virgo: 6,
    libra: 7,
    scorpio: 8,
    sagittarius: 9,
    capricorn: 10,
    aquarius: 11,
    pisces: 12
  };

  housePlanets: Record<number, string[]> = {};

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['ascendant']) {
      this.ascendantSign = this.getSignNumber(this.ascendant);
    }

    if (changes['planets']) {
      this.mapPlanetsToSigns();
    }
  }

  getHouseForSign(sign: number): number {
    if (!this.ascendantSign) {
      return sign;
    }

    return ((sign - this.ascendantSign + 12) % 12) + 1;
  }

  getPlanets(sign: number): string {
    return this.housePlanets[sign]?.join(' ') || '';
  }

  hoverHouse(house: number): void {
    this.hoveredHouse = house;
  }

  leaveHouse(): void {
    this.hoveredHouse = null;
  }

  selectHouse(house: number): void {
    this.selectedHouse = house;
    this.selectedHouseData = this.housesData.find(h => h.houseNumber === house);
    this.houseSelected.emit(this.selectedHouseData);
  }

  private mapPlanetsToSigns(): void {
    this.housePlanets = {};

    for (let sign = 1; sign <= 12; sign++) {
      this.housePlanets[sign] = [];
    }

    this.planets?.forEach((planet: any) => {
      const sign = this.getSignNumber(planet.sign);

      if (!sign) {
        return;
      }

      this.housePlanets[sign].push(this.shortNames[planet.planet] || planet.planet);
    });
  }

  private getSignNumber(value: string | number | null | undefined): number | null {
    if (value === null || value === undefined) {
      return null;
    }

    if (typeof value === 'number') {
      return value >= 1 && value <= 12 ? value : null;
    }

    const numericPrefix = value.match(/^\s*(\d{1,2})/);

    if (numericPrefix) {
      const sign = Number(numericPrefix[1]);
      return sign >= 1 && sign <= 12 ? sign : null;
    }

    const normalized = value.trim().toLowerCase();
    return this.signNames[normalized] || null;
  }
}
