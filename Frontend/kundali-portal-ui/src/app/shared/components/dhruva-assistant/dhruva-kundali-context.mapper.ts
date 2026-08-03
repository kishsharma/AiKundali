export type DhruvaFieldAvailability = 'available' | 'missing';

export interface DhruvaKundaliField {
  value: string | null;
  status: DhruvaFieldAvailability;
}

export interface DhruvaPlanetContext {
  name: string;
  sign: DhruvaKundaliField;
  house: DhruvaKundaliField;
  degree: DhruvaKundaliField;
  nakshatraPada: DhruvaKundaliField;
  retrogradeStatus: DhruvaKundaliField;
  combustionStatus: DhruvaKundaliField;
  strength: DhruvaKundaliField;
}

export interface DhruvaResultDetailContext {
  name: string;
  description: DhruvaKundaliField;
  reason: DhruvaKundaliField;
  category: DhruvaKundaliField;
  strengthOrSeverity: DhruvaKundaliField;
  involvedPlanets: DhruvaKundaliField;
  involvedHouses: DhruvaKundaliField;
  impactAreas: DhruvaKundaliField;
  activationPeriod: DhruvaKundaliField;
}

export interface DhruvaHouseContext {
  houseNumber: number;
  label: string;
  sign: DhruvaKundaliField;
  lord: DhruvaKundaliField;
  occupants: DhruvaKundaliField;
  score: DhruvaKundaliField;
  status: DhruvaKundaliField;
  themes: DhruvaKundaliField;
  lifeAreas: DhruvaKundaliField;
  supportingFactors: DhruvaKundaliField;
  pressureFactors: DhruvaKundaliField;
}

export interface DhruvaKundaliContext {
  kundaliId: DhruvaKundaliField;
  schemaVersion: DhruvaKundaliField;
  calculationVersion: DhruvaKundaliField;
  ascendant: DhruvaKundaliField;
  moonSign: DhruvaKundaliField;
  sunSign: DhruvaKundaliField;
  birthNakshatra: DhruvaKundaliField;
  currentMahadasha: DhruvaKundaliField;
  currentAntardasha: DhruvaKundaliField;
  presentYogas: DhruvaKundaliField;
  presentDoshas: DhruvaKundaliField;
  strongestPlanets: DhruvaKundaliField;
  panchangMaas: DhruvaKundaliField;
  panchangPaksha: DhruvaKundaliField;
  panchangTithi: DhruvaKundaliField;
  panchangNakshatraPada: DhruvaKundaliField;
  panchangYoga: DhruvaKundaliField;
  panchangKarana: DhruvaKundaliField;
  panchangVaar: DhruvaKundaliField;
  yogas: DhruvaResultDetailContext[];
  doshas: DhruvaResultDetailContext[];
  houses: DhruvaHouseContext[];
  strongestHouses: DhruvaKundaliField;
  supportNeededHouses: DhruvaKundaliField;
  planets: DhruvaPlanetContext[];
}

export interface DhruvaKundaliContextSource {
  id?: unknown;
  kundaliId?: unknown;
  savedKundaliId?: unknown;
  data?: unknown;
  result?: unknown;
  kundali?: unknown;
  report?: unknown;
  schemaVersion?: unknown;
  responseSchemaVersion?: unknown;
  calculationVersion?: unknown;
  calculationEngineVersion?: unknown;
  engineVersion?: unknown;
  chartData?: unknown;
  summary?: Record<string, unknown> | null;
  currentDasha?: Record<string, unknown> | null;
  panchang?: Record<string, unknown> | null;
  nakshatra?: unknown;
  nakshatraName?: unknown;
  rashi?: unknown;
  rashiName?: unknown;
}

