import { DashaPlanetReading, LifeArea, LifeAreaFactor, LifeAreaLink } from '../../models/mahadasha.model';

const SIGN_LORDS: Record<number, string> = {
  1: 'Mars',
  2: 'Venus',
  3: 'Mercury',
  4: 'Moon',
  5: 'Sun',
  6: 'Mercury',
  7: 'Venus',
  8: 'Mars',
  9: 'Jupiter',
  10: 'Saturn',
  11: 'Saturn',
  12: 'Jupiter',
};

const AREAS: Array<{ name: string; icon: string; houses: number[] }> = [
  { name: 'Career', icon: 'work', houses: [10] },
  { name: 'Finance', icon: 'payments', houses: [2, 11] },
  { name: 'Relationships', icon: 'favorite', houses: [7] },
  { name: 'Marriage', icon: 'diversity_3', houses: [7] },
  { name: 'Health', icon: 'health_and_safety', houses: [1, 6] },
  { name: 'Education', icon: 'school', houses: [5] },
  { name: 'Spirituality', icon: 'self_improvement', houses: [9, 12] },
  { name: 'Travel', icon: 'flight', houses: [3, 9, 12] },
];

const CLASSICAL_LORDS = ['Sun', 'Moon', 'Mars', 'Mercury', 'Jupiter', 'Venus', 'Saturn'];

export function buildDashaLifeAreas(input: {
  lagna: string;
  readings: DashaPlanetReading[];
  mahadasha: string;
  antardasha: string;
}): LifeArea[] | null {
  const lagna = parseLagna(input.lagna);
  const readings = new Map(input.readings.map((reading) => [reading.planet, reading]));
  const chartReady = lagna != null
    && CLASSICAL_LORDS.every((planet) => {
      const house = readings.get(planet)?.house ?? 0;
      return house >= 1 && house <= 12 && typeof readings.get(planet)?.strength?.overallStrength === 'number';
    });

  if (!chartReady || lagna == null) {
    return null;
  }

  return AREAS.map((area) => scoreArea(lagna, readings, input.mahadasha, input.antardasha, area));
}

function scoreArea(
  lagna: number,
  readings: Map<string, DashaPlanetReading>,
  mahadasha: string,
  antardasha: string,
  area: { name: string; icon: string; houses: number[] },
): LifeArea {
  const factors: LifeAreaFactor[] = [];

  for (const house of area.houses) {
    const lord = SIGN_LORDS[houseSign(lagna, house)];
    const lordReading = readings.get(lord);
    const placedHouse = lordReading?.house ?? 0;
    factors.push({
      kind: 'lord',
      weight: 50,
      planet: lord,
      score: lordReading?.strength.overallStrength ?? 68,
      base: lordReading?.strength.overallStrength ?? 68,
      change: '',
      dignity: lordReading?.strength.planetStrength || 'Moderate',
      house,
      placedHouse,
      link: 'rules',
      linkedHouse: house,
    });
  }

  const mahadashaLink = periodLink(lagna, readings, mahadasha, area.houses, 'mahadasha', 30);
  const antardashaLink = periodLink(lagna, readings, antardasha, area.houses, 'antardasha', 20);
  factors.push(mahadashaLink, antardashaLink);

  const lordAverage = average(factors.filter((factor) => factor.kind === 'lord').map((factor) => factor.score));
  const raw = lordAverage * 0.5 + mahadashaLink.score * 0.3 + antardashaLink.score * 0.2;
  const score = clamp(Math.round(raw), 25, 96);
  const weighted = round1(raw);

  return {
    name: area.name,
    icon: area.icon,
    score,
    rating: Math.max(1, Math.round(score / 20)),
    status: scoreToResult(score),
    formula: weighted === String(score)
      ? `${round1(lordAverage)} × 50% + ${mahadashaLink.score} × 30% + ${antardashaLink.score} × 20% = ${score}`
      : `${round1(lordAverage)} × 50% + ${mahadashaLink.score} × 30% + ${antardashaLink.score} × 20% = ${weighted}, rounded to ${score}`,
    factors,
  };
}

function periodLink(
  lagna: number,
  readings: Map<string, DashaPlanetReading>,
  planet: string,
  houses: number[],
  kind: 'mahadasha' | 'antardasha',
  weight: number,
): LifeAreaFactor {
  const reading = readings.get(planet);
  const planetHouse = reading?.house;
  const strength = reading?.strength.overallStrength ?? 68;
  let link: LifeAreaLink = 'none';
  let linkedHouse = houses[0] ?? 0;

  for (const house of houses) {
    if (planetHouse === house) {
      link = 'occupies';
      linkedHouse = house;
      break;
    }
    if (SIGN_LORDS[houseSign(lagna, house)] === planet) {
      link = 'rules';
      linkedHouse = house;
      break;
    }
    if (planetHouse != null && planetHouse >= 1 && aspectOffsets(planet).includes(houseDistance(planetHouse, house))) {
      link = 'aspects';
      linkedHouse = house;
      break;
    }
  }

  const change = link === 'none' ? '− 6' : '+ 8';
  return {
    kind,
    weight,
    planet,
    score: clamp(strength + (link === 'none' ? -6 : 8), 25, 96),
    base: strength,
    change,
    dignity: reading?.strength.planetStrength || 'Moderate',
    house: linkedHouse,
    placedHouse: planetHouse ?? 0,
    link,
    linkedHouse,
  };
}

export function formatLifeHouse(house: number): string {
  return `${ordinal(house)} house`;
}

function round1(value: number): string {
  const rounded = Math.round(value * 10) / 10;
  return Number.isInteger(rounded) ? String(rounded) : rounded.toFixed(1);
}

function houseSign(lagna: number, house: number): number {
  return ((lagna - 1 + house - 1) % 12) + 1;
}

function houseDistance(from: number, to: number): number {
  return ((to - from + 12) % 12) + 1;
}

function aspectOffsets(planet: string): number[] {
  if (planet === 'Jupiter' || planet === 'Rahu' || planet === 'Ketu') {
    return [5, 7, 9];
  }
  if (planet === 'Mars') {
    return [4, 7, 8];
  }
  if (planet === 'Saturn') {
    return [3, 7, 10];
  }
  return [7];
}

function parseLagna(lagna: string): number | null {
  const leading = Number.parseInt(lagna, 10);
  if (leading >= 1 && leading <= 12) {
    return leading;
  }

  const names = ['Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo', 'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces'];
  const name = lagna.split('-').pop()?.trim() ?? '';
  const index = names.indexOf(name);
  return index >= 0 ? index + 1 : null;
}

function scoreToResult(score: number): string {
  if (score >= 80) return 'Very Favorable';
  if (score >= 65) return 'Favorable';
  if (score >= 50) return 'Mixed';
  return 'Challenging';
}

function ordinal(house: number): string {
  const suffix = house === 1 ? 'st' : house === 2 ? 'nd' : house === 3 ? 'rd' : 'th';
  return `${house}${suffix}`;
}

function average(values: number[]): number {
  return values.reduce((sum, value) => sum + value, 0) / values.length;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
