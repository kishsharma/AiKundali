import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { formatPlanetDegreeInSign } from '../../shared/utils/common.utils';

type PlacedPlanet = {
  planet: any;
  x: number;
  y: number;
  tickInnerX: number;
  tickInnerY: number;
  tickOuterX: number;
  tickOuterY: number;
  angle: number;
  longitude: number;
};

type WheelSegment = {
  sign: number;
  label: string;
  startAngle: number;
  endAngle: number;
  path: string;
  labelX: number;
  labelY: number;
  houseX: number;
  houseY: number;
  planetX: number;
  planetY: number;
};

@Component({
  selector: 'app-western-chart',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './western-chart.component.html',
  styleUrls: ['./western-chart.component.scss']
})
export class WesternChartComponent implements OnChanges {
  constructor(private readonly translocoService: TranslocoService) {}

  @Input() planets: any[] = [];
  @Input() ascendant: string = '';
  @Input() housesData: any[] = [];
  @Input() showDegrees = false;
  @Input() placeByDegree = false;
  @Output() houseSelected = new EventEmitter<any>();

  hoveredHouse: number | null = null;
  selectedHouse: number | null = null;
  selectedHouseData: any = null;
  ascendantSign: number | null = null;

  readonly center = 250;
  readonly outerRadius = 210;
  readonly innerRadius = 72;
  readonly transitRingRadius = 232;
  readonly transitTickInnerRadius = 210;
  readonly transitLabelRadius = 248;
  readonly signLabels: Record<number, string> = {
    1: 'Aries',
    2: 'Taurus',
    3: 'Gemini',
    4: 'Cancer',
    5: 'Leo',
    6: 'Virgo',
    7: 'Libra',
    8: 'Scorpio',
    9: 'Sagittarius',
    10: 'Capricorn',
    11: 'Aquarius',
    12: 'Pisces'
  };

  readonly segments: WheelSegment[] = this.buildSegments();

  housePlanets: Record<number, any[]> = {};
  placedPlanets: PlacedPlanet[] = [];

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
    if (changes['ascendant']) {
      this.ascendantSign = this.getSignNumber(this.ascendant);
    }

