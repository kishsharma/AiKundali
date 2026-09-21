// kundali-chart.component.ts

import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';
import { formatPlanetDegreeInSign } from '../../shared/utils/common.utils';

@Component({
  selector: 'app-north-indian-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './north-indian-chart.component.html',
  styleUrls: ['./north-indian-chart.component.scss']
})
export class NorthIndianChartComponent implements OnChanges {
  constructor(private readonly translocoService: TranslocoService) {}

  hoveredHouse: number | null = null;

  selectedHouse: number | null = null;

@Input() planets: any[] = [];
@Input() ascendant: string | undefined = '';
@Input() housesData: any[] = [];
@Input() showDegrees = false;
@Output() houseSelected = new EventEmitter<any>();
selectedHouseData: any = null;
zodiacNumbers: number[] = [];

  housePlanets: { [key: number]: any[] } = {};
  houses = [1,2,3,4,5,6,7,8,9,10,11,12];
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

  ngOnChanges(changes: SimpleChanges): void {

    if (changes['planets'] || changes['housesData'] || changes['ascendant']) {

      this.mapPlanetsToHouses();

    }

    if (changes['ascendant']) {

  this.generateZodiacSequence();

}

  }

  mapPlanetsToHouses() {

    // RESET houses

    this.housePlanets = {};

    for (let i = 1; i <= 12; i++) {

      this.housePlanets[i] = [];

    }

    // SAFETY CHECK

    if (!this.planets?.length) {
      return;
    }

    // MAP PLANETS

    this.planets.forEach((planet: any) => {

      const house = this.getPlanetHouse(planet);
      if (this.housePlanets[house]) {
this.housePlanets[house].push(planet);

        //this.housePlanets[house].push(shortNames[planet.planet]);

      }

    });
  }

  getPlanets(house: number): string {

    return this.housePlanets[house]?.map(planet => this.getPlanetShortName(planet)).join(' ') || '';

  }

  private getPlanetHouse(planet: any): number {
    const rawHouse = planet?.house ?? planet?.House ?? planet?.houseNumber ?? planet?.HouseNumber;
    const explicitHouse = this.getHouseNumber(rawHouse);

    if (explicitHouse >= 1 && explicitHouse <= 12) {
      return explicitHouse;
    }

    const planetSign = this.getSignNumber(planet?.sign ?? planet?.Sign);
    const ascendantSign = this.getSignNumber(this.ascendant);

    if (!planetSign || !ascendantSign) {
      return 0;
    }

    return (((planetSign - ascendantSign + 12) % 12) + 1);
  }


