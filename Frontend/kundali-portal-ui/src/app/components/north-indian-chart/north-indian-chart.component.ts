// kundali-chart.component.ts

import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoService } from '@jsverse/transloco';

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
console.log('CHILD INPUT:', this.planets);
    if (changes['planets']) {

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

      const house = planet.house;

console.log('Mapping Planet:', planet, 'to House:', house,'planetname:',planet.planetName);

      if (this.housePlanets[house]) {
console.log('Adding Planet:', planet.planetName, 'to House:', house);
this.housePlanets[house].push(planet);

        //this.housePlanets[house].push(shortNames[planet.planet]);

      }

    });

    console.log(this.housePlanets);

  }

  getPlanets(house: number): string {

    return this.housePlanets[house]?.map(planet => this.getPlanetShortName(planet)).join(' ') || '';

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

  console.log(this.selectedHouseData);

  this.houseSelected.emit(houseData);

}
  
planetPositions: any = {

  1: { x: 240, y: 190 },

  2: { x: 140, y: 100 },

  3: { x: 70, y: 160 },

  4: { x: 150, y: 250 },

  5: { x: 70, y: 350 },

  6: { x: 140, y: 430 },

  7: { x: 240, y: 320 },

  8: { x: 340, y: 430 },

  9: { x: 390, y: 350 },

  10: { x: 300, y: 250 },

  11: { x: 390, y: 140 },

  12: { x: 330, y: 100 }

};


getPlanetX(house: number, index: number): number {

  const verticalHouses = [3, 5, 9, 11];

  if (verticalHouses.includes(house)) {

    return this.planetPositions[house]?.x || 0;

  }

  return (this.planetPositions[house]?.x || 0) + (index * 18);
}

getPlanetY(house: number, index: number): number {

  const verticalHouses = [3, 5, 9, 11];

  if (verticalHouses.includes(house)) {

    return (this.planetPositions[house]?.y || 0) + (index * 18);

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

  12: { x: 335, y: 125 }

};


}