export function mapDhruvaKundaliContext(source: DhruvaKundaliContextSource | null | undefined): DhruvaKundaliContext {
  const chartData = firstRecord(source?.chartData, source?.data, source?.result, source?.kundali, source?.report);
  const chart = chartData ?? asRecord(source);
  const summary = asRecord(chart?.['summary']);
  const currentDasha = asRecord(chart?.['currentDasha']);
  const panchang = asRecord(chart?.['panchang']);

  return {
    kundaliId: fieldFrom(source?.kundaliId, source?.id, source?.savedKundaliId, chart?.['kundaliId'], chart?.['id']),
    schemaVersion: fieldFrom(source?.schemaVersion, source?.responseSchemaVersion, chart?.['schemaVersion'], chart?.['responseSchemaVersion'], chart?.['chartSchemaVersion']),
    calculationVersion: fieldFrom(source?.calculationVersion, source?.calculationEngineVersion, source?.engineVersion, chart?.['calculationVersion'], chart?.['calculationEngineVersion'], chart?.['engineVersion']),
    ascendant: fieldFrom(summary?.['ascendant'], summary?.['lagna'], chart?.['ascendant']),
    moonSign: fieldFrom(summary?.['moonSign'], summary?.['rashi'], summary?.['chandraRashi'], chart?.['moonSign'], chart?.['rashi'], chart?.['rashiName']),
    sunSign: fieldFrom(summary?.['sunSign'], summary?.['suryaRashi'], chart?.['sunSign'], chart?.['suryaRashi']),
    birthNakshatra: fieldFrom(summary?.['nakshatra'], chart?.['nakshatra'], chart?.['nakshatraName']),
    currentMahadasha: fieldFrom(summary?.['currentMahadasha'], currentDasha?.['mahadasha'], currentDasha?.['planet']),
    currentAntardasha: fieldFrom(summary?.['currentAntardasha'], currentDasha?.['antardasha'], currentDasha?.['subPlanet']),
    presentYogas: namedListFieldFrom(chart?.['yogas'], summary?.['yogas'], summary?.['yoga'], chart?.['yoga'], ['name', 'title', 'label', 'yogaName']),
    presentDoshas: namedListFieldFrom(chart?.['doshas'], summary?.['doshas'], summary?.['dosha'], chart?.['dosha'], ['name', 'Name', 'title', 'label', 'doshaName', 'DoshaName']),
    strongestPlanets: strongestPlanetsFieldFrom(chart?.['planets'], chart?.['planetaryPositions']),
    panchangMaas: fieldFrom(panchang?.['maas'], panchang?.['masa'], panchang?.['month']),
    panchangPaksha: fieldFrom(panchang?.['paksha']),
    panchangTithi: fieldFrom(panchang?.['tithi']),
    panchangNakshatraPada: fieldFrom(formatNakshatraPada(panchang?.['nakshatra'], panchang?.['pada'])),
    panchangYoga: fieldFrom(panchang?.['yoga']),
    panchangKarana: fieldFrom(panchang?.['karana']),
    panchangVaar: fieldFrom(panchang?.['vaar'], panchang?.['vara'], panchang?.['weekday']),
    yogas: resultDetailsFrom(chart?.['yogas'], summary?.['yogas'], summary?.['yoga'], chart?.['yoga'], 'yoga'),
    doshas: resultDetailsFrom(chart?.['doshas'], summary?.['doshas'], summary?.['dosha'], chart?.['dosha'], 'dosha'),
    houses: housesFrom(chart?.['houses'], chart?.['houseScores'], chart?.['bhavaScores']),
    strongestHouses: housesSummaryFieldFrom(chart?.['houses'], chart?.['houseScores'], chart?.['bhavaScores'], 'strong'),
    supportNeededHouses: housesSummaryFieldFrom(chart?.['houses'], chart?.['houseScores'], chart?.['bhavaScores'], 'support'),
    planets: planetsFrom(chart?.['planets'], chart?.['planetaryPositions']),
  };
}

function fieldFrom(...values: unknown[]): DhruvaKundaliField {
  const value = firstText(...values);

  return {
    value: value || null,
    status: value ? 'available' : 'missing',
  };
}

function booleanFieldFrom(value: unknown, trueLabel: string, falseLabel: string): DhruvaKundaliField {
  if (typeof value !== 'boolean') {
    return fieldFrom(value);
  }

  return fieldFrom(value ? trueLabel : falseLabel);
}

function namedListFieldFrom(...args: unknown[]): DhruvaKundaliField {
  const nameKeys = args.at(-1) as string[];
  const values = args.slice(0, -1);
  const items = values.flatMap((value) => namesFrom(value, nameKeys));

  return fieldFrom(joinUnique(items));
}