  private getHouseNumber(value: number | string | null | undefined): number {
    if (value === null || value === undefined || value === '') {
      return 0;
    }

    const numeric = Number(value);
    if (Number.isFinite(numeric) && numeric >= 1 && numeric <= 12) {
      return numeric;
    }

    const houses: Record<string, number> = {
      first: 1,
      second: 2,
      third: 3,
      fourth: 4,
      fifth: 5,
      sixth: 6,
      seventh: 7,
      eighth: 8,
      ninth: 9,
      tenth: 10,
      eleventh: 11,
      twelfth: 12,
    };

    return houses[String(value).trim().toLowerCase()] ?? 0;
  }
  private getSignNumber(value: number | string | null | undefined): number {
    if (value === null || value === undefined || value === '') {
      return 0;
    }

    if (typeof value === 'number') {
      return value >= 1 && value <= 12 ? value : 0;
    }

    const normalized = value.trim();
    const leadingNumber = normalized.match(/^\d+/)?.[0];
    const numeric = Number(leadingNumber ?? normalized);

    if (Number.isFinite(numeric) && numeric >= 1 && numeric <= 12) {
      return numeric;
    }

    const signName = normalized.replace(/^\d+\s*-\s*/, '').replace(/[^a-z]/gi, '').toLowerCase();
    const signs: Record<string, number> = {
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
      pisces: 12,
    };

    return signs[signName] ?? 0;
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

  private getPlanetName(planet: any): string {
    if (!planet) {
      return '';
    }

    if (typeof planet === 'string') {
      return planet;
    }

    const rawPlanet = planet.planet ?? planet.Planet;
    const name = planet.planetName ?? planet.PlanetName;

    if (name) {
      return name;
    }

    if (typeof rawPlanet === 'number') {
      return this.getPlanetNameFromNumber(rawPlanet);
    }

    return rawPlanet ?? '';
  }


  private getPlanetNameFromNumber(value: number): string {
    const planets: Record<number, string> = {
      0: 'Sun',
      1: 'Moon',
      2: 'Mercury',
      3: 'Venus',
      4: 'Mars',
      5: 'Jupiter',
      6: 'Saturn',
      7: 'Rahu',
      8: 'Ketu',
    };

    return planets[value] ?? '';
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

  hoverHouse(house: number): void {
    this.hoveredHouse = house;
  }

  leaveHouse(): void {
    this.hoveredHouse = null;
  }

selectHouse(house: number): void {

  this.selectedHouse = house;

  const houseData =
    this.housesData.find(
      h => h.houseNumber === house
    );

  this.selectedHouseData = houseData;
  this.houseSelected.emit(houseData);

}
  
planetPositions: any = {

  1: { x: 230, y: 155 },

  2: { x: 140, y: 100 },

  3: { x: 70, y: 160 },

  4: { x: 116, y: 226 },

  5: { x: 70, y: 350 },

  6: { x: 140, y: 430 },

  7: { x: 230, y: 370 },

  8: { x: 340, y: 430 },

  9: { x: 390, y: 350 },

  10: { x: 342, y: 226 },

  11: { x: 390, y: 140 },

  12: { x: 330, y: 100 }

};


private readonly centerSafePlanetOffsets: Record<number, Array<{ x: number; y: number }>> = {

  1: [
    { x: 0, y: 0 },
    { x: 18, y: 0 },
    { x: 36, y: 0 },
    { x: 0, y: 19 },
    { x: 18, y: 19 },
    { x: 36, y: 19 }
  ],

  4: [
    { x: 0, y: -32 },
    { x: 0, y: 32 },
    { x: 28, y: -48 },
    { x: 28, y: 48 },
    { x: 56, y: -64 },
    { x: 56, y: 64 }
  ],

  7: [
    { x: 0, y: 0 },
    { x: 18, y: 0 },
    { x: 36, y: 0 },
    { x: 0, y: -19 },
    { x: 18, y: -19 },
    { x: 36, y: -19 }
  ],

  10: [
    { x: 0, y: -32 },
    { x: 0, y: 32 },
    { x: -28, y: -48 },
    { x: -28, y: 48 },
    { x: -56, y: -64 },
    { x: -56, y: 64 }
  ]

};

private getCenterSafePlanetPosition(house: number, index: number): { x: number; y: number } | null {

  const basePosition = this.planetPositions[house];
  const offsets = this.centerSafePlanetOffsets[house];

  if (!basePosition || !offsets?.length) {
    return null;
  }

  const offset = offsets[index % offsets.length];
  const rowOffset = Math.floor(index / offsets.length) * 14;

  return {
    x: basePosition.x + offset.x,
    y: basePosition.y + offset.y + rowOffset
  };
}

getPlanetX(house: number, index: number): number {

  const centerSafePosition = this.getCenterSafePlanetPosition(house, index);

  if (centerSafePosition) {
    return centerSafePosition.x;
  }

  const verticalHouses = [3, 5, 9, 11];

  if (verticalHouses.includes(house)) {

    return this.planetPositions[house]?.x || 0;

  }

  return (this.planetPositions[house]?.x || 0) + (index * (this.showDegrees ? 28 : 18));
}

getPlanetY(house: number, index: number): number {

  const centerSafePosition = this.getCenterSafePlanetPosition(house, index);

  if (centerSafePosition) {
    return centerSafePosition.y;
  }

  const verticalHouses = [3, 5, 9, 11];

  if (verticalHouses.includes(house)) {

    return (this.planetPositions[house]?.y || 0) + (index * (this.showDegrees ? 16 : 18));

  }

  return this.planetPositions[house]?.y || 0;
}
generateZodiacSequence(): void {

  if (!this.ascendant) {
    return;
  }

  const ascendantNumber=Number(this.ascendant.split('-')[0].trim());

  // if (!isNaN(Number(this.ascendant))) {
  //   ascendantNumber = Number(this.ascendant);
  // } else {
  //   ascendantNumber = Number(this.ascendant.split('-')[0].trim());
  // }

  this.zodiacNumbers = [];

  for (let i = 0; i < 12; i++) {

    let zodiac = ascendantNumber + i;

    if (zodiac > 12) {
      zodiac -= 12;
    }
console.log('Calculated Zodiac:', zodiac);
    this.zodiacNumbers.push(zodiac);
  }

  console.log(this.zodiacNumbers);
}

zodiacLabelPositions: any = {

  1: { x: 240, y: 220 },

  2: { x: 140, y: 128 },

  3: { x: 110, y: 150 },

  4: { x: 201, y: 260 },

  5: { x: 110, y: 360 },

  6: { x: 136, y: 380 },

  7: { x: 240, y: 290 },

  8: { x: 345, y: 380 },

  9: { x: 370, y: 360 },

  10: { x: 280, y: 250 },

  11: { x: 370, y: 160 },

  12: { x: 340, y: 128 }

};


}
