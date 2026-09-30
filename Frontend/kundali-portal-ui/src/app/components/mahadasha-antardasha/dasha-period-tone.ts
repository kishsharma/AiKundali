export type DashaPeriodTone = 'favourable' | 'unfavourable' | 'neutral';

const SUPPORTIVE_HOUSES = new Set([1, 4, 5, 7, 9, 10]);
const CHALLENGING_HOUSES = new Set([6, 8, 12]);

export function isBestDashaPeriod(primary: number | null | undefined, companion: number | null | undefined): boolean {
  return primary != null && companion != null && primary >= 65 && companion >= 65;
}

export function dashaPeriodTone(score: number | null | undefined): DashaPeriodTone {
  if (score == null || Number.isNaN(score)) {
    return 'neutral';
  }
  if (score >= 65) {
    return 'favourable';
  }
  if (score < 50) {
    return 'unfavourable';
  }
  return 'neutral';
}

export function blendDashaScores(primary: number | null, companion: number | null): number | null {
  if (primary == null && companion == null) {
    return null;
  }
  if (primary == null) {
    return companion;
  }
  if (companion == null) {
    return primary;
  }
  return Math.round((primary + companion) / 2);
}

export function buildDashaPlanetScores(tables: any, planets: any[] | null | undefined): Record<string, number> {
  const totals = new Map<string, number>();
  const shadbala = new Map<string, number>();

  for (const row of asArray(tables?.planetaryStrengths ?? tables?.PlanetaryStrengths)) {
    const planet = planetKey(firstText(row, 'planet', 'Planet'));
    const score = firstNumber(row, 'totalScore', 'TotalScore');
    if (planet && score != null) {
      totals.set(planet, clampScore(score));
    }
  }

  for (const row of asArray(tables?.shadbala ?? tables?.Shadbala)) {
    const planet = planetKey(firstText(row, 'planet', 'Planet'));
    const percent = firstNumber(row, 'percent', 'Percent');
    if (planet && percent != null) {
      shadbala.set(planet, clampScore(percent));
    }
  }

  const scores: Record<string, number> = {};
  const names = new Set<string>([...totals.keys(), ...shadbala.keys()]);
  names.forEach(name => {
    const parts = [totals.get(name), shadbala.get(name)].filter((value): value is number => value != null);
    scores[name] = Math.round(parts.reduce((sum, value) => sum + value, 0) / parts.length);
  });

  for (const planet of asArray(planets)) {
    const name = planetKey(firstText(planet, 'planet', 'Planet', 'name', 'Name', 'planetName', 'PlanetName'));
    if (!name || scores[name] != null) {
      continue;
    }
    const fallback = fallbackPlanetScore(planet);
    if (fallback != null) {
      scores[name] = fallback;
    }
  }

  return scores;
}

export function fallbackPlanetScore(planet: any): number | null {
  if (!planet) {
    return null;
  }

  let score = 50;
  let used = false;
  const dignity = firstText(planet, 'detailedStrength', 'DetailedStrength').toLowerCase();

  if (dignity.includes('exalted') && dignity.includes('strong')) {
    score += 25;
    used = true;
  } else if (dignity.includes('exalted')) {
    score += 15;
    used = true;
  } else if (dignity.includes('moolatrikona') || dignity.includes('moola')) {
    score += 20;
    used = true;
  } else if (dignity.includes('own')) {
    score += 18;
    used = true;
  } else if (dignity.includes('friend')) {
    score += 10;
    used = true;
  } else if (dignity.includes('debil')) {
    score -= 25;
    used = true;
  } else if (dignity.includes('enemy')) {
    score -= 10;
    used = true;
  } else if (dignity.includes('neutral') || dignity.includes('notapplicable')) {
    used = true;
  }

  const house = firstNumber(planet, 'house', 'House');
  if (house != null && house > 0) {
    used = true;
    if (SUPPORTIVE_HOUSES.has(house)) {
      score += 10;
    } else if (CHALLENGING_HOUSES.has(house)) {
      score -= 10;
    }
  }

  if (firstBoolean(planet, 'isCombust', 'IsCombust')) {
    score -= 15;
    used = true;
  }

  const beneficAspect = firstBoolean(planet, 'hasBeneficAspect', 'HasBeneficAspect');
  const maleficAspect = firstBoolean(planet, 'hasMaleficAspect', 'HasMaleficAspect');
  if (beneficAspect && !maleficAspect) {
    score += 8;
    used = true;
  } else if (maleficAspect && !beneficAspect) {
    score -= 8;
    used = true;
  } else if (beneficAspect && maleficAspect) {
    used = true;
  }

  return used ? clampScore(score) : null;
}

function clampScore(value: number): number {
  return Math.max(0, Math.min(100, Math.round(value)));
}

function planetKey(value: string): string {
  return value.trim().toLowerCase();
}

function asArray(value: unknown): any[] {
  return Array.isArray(value) ? value : [];
}

function firstText(source: any, ...keys: string[]): string {
  for (const key of keys) {
    const value = source?.[key];
    if (typeof value === 'string' && value.trim()) {
      return value.trim();
    }
  }
  return '';
}

function firstNumber(source: any, ...keys: string[]): number | null {
  for (const key of keys) {
    const value = source?.[key];
    if (typeof value === 'number' && !Number.isNaN(value)) {
      return value;
    }
    if (typeof value === 'string' && value.trim() && /^-?\d+(\.\d+)?$/.test(value.trim())) {
      return Number(value);
    }
  }
  return null;
}

function firstBoolean(source: any, ...keys: string[]): boolean {
  return keys.some(key => source?.[key] === true);
}
