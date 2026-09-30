import { DashaGocharPlanet, DashaPlanetReading } from '../../models/mahadasha.model';

const SIGN_LORDS = ['Mars', 'Venus', 'Mercury', 'Moon', 'Sun', 'Mercury', 'Venus', 'Mars', 'Jupiter', 'Saturn', 'Saturn', 'Jupiter'];

export interface DashaGocharLine {
  planet: string;
  tone: 'Support' | 'Pressure' | 'Quiet';
  text: string;
}

export interface DashaGocharArea {
  name: string;
  tone: 'Support' | 'Pressure' | 'Mixed';
  text: string;
}

const LIFE_AREAS: Array<{ name: string; houses: number[] }> = [
  { name: 'Career', houses: [10] },
  { name: 'Finance', houses: [2, 11] },
  { name: 'Relationships', houses: [7] },
  { name: 'Marriage', houses: [7] },
  { name: 'Health', houses: [1, 6] },
  { name: 'Education', houses: [5] },
  { name: 'Spirituality', houses: [9, 12] },
  { name: 'Travel', houses: [3, 9, 12] },
];

/** Favorable gochar houses counted from the natal Moon. */
const FAVORABLE_FROM_MOON: Record<string, number[]> = {
  Sun: [3, 6, 10, 11],
  Moon: [1, 3, 6, 7, 10, 11],
  Mars: [3, 6, 11],
  Mercury: [2, 4, 6, 8, 10, 11],
  Jupiter: [2, 5, 7, 9, 11],
  Venus: [1, 2, 3, 4, 5, 8, 9, 11, 12],
  Saturn: [3, 6, 11],
  Rahu: [3, 6, 11],
  Ketu: [3, 6, 11],
};

export function buildDashaGochar(input: {
  gochar: DashaGocharPlanet[] | undefined;
  lagna: string;
  readings: DashaPlanetReading[];
  mahadasha: string;
  antardasha: string;
}): { summary: string; areas: DashaGocharArea[]; lines: DashaGocharLine[] } {
  const planets = input.gochar ?? [];
  if (!planets.length || !input.mahadasha) {
    return { summary: '', areas: [], lines: [] };
  }

  const lagna = parseLagna(input.lagna);
  const names = ['Jupiter', 'Saturn', input.mahadasha, input.antardasha].filter((name, index, all) =>
    Boolean(name) && all.indexOf(name) === index);
  const targets = lordHouses(lagna, input.readings, input.mahadasha, input.antardasha);

  const lines = names
    .map((name) => planets.find((planet) => planet.planet === name))
    .filter((planet): planet is DashaGocharPlanet => Boolean(planet))
    .map((planet) => lineFor(planet, targets, input.mahadasha, input.antardasha));

  const shown = names
    .map((name) => planets.find((planet) => planet.planet === name))
    .filter((planet): planet is DashaGocharPlanet => Boolean(planet));
  return { summary: impactSummary(lines), areas: areaImpacts(shown), lines };
}

function lineFor(
  planet: DashaGocharPlanet,
  targets: Array<{ house: number; lord: string; kind: 'occupies' | 'rules' }>,
  mahadasha: string,
  antardasha: string,
): DashaGocharLine {
  const favorable = (FAVORABLE_FROM_MOON[planet.planet] ?? []).includes(planet.houseFromMoon);
  const occupy = targets.find((target) => target.house === planet.houseFromLagna);
  const aspect = targets.find((target) => (planet.aspectedHouses ?? []).includes(target.house));
  const hit = occupy ?? aspect;
  const place = `${planet.planet} is in the ${ordinal(planet.houseFromLagna)} house from the lagna and the ${ordinal(planet.houseFromMoon)} from the Moon.`;
  if (!hit) {
    return {
      planet: planet.planet,
      tone: 'Quiet',
      text: `${place} It does not occupy or aspect a house ${periodLords(mahadasha, antardasha)} rule or sit in, so it does not trigger this period today.`,
    };
  }

  const action = hit === occupy ? 'occupies' : 'aspects';
  const relation = hit.kind === 'occupies' ? 'sits in' : 'rules';
  const role = hit.lord === mahadasha ? 'Mahadasha' : 'Antardasha';
  const contact = `It ${action} the ${ordinal(hit.house)} house, which the ${role} lord ${relation}.`;
  const tone = favorable ? 'Support' : 'Pressure';
  const impact = favorable
    ? 'That house from the Moon is favorable for this graha, so the period can ripen through this contact.'
    : 'That house from the Moon is unfavorable for this graha, so the contact is active and strained.';
  return { planet: planet.planet, tone, text: `${place} ${contact} ${impact}` };
}