    if (changes['planets'] || changes['placeByDegree']) {
      this.mapPlanetsToSigns();
      this.mapPlanetsByDegree();
    }
  }

  getHouseForSign(sign: number): number {
    if (!this.ascendantSign) {
      return sign;
    }

    return ((sign - this.ascendantSign + 12) % 12) + 1;
  }

  getPlanets(sign: number): any[] {
    return this.housePlanets[sign] ?? [];
  }

  getPlanetShortName(planet: any): string {
    const planetName = this.getPlanetName(planet);
    if (!planetName) {
      return '';
    }

    const key = `planetShort.${planetName}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : (this.shortNames[planetName] || planetName);
  }

  getPlanetToneClass(planet: any): string {
    const tone = (planet?.conditionTone ?? this.inferPlanetTone(planet)).toString().toLowerCase();

    if (tone.includes('challenging')) {
      return 'tone-challenging';
    }

    if (tone.includes('supportive')) {
      return 'tone-supportive';
    }

    if (tone.includes('special')) {
      return 'tone-special';
    }

    if (tone.includes('mixed')) {
      return 'tone-mixed';
    }

    return 'tone-neutral';
  }

  getPlanetDegreeLabel(planet: any): string {
    return formatPlanetDegreeInSign(planet);
  }

  getPlanetTooltip(planet: any): string {
    const name = this.translatePlanetName(this.getPlanetName(planet));
    const tone = planet?.conditionTone || this.inferPlanetTone(planet);
    const parts = [`${name}: ${tone}`];

    if (planet?.detailedStrength) {
      parts.push(planet.detailedStrength);
    }

    if (planet?.isCombust) {
      parts.push('Combust');
    }

    if (planet?.isRetrograde) {
      parts.push('Retrograde');
    }

    if (planet?.aspectedBy?.length) {
      const aspectingPlanets = planet.aspectedBy
        .map((item: string) => this.translatePlanetName(item))
        .join(', ');
      parts.push(`Aspected by ${aspectingPlanets}`);
    }

    return parts.filter(Boolean).join(' | ');
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

  private buildSegments(): WheelSegment[] {
    return Array.from({ length: 12 }, (_, index) => {
      const sign = index + 1;
      const startAngle = -90 + index * 30;
      const endAngle = startAngle + 30;
      const midAngle = startAngle + 15;

      return {
        sign,
        label: this.signLabels[sign],
        startAngle,
        endAngle,
        path: this.describeArcSector(startAngle, endAngle),
        labelX: this.pointOnCircle(midAngle, 185).x,
        labelY: this.pointOnCircle(midAngle, 185).y,
        houseX: this.pointOnCircle(midAngle, 145).x,
        houseY: this.pointOnCircle(midAngle, 145).y,
        planetX: this.pointOnCircle(midAngle, 110).x,
        planetY: this.pointOnCircle(midAngle, 110).y
      };
    });
  }

  private mapPlanetsToSigns(): void {
    this.housePlanets = {};

    for (let sign = 1; sign <= 12; sign++) {
      this.housePlanets[sign] = [];
    }

    this.planets?.forEach((planet: any) => {
      const sign = this.getSignNumber(planet.sign ?? planet.Sign);

      if (!sign) {
        return;
      }

      this.housePlanets[sign].push(planet);
    });
  }

  private mapPlanetsByDegree(): void {
    this.placedPlanets = [];

    if (!this.placeByDegree) {
      return;
    }

    const labelRadius = this.transitLabelRadius;
    const clusterGap = 7;
    const radiusStep = 14;
    const items = (this.planets ?? [])
      .map(planet => {
        const longitude = this.getPlanetAbsoluteLongitude(planet);
        return longitude == null ? null : { planet, longitude };
      })
      .filter((item): item is { planet: any; longitude: number } => item != null)
      .sort((left, right) => left.longitude - right.longitude);

    items.forEach((item, index) => {
      let radius = labelRadius;
      const previous = items[index - 1];
      if (previous && this.longitudeDistance(item.longitude, previous.longitude) < clusterGap) {
        const stacked = this.placedPlanets[this.placedPlanets.length - 1];
        const previousRadius = stacked
          ? Math.hypot(stacked.x - this.center, stacked.y - this.center)
          : labelRadius;
        radius = previousRadius + radiusStep;
      }

      const angle = -90 + item.longitude;
      const point = this.pointOnCircle(angle, radius);
      const tickInner = this.pointOnCircle(angle, this.transitTickInnerRadius);
      const tickOuter = this.pointOnCircle(angle, this.transitRingRadius);
      this.placedPlanets.push({
        planet: item.planet,
        x: point.x,
        y: point.y,
        tickInnerX: tickInner.x,
        tickInnerY: tickInner.y,
        tickOuterX: tickOuter.x,
        tickOuterY: tickOuter.y,
        angle,
        longitude: item.longitude
      });
    });
  }

  getPlanetAbsoluteLongitude(planet: any): number | null {
    const longitude = Number(planet?.longitude ?? planet?.Longitude);
    if (Number.isFinite(longitude)) {
      return ((longitude % 360) + 360) % 360;
    }

    const sign = this.getSignNumber(planet?.sign ?? planet?.Sign);
    if (!sign) {
      return null;
    }

    const degreeLabel = Number(formatPlanetDegreeInSign(planet));
    if (!Number.isFinite(degreeLabel)) {
      return null;
    }

    return ((sign - 1) * 30 + degreeLabel + 360) % 360;
  }

  private longitudeDistance(left: number, right: number): number {
    const delta = Math.abs(left - right) % 360;
    return Math.min(delta, 360 - delta);
  }

  private getPlanetName(planet: any): string {
    if (!planet) {
      return '';
    }

    if (typeof planet === 'string') {
      return planet;
    }

    return typeof planet.planet === 'number'
      ? planet.planetName
      : planet.planet;
  }

  private translatePlanetName(planet: string | null | undefined): string {
    if (!planet) {
      return '';
    }

    const key = `planet.${planet}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : planet;
  }

  private inferPlanetTone(planet: any): string {
    const strength = (planet?.detailedStrength ?? '').toString();
    const isWeak = planet?.isCombust || /Weak|Enemy|Debilitated/i.test(strength);
    const isStrong = /Own|Exalted|Friendly/i.test(strength);

    if (isWeak) {
      return 'Challenging';
    }

    if (planet?.isRetrograde) {
      return 'Special';
    }

    if (isStrong) {
      return 'Supportive';
    }

    return 'Neutral';
  }

  private describeArcSector(startAngle: number, endAngle: number): string {
    const outerStart = this.pointOnCircle(startAngle, this.outerRadius);
    const outerEnd = this.pointOnCircle(endAngle, this.outerRadius);
    const innerEnd = this.pointOnCircle(endAngle, this.innerRadius);
    const innerStart = this.pointOnCircle(startAngle, this.innerRadius);

    return [
      `M ${outerStart.x} ${outerStart.y}`,
      `A ${this.outerRadius} ${this.outerRadius} 0 0 1 ${outerEnd.x} ${outerEnd.y}`,
      `L ${innerEnd.x} ${innerEnd.y}`,
      `A ${this.innerRadius} ${this.innerRadius} 0 0 0 ${innerStart.x} ${innerStart.y}`,
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