function strongestPlanetsFieldFrom(...values: unknown[]): DhruvaKundaliField {
  const planets = values
    .filter(Array.isArray)
    .flatMap((value) => value as unknown[])
    .map((planet) => asRecord(planet))
    .filter((planet): planet is Record<string, unknown> => planet !== null)
    .map((planet) => {
      const strength = firstText(planet['detailedStrength'], planet['strength'], planet['Strength']);
      const normalizedStrength = formatStrength(strength);

      if (!isStrongStrength(normalizedStrength)) {
        return '';
      }

      const name = firstText(planet['planet'], planet['planetName'], planet['name'], planet['Name']);
      return name ? `${name} (${normalizedStrength})` : '';
    });

  return fieldFrom(joinUnique(planets));
}

function planetsFrom(...values: unknown[]): DhruvaPlanetContext[] {
  return values
    .filter(Array.isArray)
    .flatMap((value) => value as unknown[])
    .map((planet) => asRecord(planet))
    .filter((planet): planet is Record<string, unknown> => planet !== null)
    .map((planet) => {
      const name = firstText(planet['planet'], planet['planetName'], planet['name'], planet['Name']);
      const degree = formatDegree(planet['degree'], planet['minute']);
      const nakshatraPada = formatNakshatraPada(planet['nakshatra'], planet['pada']);
      const strength = formatStrength(firstText(planet['detailedStrength'], planet['strength'], planet['Strength'], planet['strengthLevel']));

      return name ? {
        name,
        sign: fieldFrom(planet['sign'], planet['rashi']),
        house: fieldFrom(planet['house'], planet['houseNumber']),
        degree: fieldFrom(degree),
        nakshatraPada: fieldFrom(nakshatraPada),
        retrogradeStatus: booleanFieldFrom(planet['isRetrograde'], 'retrograde', 'not retrograde'),
        combustionStatus: booleanFieldFrom(planet['isCombust'], 'combust', 'not combust'),
        strength: fieldFrom(strength),
      } : null;
    })
    .filter((planet): planet is DhruvaPlanetContext => planet !== null);
}

function resultDetailsFrom(...args: unknown[]): DhruvaResultDetailContext[] {
  const kind = args.at(-1) as 'yoga' | 'dosha';
  const values = args.slice(0, -1);

  return values
    .flatMap((value) => resultItemsFrom(value))
    .map((item) => resultDetailFrom(item, kind))
    .filter((item): item is DhruvaResultDetailContext => item !== null);
}

function resultItemsFrom(value: unknown): unknown[] {
  if (Array.isArray(value)) {
    return value;
  }

  if (typeof value === 'string') {
    return value.split(',').map((item) => item.trim()).filter(Boolean);
  }

  return [];
}

function resultDetailFrom(value: unknown, kind: 'yoga' | 'dosha'): DhruvaResultDetailContext | null {
  if (typeof value === 'string') {
    return emptyResultDetail(value);
  }

  const item = asRecord(value);

  if (!item) {
    return null;
  }

  const name = firstText(item['name'], item['Name'], item['title'], item['label'], item[`${kind}Name`], item[kind === 'dosha' ? 'DoshaName' : 'YogaName']);

  if (!name) {
    return null;
  }

  return {
    name,
    description: fieldFrom(item['description'], item['Description']),
    reason: fieldFrom(item['reason'], item['Reason']),
    category: fieldFrom(item['category'], item['Category']),
    strengthOrSeverity: fieldFrom(item['strength'], item['Strength'], item['severity'], item['Severity'], item['status'], item['Status']),
    involvedPlanets: listFieldFrom(item['involvedPlanets'], item['InvolvedPlanets'], item['planets'], item['Planets']),
    involvedHouses: listFieldFrom(item['involvedHouses'], item['InvolvedHouses'], item['houses'], item['Houses']),
    impactAreas: impactAreasFieldFrom(item['impactAreas'], item['ImpactAreas'], item['effects'], item['Effects'], item['impact'], item['Impact']),
    activationPeriod: activationPeriodFieldFrom(item['activationPeriods'], item['ActivationPeriods'], item['activationPeriod'], item['ActivationPeriod']),
  };
}