function impactSummary(lines: DashaGocharLine[]): string {
  const timing = lines.filter((line) => line.planet === 'Jupiter' || line.planet === 'Saturn');
  if (timing.some((line) => line.tone === 'Support')) {
    return 'Jupiter or Saturn reaches the selected lords from a favorable house from the Moon, so this dasha can give its result now.';
  }
  if (timing.some((line) => line.tone === 'Pressure')) {
    return 'Jupiter or Saturn reaches the selected lords from an unfavorable house from the Moon, so the result is active and strained.';
  }
  if (lines.some((line) => line.tone === 'Support')) {
    return 'A selected lord supports its own houses. Jupiter and Saturn do not open them today.';
  }
  return 'Jupiter and Saturn do not reach these lords from a favorable Moon transit, so today\'s sky does not trigger the period.';
}

function areaImpacts(planets: DashaGocharPlanet[]): DashaGocharArea[] {
  const hits = new Map<string, { tones: Array<'Support' | 'Pressure'>; notes: string[] }>();
  for (const planet of planets) {
    const tone = (FAVORABLE_FROM_MOON[planet.planet] ?? []).includes(planet.houseFromMoon) ? 'Support' : 'Pressure';
    const contacts = [
      { house: planet.houseFromLagna, action: 'occupies' },
      ...(planet.aspectedHouses ?? []).map((house) => ({ house, action: 'aspects' })),
    ];
    for (const contact of contacts) {
      const note = `${planet.planet} ${contact.action} the ${ordinal(contact.house)} house.`;
      for (const area of LIFE_AREAS.filter((item) => item.houses.includes(contact.house))) {
        const current = hits.get(area.name) ?? { tones: [], notes: [] };
        current.tones.push(tone);
        if (!current.notes.includes(note)) {
          current.notes.push(note);
        }
        hits.set(area.name, current);
      }
    }
  }

  return LIFE_AREAS
    .filter((area) => hits.has(area.name))
    .map((area) => {
      const hit = hits.get(area.name)!;
      const support = hit.tones.includes('Support');
      const pressure = hit.tones.includes('Pressure');
      return {
        name: area.name,
        tone: support && pressure ? 'Mixed' : support ? 'Support' : 'Pressure',
        text: hit.notes.join(' '),
      };
    });
}

function periodLords(mahadasha: string, antardasha: string): string {
  return antardasha && antardasha !== mahadasha ? 'these lords' : 'this lord';
}

function lordHouses(
  lagna: number | null,
  readings: DashaPlanetReading[],
  mahadasha: string,
  antardasha: string,
): Array<{ house: number; lord: string; kind: 'occupies' | 'rules' }> {
  const targets: Array<{ house: number; lord: string; kind: 'occupies' | 'rules' }> = [];
  for (const lord of [mahadasha, antardasha].filter(Boolean)) {
    const house = readings.find((reading) => reading.planet === lord)?.house;
    if (house && house >= 1 && house <= 12) {
      targets.push({ house, lord, kind: 'occupies' });
    }
    if (lagna != null) {
      for (let index = 1; index <= 12; index += 1) {
        if (SIGN_LORDS[(lagna - 1 + index - 1) % 12] === lord) {
          targets.push({ house: index, lord, kind: 'rules' });
        }
      }
    }
  }
  return targets;
}

function parseLagna(lagna: string): number | null {
  const leading = Number.parseInt(lagna, 10);
  if (leading >= 1 && leading <= 12) {
    return leading;
  }
  const names = ['Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo', 'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces'];
  const index = names.indexOf(lagna.split('-').pop()?.trim() ?? '');
  return index >= 0 ? index + 1 : null;
}

function ordinal(house: number): string {
  const suffix = house % 10 === 1 && house !== 11 ? 'st' : house % 10 === 2 && house !== 12 ? 'nd' : house % 10 === 3 && house !== 13 ? 'rd' : 'th';
  return `${house}${suffix}`;
}
