import { DashaPlanetReading } from '../../models/mahadasha.model';

const SIGN_LORDS: Record<number, string> = {
  1: 'Mars', 2: 'Venus', 3: 'Mercury', 4: 'Moon', 5: 'Sun', 6: 'Mercury',
  7: 'Venus', 8: 'Mars', 9: 'Jupiter', 10: 'Saturn', 11: 'Saturn', 12: 'Jupiter',
};

const AREAS: Array<{ name: string; houses: number[] }> = [
  { name: 'Career', houses: [10] },
  { name: 'Finance', houses: [2, 11] },
  { name: 'Health', houses: [1, 6] },
  { name: 'Education', houses: [5] },
  { name: 'Spirituality', houses: [9, 12] },
  { name: 'Travel', houses: [3, 9, 12] },
];

export interface BuiltDashaPhase {
  key: 'start' | 'middle' | 'end';
  titleKey: string;
  start: string;
  end: string;
  points: string[];
  periodLabel?: string;
}

export function buildSelectedDashaPhases(input: {
  lagna: string;
  readings: DashaPlanetReading[];
  antardasha: string;
  start?: string;
  end?: string;
  nextAntardasha?: string;
  pratyantardashas?: Array<{ planet: string; start?: string; end?: string }>;
}): BuiltDashaPhase[] | null {
  const start = parseTime(input.start);
  const end = parseTime(input.end);
  const reading = input.readings.find((item) => item.planet === input.antardasha);
  if (start == null || end == null || end <= start || !reading?.strength?.planet) {
    return null;
  }

  const lagna = parseLagna(input.lagna);
  const firstEnd = start + (end - start) / 3;
  const secondEnd = start + ((end - start) * 2) / 3;
  const areas = lagna == null ? [] : linkedAreaNames(lagna, input.readings, input.antardasha);
  const opening = areas.length
    ? `${joinNames(areas)} become active in this opening third.`
    : 'This opening third carries the Antardasha lord without a direct life-area link.';
  const closing = input.nextAntardasha
    ? `Next Antardasha is ${input.nextAntardasha}, after this one ends on ${day(input.end)}.`
    : `This Antardasha ends on ${day(input.end)}.`;
  const periods = input.pratyantardashas ?? [];

  const phases: BuiltDashaPhase[] = [
    { key: 'start', titleKey: 'dashaPage.phaseStart', start: input.start ?? '', end: toIso(firstEnd), points: [opening, ...periodsIn(periods, start, firstEnd)] },
    { key: 'middle', titleKey: 'dashaPage.phaseMiddle', start: toIso(firstEnd), end: toIso(secondEnd), points: periodsIn(periods, firstEnd, secondEnd) },
    { key: 'end', titleKey: 'dashaPage.phaseEnd', start: toIso(secondEnd), end: input.end ?? '', points: [closing, ...periodsIn(periods, secondEnd, end)] },
  ];

  for (const phase of phases) {
    if (!phase.points.length) {
      phase.points = ['No Pratyantardasha begins in this third.'];
    }
  }

  return phases;
}

function periodsIn(
  periods: Array<{ planet: string; start?: string; end?: string }>,
  from: number,
  to: number,
): string[] {
  return periods
    .filter((period) => {
      const at = parseTime(period.start);
      return at != null && at >= from && at < to;
    })
    .map((period) => `${period.planet} Pratyantardasha, ${day(period.start)} to ${day(period.end)}.`);
}

function day(value?: string): string {
  return value ? value.slice(0, 10) : '';
}

function joinNames(names: string[]): string {
  const unique = [...new Set(names)];
  if (unique.length === 1) return unique[0];
  return `${unique.slice(0, -1).join(', ')} and ${unique[unique.length - 1]}`;
}

function linkedAreaNames(lagna: number, readings: DashaPlanetReading[], planet: string): string[] {
  const byPlanet = new Map(readings.map((reading) => [reading.planet, reading]));
  const house = byPlanet.get(planet)?.house ?? 0;
  const lines: string[] = [];

  for (const area of AREAS) {
    for (const areaHouse of area.houses) {
      const link = houseLink(lagna, planet, house, areaHouse);
      if (link) {
        lines.push(area.name);
      }
    }
  }

  return lines;
}

function houseLink(lagna: number, planet: string, planetHouse: number, house: number): string | null {
  if (planetHouse === house) {
    return 'occupies';
  }
  if (SIGN_LORDS[houseSign(lagna, house)] === planet) {
    return 'rules';
  }
  if (planetHouse >= 1 && aspectOffsets(planet).includes(houseDistance(planetHouse, house))) {
    return 'aspects';
  }
  return null;
}

function houseSign(lagna: number, house: number): number {
  return ((lagna - 1 + house - 1) % 12) + 1;
}

function houseDistance(from: number, to: number): number {
  return ((to - from + 12) % 12) + 1;
}

function aspectOffsets(planet: string): number[] {
  if (planet === 'Jupiter' || planet === 'Rahu' || planet === 'Ketu') return [5, 7, 9];
  if (planet === 'Mars') return [4, 7, 8];
  if (planet === 'Saturn') return [3, 7, 10];
  return [7];
}

function parseLagna(lagna: string): number | null {
  const leading = Number.parseInt(lagna, 10);
  if (leading >= 1 && leading <= 12) return leading;
  const names = ['Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo', 'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces'];
  const index = names.indexOf(lagna.split('-').pop()?.trim() ?? '');
  return index >= 0 ? index + 1 : null;
}

function parseTime(value?: string): number | null {
  if (!value) return null;
  const time = new Date(value).getTime();
  return Number.isNaN(time) ? null : time;
}

function toIso(time: number): string {
  return new Date(time).toISOString();
}