function emptyResultDetail(name: string): DhruvaResultDetailContext {
  return {
    name,
    description: fieldFrom(),
    reason: fieldFrom(),
    category: fieldFrom(),
    strengthOrSeverity: fieldFrom(),
    involvedPlanets: fieldFrom(),
    involvedHouses: fieldFrom(),
    impactAreas: fieldFrom(),
    activationPeriod: fieldFrom(),
  };
}

function housesFrom(...values: unknown[]): DhruvaHouseContext[] {
  return values
    .filter(Array.isArray)
    .flatMap((value) => value as unknown[])
    .map((house) => asRecord(house))
    .filter((house): house is Record<string, unknown> => house !== null)
    .map((house) => {
      const houseNumber = Number(firstText(house['houseNumber'], house['house'], house['number']));

      return Number.isFinite(houseNumber) && houseNumber >= 1 && houseNumber <= 12
        ? houseFrom(house, houseNumber)
        : null;
    })
    .filter((house): house is DhruvaHouseContext => house !== null);
}

function houseFrom(house: Record<string, unknown>, houseNumber: number): DhruvaHouseContext {
  const scoreText = firstText(house['score'], house['value']);
  const status = firstText(house['status'], house['Status']) || statusFromScore(scoreText);
  const label = firstText(house['houseName'], house['name'], house['title'], standardHouseName(houseNumber));

  return {
    houseNumber,
    label,
    sign: fieldFrom(house['sign'], house['rashi']),
    lord: fieldFrom(house['lord'], house['houseLord'], house['ruler']),
    occupants: listFieldFrom(house['occupants'], house['planets'], house['planetNames']),
    score: fieldFrom(scoreText ? `${scoreText}/100` : ''),
    status: fieldFrom(status),
    themes: listFieldFrom(house['themes'], house['theme']),
    lifeAreas: listFieldFrom(house['lifeAreas'], house['lifeArea']),
    supportingFactors: listFieldFrom(house['supportingFactors'], house['supportFactors'], house['strengths'], house['positiveFactors']),
    pressureFactors: listFieldFrom(house['pressureFactors'], house['challengeFactors'], house['weaknesses'], house['negativeFactors']),
  };
}

function housesSummaryFieldFrom(value: unknown, fallback: unknown, secondFallback: unknown, mode: 'strong' | 'support'): DhruvaKundaliField {
  const houses = housesFrom(value, fallback, secondFallback)
    .filter((house) => house.score.value)
    .sort((a, b) => Number(b.score.value?.replace(/\/100$/, '') ?? 0) - Number(a.score.value?.replace(/\/100$/, '') ?? 0));
  const selected = mode === 'strong' ? houses.slice(0, 3) : houses.slice(-3).reverse();

  return fieldFrom(selected.map((house) => `${house.houseNumber}${ordinalSuffix(house.houseNumber)} house (${house.label}) - ${house.score.value}`).join(', '));
}

function listFieldFrom(...values: unknown[]): DhruvaKundaliField {
  return fieldFrom(joinUnique(values.flatMap((value) => listValuesFrom(value))));
}

function listValuesFrom(value: unknown): string[] {
  if (Array.isArray(value)) {
    return value.flatMap((item) => {
      if (typeof item === 'string' || typeof item === 'number') {
        return [String(item).trim()].filter(Boolean);
      }

      const record = asRecord(item);
      return record ? [firstText(record['name'], record['Name'], record['planet'], record['planetName'], record['label'], record['title'])].filter(Boolean) : [];
    });
  }

  if (typeof value === 'string' || typeof value === 'number') {
    return String(value).split(',').map((item) => item.trim()).filter(Boolean);
  }

  return [];
}

function impactAreasFieldFrom(...values: unknown[]): DhruvaKundaliField {
  const areas = values.flatMap((value) => {
    if (Array.isArray(value)) {
      return value.flatMap((item) => {
        const record = asRecord(item);
        if (!record) return listValuesFrom(item);
        return [firstText(record['category'], record['Category'], record['area'], record['name'], record['label']), ...listValuesFrom(record['statements'] ?? record['Statements'])].filter(Boolean);
      });
    }
    return listValuesFrom(value);
  });

  return fieldFrom(joinUnique(areas));
}

