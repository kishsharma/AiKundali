import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

type ChakraRingKey = 'lagna' | 'moon' | 'sun';

type ChakraRing = {
  key: ChakraRingKey;
  label: string;
  outerRadius: number;
  innerRadius: number;
  textRadius: number;
  className: string;
};

type ChakraSector = {
  ring: ChakraRing;
  house: number;
  path: string;
};

type ChakraPlanetGroup = {
  ring: ChakraRing;
  house: number;
  x: number;
  y: number;
  labels: string[];
};

type ChakraHouseLabel = {
  house: number;
  x: number;
  y: number;
};

@Component({
  selector: 'app-sudarshan-chakra-chart',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './sudarshan-chakra-chart.component.html',
  styleUrls: ['./sudarshan-chakra-chart.component.scss']
})
export class SudarshanChakraChartComponent implements OnChanges {
  constructor(private readonly translocoService: TranslocoService) {}

  @Input() planets: any[] = [];
  @Input() ascendant: string | number | null | undefined = '';

  readonly center = 250;
  readonly viewBox = '0 0 500 500';

  readonly rings: ChakraRing[] = [
    { key: 'lagna', label: 'Lagna', outerRadius: 205, innerRadius: 155, textRadius: 181, className: 'lagna-ring' },
    { key: 'moon', label: 'Moon', outerRadius: 155, innerRadius: 105, textRadius: 131, className: 'moon-ring' },
    { key: 'sun', label: 'Sun', outerRadius: 105, innerRadius: 55, textRadius: 81, className: 'sun-ring' }
  ];

  sectors: ChakraSector[] = [];
  planetGroups: ChakraPlanetGroup[] = [];
  houseLabels: ChakraHouseLabel[] = [];
  lagnaSign: number | null = null;
  moonSign: number | null = null;
  sunSign: number | null = null;

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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['planets'] || changes['ascendant']) {
      this.rebuildChart();
    }
  }

  get hasRequiredData(): boolean {
    return !!(this.lagnaSign && this.moonSign && this.sunSign);
  }

  getSignLabel(sign: number | null): string {
    if (!sign) {
      return '--';
    }

    const label = Object.entries(this.signNames).find(([, value]) => value === sign)?.[0]
      ?.replace(/^\w/, char => char.toUpperCase());

    if (!label) {
      return String(sign);
    }

    const key = `sign.${label}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : label;
  }

  getPlanetShortName(planet: string | null | undefined): string {
    if (!planet) {
      return '';
    }

    const key = `planetShort.${planet}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : (this.shortNames[planet] || planet);
  }

  trackSector(_: number, sector: ChakraSector): string {
    return `${sector.ring.key}-${sector.house}`;
  }

  trackPlanetGroup(_: number, group: ChakraPlanetGroup): string {
    return `${group.ring.key}-${group.house}`;
  }

  trackLabel(_: number, label: ChakraHouseLabel): number {
    return label.house;
  }

  private rebuildChart(): void {
    this.lagnaSign = this.getSignNumber(this.ascendant);
    this.moonSign = this.getPlanetSign('Moon');
    this.sunSign = this.getPlanetSign('Sun');
    this.houseLabels = this.buildHouseLabels();
    this.sectors = this.buildSectors();
    this.planetGroups = this.buildPlanetGroups();
  }

  private buildSectors(): ChakraSector[] {
    return this.rings.flatMap(ring =>
      Array.from({ length: 12 }, (_, index) => {
        const house = index + 1;
        const startAngle = -90 + index * 30;
        const endAngle = startAngle + 30;

        return {
          ring,
          house,
          path: this.describeArcSector(startAngle, endAngle, ring.outerRadius, ring.innerRadius)
        };
      })
    );
  }

  private buildPlanetGroups(): ChakraPlanetGroup[] {
    const referenceSigns: Record<ChakraRingKey, number | null> = {
      lagna: this.lagnaSign,
      moon: this.moonSign,
      sun: this.sunSign
    };

    const groups = new Map<string, ChakraPlanetGroup>();

    this.rings.forEach(ring => {
      for (let house = 1; house <= 12; house++) {
        const midAngle = -90 + (house - 1) * 30 + 15;
        const point = this.pointOnCircle(midAngle, ring.textRadius);

        groups.set(`${ring.key}-${house}`, {
          ring,
          house,
          x: point.x,
          y: point.y,
          labels: []
        });
      }
    });

    this.planets?.forEach(planet => {
      const planetSign = this.getSignNumber(planet?.sign);
      const planetName = this.getPlanetName(planet);

      if (!planetSign || !planetName) {
        return;
      }

      this.rings.forEach(ring => {
        const referenceSign = referenceSigns[ring.key];

        if (!referenceSign) {
          return;
        }

        const house = this.getHouseFromReference(planetSign, referenceSign);
        groups.get(`${ring.key}-${house}`)?.labels.push(planetName);
      });
    });

    return Array.from(groups.values()).filter(group => group.labels.length);
  }

  private buildHouseLabels(): ChakraHouseLabel[] {
    return Array.from({ length: 12 }, (_, index) => {
      const house = index + 1;
      const point = this.pointOnCircle(-90 + index * 30 + 15, 226);

      return {
        house,
        x: point.x,
        y: point.y
      };
    });
  }

  private describeArcSector(startAngle: number, endAngle: number, outerRadius: number, innerRadius: number): string {
    const outerStart = this.pointOnCircle(startAngle, outerRadius);
    const outerEnd = this.pointOnCircle(endAngle, outerRadius);
    const innerEnd = this.pointOnCircle(endAngle, innerRadius);
    const innerStart = this.pointOnCircle(startAngle, innerRadius);

    return [
      `M ${outerStart.x} ${outerStart.y}`,
      `A ${outerRadius} ${outerRadius} 0 0 1 ${outerEnd.x} ${outerEnd.y}`,
      `L ${innerEnd.x} ${innerEnd.y}`,
      `A ${innerRadius} ${innerRadius} 0 0 0 ${innerStart.x} ${innerStart.y}`,
      'Z'
    ].join(' ');
  }

  private pointOnCircle(angle: number, radius: number): { x: number; y: number } {
    const radians = angle * Math.PI / 180;

    return {
      x: this.center + radius * Math.cos(radians),
      y: this.center + radius * Math.sin(radians)
    };
  }

  private getHouseFromReference(planetSign: number, referenceSign: number): number {
    return ((planetSign - referenceSign + 12) % 12) + 1;
  }

  private getPlanetSign(planetName: string): number | null {
    const planet = this.planets?.find(item => this.getPlanetName(item)?.toLowerCase() === planetName.toLowerCase());
    return this.getSignNumber(planet?.sign);
  }

  private getPlanetName(planet: any): string {
    if (!planet) {
      return '';
    }

    if (typeof planet.planet === 'string') {
      return planet.planet;
    }

    return planet.planetName ?? '';
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

    return this.signNames[value.trim().toLowerCase()] || null;
  }
}