function activationPeriodFieldFrom(...values: unknown[]): DhruvaKundaliField {
  const periods = values.flatMap((value) => {
    if (Array.isArray(value)) {
      return value.map(formatActivationPeriod).filter(Boolean);
    }
    return [formatActivationPeriod(value)].filter(Boolean);
  });

  return fieldFrom(joinUnique(periods));
}

function formatActivationPeriod(value: unknown): string {
  if (typeof value === 'string') {
    return value.trim();
  }

  const record = asRecord(value);
  if (!record) return '';

  return firstText(record['label'], record['period'], record['name'])
    || [firstText(record['start'], record['startDate']), firstText(record['end'], record['endDate'])].filter(Boolean).join(' to ');
}

function statusFromScore(score: string): string {
  const value = Number(score);
  if (!Number.isFinite(value)) return '';
  if (value >= 80) return 'Excellent';
  if (value >= 65) return 'Strong';
  if (value >= 50) return 'Supportive';
  if (value >= 35) return 'Challenging';
  return 'Weak';
}

function standardHouseName(houseNumber: number): string {
  const names: Record<number, string> = {
    1: 'Self',
    2: 'Wealth',
    3: 'Siblings',
    4: 'Property',
    5: 'Children',
    6: 'Service',
    7: 'Marriage',
    8: 'Transformation',
    9: 'Fortune',
    10: 'Career',
    11: 'Gains',
    12: 'Liberation',
  };
  return names[houseNumber] ?? `House ${houseNumber}`;
}

function ordinalSuffix(value: number): string {
  if ([11, 12, 13].includes(value % 100)) return 'th';
  if (value % 10 === 1) return 'st';
  if (value % 10 === 2) return 'nd';
  if (value % 10 === 3) return 'rd';
  return 'th';
}
function namesFrom(value: unknown, nameKeys: string[]): string[] {
  if (Array.isArray(value)) {
    return value
      .map((item) => nameFrom(item, nameKeys))
      .filter(Boolean);
  }

  if (typeof value === 'string') {
    return value
      .split(',')
      .map((item) => item.trim())
      .filter(Boolean);
  }

  return [];
}

function nameFrom(value: unknown, nameKeys: string[]): string {
  if (typeof value === 'string') {
    return value.trim();
  }

  const record = asRecord(value);

  if (!record) {
    return '';
  }

  return firstText(...nameKeys.map((key) => record[key]));
}

function joinUnique(values: string[]): string {
  const seen = new Set<string>();
  const uniqueValues = values
    .map((value) => value.trim())
    .filter((value) => {
      const normalized = value.toLowerCase();
      const keep = Boolean(value) && !seen.has(normalized);
      seen.add(normalized);
      return keep;
    });

  return uniqueValues.join(', ');
}

function formatDegree(degree: unknown, minute: unknown): string {
  const degreeText = firstText(degree);
  const minuteText = firstText(minute);

  if (!degreeText) {
    return '';
  }

  return minuteText ? `${degreeText}° ${minuteText}'` : `${degreeText}°`;
}

function formatNakshatraPada(nakshatra: unknown, pada: unknown): string {
  const nakshatraText = firstText(nakshatra);
  const padaText = firstText(pada);

  if (!nakshatraText) {
    return '';
  }

  return padaText ? `${nakshatraText}, Pada ${padaText}` : nakshatraText;
}

function formatStrength(value: string): string {
  return value
    .replace(/^strength\./, '')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\s+/g, ' ')
    .trim();
}

function isStrongStrength(value: string): boolean {
  return /own sign|own|exalted/i.test(value);
}

function firstText(...values: unknown[]): string {
  for (const value of values) {
    const text = String(value ?? '').trim();
    if (text) {
      return text;
    }
  }

  return '';
}

function firstRecord(...values: unknown[]): Record<string, unknown> | null {
  for (const value of values) {
    const record = asRecord(value);

    if (record) {
      return record;
    }
  }

  return null;
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value)
    ? value as Record<string, unknown>
    : null;
}




