import { DashaPeriodFact, DashaPlanetReading } from '../../models/mahadasha.model';

export interface RemedyCard {
  key: 'japa' | 'daan' | 'gemstone' | 'conduct';
  titleKey: string;
  value: string;
  detail: string;
}

interface GrahaShanti {
  japa: string;
  weekday: string;
  daan: string;
  recipient: string;
  ratna: string;
  color: string;
}

const SIGN_LORDS = ['Mars', 'Venus', 'Mercury', 'Moon', 'Sun', 'Mercury', 'Venus', 'Mars', 'Jupiter', 'Saturn', 'Saturn', 'Jupiter'];

/** Beej, weekday, daan article, and navaratna named for each graha in the Parashara ratna list. */
const SHANTI: Record<string, GrahaShanti> = {
  Sun: { japa: 'Om Hraam Hreem Hraum Sah Suryaya Namah', weekday: 'Sunday', daan: 'Wheat, jaggery, and copper', recipient: 'a person in public service', ratna: 'Ruby (Manikya)', color: 'Red' },
  Moon: { japa: 'Om Shraam Shreem Shraum Sah Chandraya Namah', weekday: 'Monday', daan: 'Rice, milk, and white cloth', recipient: 'a caregiver', ratna: 'Pearl (Mukta)', color: 'White' },
  Mars: { japa: 'Om Kraam Kreem Kraum Sah Bhaumaya Namah', weekday: 'Tuesday', daan: 'Red lentils and red cloth', recipient: 'someone who can use them', ratna: 'Red coral (Vidruma)', color: 'Red' },
  Mercury: { japa: 'Om Braam Breem Braum Sah Budhaya Namah', weekday: 'Wednesday', daan: 'Green gram and green cloth', recipient: 'a student', ratna: 'Emerald (Marakata)', color: 'Green' },
  Jupiter: { japa: 'Om Graam Greem Graum Sah Gurave Namah', weekday: 'Thursday', daan: 'Turmeric, chana, ghee, and yellow cloth', recipient: 'a teacher or student', ratna: 'Yellow sapphire (Pushyaraga)', color: 'Yellow' },
  Venus: { japa: 'Om Draam Dreem Draum Sah Shukraya Namah', weekday: 'Friday', daan: 'White cloth, rice, and sugar', recipient: 'someone who can use them', ratna: 'Diamond (Vajra)', color: 'White' },
  Saturn: { japa: 'Om Praam Preem Praum Sah Shanaischaraya Namah', weekday: 'Saturday', daan: 'Black sesame, oil, and a blanket', recipient: 'a worker or elder', ratna: 'Blue sapphire (Neelam)', color: 'Dark blue' },
  Rahu: { japa: 'Om Bhraam Bhreem Bhraum Sah Rahave Namah', weekday: 'Saturday', daan: 'Sesame, black cloth, and a blanket', recipient: 'a person left without support', ratna: 'Hessonite (Gomed)', color: 'Smoky blue' },
  Ketu: { japa: 'Om Sraam Sreem Sraum Sah Ketave Namah', weekday: 'Tuesday', daan: 'Sesame and a blanket', recipient: 'a person left without support', ratna: "Cat's eye (Vaidurya)", color: 'Grey' },
};

type GrahaRole = 'node' | 'yogakaraka' | 'benefic' | 'malefic' | 'maraka' | 'neutral';

export function remedyCards(planet: string, reading: DashaPlanetReading | undefined, lagna: string): RemedyCard[] {
  const graha = SHANTI[planet] ?? SHANTI['Saturn'];
  const role = grahaRole(planet, lagna);
  const score = reading?.strength?.overallStrength;
  const combust = isCombust(reading);
  const strong = isStrong(reading, score);
  const stone = ratnaDecision(planet, graha.ratna, role, score, combust, strong);
  const conduct = conductLine(planet, role, strong, combust);

  return [
    {
      key: 'japa',
      titleKey: 'dashaPage.remedyJapa',
      value: graha.japa,
      detail: `Beej of ${planet}, on ${graha.weekday}. Graha shanti for the Mahadasha lord.`,
    },
    {
      key: 'daan',
      titleKey: 'dashaPage.remedyDaan',
      value: graha.daan,
      detail: `On ${graha.weekday}, to ${graha.recipient}. ${daanReason(role, strong, combust)}`,
    },
    {
      key: 'gemstone',
      titleKey: 'dashaPage.remedyGemstone',
      value: stone.value,
      detail: stone.detail,
    },
    {
      key: 'conduct',
      titleKey: 'dashaPage.remedyConduct',
      value: graha.color,
      detail: conduct,
    },
  ];
}

export function remedyBasis(planet: string, lagna: string): string {
  const role = grahaRole(planet, lagna);
  const houses = ruledHouses(parseLagna(lagna), planet);
  const houseText = houses.length ? ` It rules the ${houses.map(ordinal).join(' and ')} for this lagna.` : ' Rahu and Ketu do not rule a sign in Parashara.';
  return `Remedies follow the Mahadasha lord. Parashara reads the dasha from the lord's rulership and strength, and names a ratna for each graha. A ratna strengthens that graha, so it is considered only when strengthening is desirable.${houseText} Role here: ${roleLabel(role)}.`;
}

const HOUSE_MATTER: Record<number, string> = {
  1: 'health and direction',
  2: 'money and family',
  3: 'effort and courage',
  4: 'home and peace of mind',
  5: 'study and children',
  6: 'daily work and health',
  7: 'partnerships',
  8: 'shared money and sudden change',
  9: 'guidance and long journeys',
  10: 'career',
  11: 'income and friendships',
  12: 'expenses and rest',
};

export function remedyDos(planet: string, reading: DashaPlanetReading | undefined, lagna: string): string[] {
  const graha = SHANTI[planet] ?? SHANTI['Saturn'];
  const role = grahaRole(planet, lagna);
  const score = reading?.strength?.overallStrength;
  const combust = isCombust(reading);
  const strong = isStrong(reading, score);
  const matters = matterPhrase(planet, lagna);
  const lines = [
    `On ${graha.weekday}, recite ${graha.japa}.`,
    `On ${graha.weekday}, give ${graha.daan} to ${graha.recipient}.`,
    actionConduct(planet, role, strong, combust, matters),
  ];
  if (combust || (role !== 'benefic' && role !== 'yogakaraka')) {
    lines.push(`Do not wear ${graha.ratna}. Keep to the recitation and the gift.`);
  } else if (strong) {
    lines.push(`Do not add ${graha.ratna}. ${planet} is already able to carry ${matters}.`);
  }
  return lines;
}

export function remedyDonts(facts: DashaPeriodFact[], planet: string, lagna: string): string[] {
  const matters = matterPhrase(planet, lagna);
  const lines = facts
    .filter((fact) => fact.tone === 'Pressure')
    .map((fact) => dontForFact(fact, matters))
    .filter((line): line is string => Boolean(line));
  return [...new Set(lines)];
}

function actionConduct(planet: string, role: GrahaRole, strong: boolean, combust: boolean, matters: string): string {
  if (combust) {
    return `Keep ${matters} modest while ${planet} is combust.`;
  }
  if (role === 'malefic' || role === 'maraka' || role === 'node') {
    return `Do the duties of ${matters} on time, and do not grow them further.`;
  }
  if (strong) {
    return `Use ${planet} for ${matters}, and keep that work on a regular rhythm.`;
  }
  return `Support ${matters} with the weekday practice. Wait before adding a larger plan.`;
}

function dontForFact(fact: DashaPeriodFact, matters: string): string | null {
  const text = fact.text;
  const planet = fact.planet || 'This lord';
  if (text.includes('Combust')) {
    return `Do not force a result in ${matters} while ${planet} is combust.`;
  }
  const conjunct = text.match(/conjunct ([A-Za-z]+)/);
  if (conjunct) {
    return `Do not treat ${planet} together with ${conjunct[1]} as a clear yes. Look at that decision twice.`;
  }
  if (text.includes('aspects')) {
    return `Do not hurry the matter ${planet} aspects. Give it more time.`;
  }
  if (text.includes('Retrograde') && !text.includes('Chart score')) {
    return `Do not read a delay from ${planet} as a final answer. Review it once more.`;
  }
  if (text.includes('Chart score')) {
    return `Do not expand ${matters} while ${planet} is under strain in this chart.`;
  }
  return `Do not push ${matters} harder while ${planet} is under strain.`;
}

function matterPhrase(planet: string, lagna: string): string {
  const houses = ruledHouses(parseLagna(lagna), planet);
  const matters = houses.map((house) => HOUSE_MATTER[house]).filter(Boolean);
  if (matters.length) {
    return joinMatters(matters);
  }
  if (planet === 'Rahu') {
    return 'unusual offers and status';
  }
  if (planet === 'Ketu') {
    return 'withdrawal and unfinished matters';
  }
  return `the matters of ${planet}`;
}

function joinMatters(matters: string[]): string {
  const unique = [...new Set(matters)];
  if (unique.length === 1) {
    return unique[0];
  }
  return `${unique.slice(0, -1).join(', ')} and ${unique[unique.length - 1]}`;
}

function ratnaDecision(
  planet: string,
  ratna: string,
  role: GrahaRole,
  score: number | undefined,
  combust: boolean,
  strong: boolean,
): { value: string; detail: string } {
  const scoreText = score == null ? 'Chart score is not on this reading.' : `Natal score ${score}.`;
  const desirable = role === 'benefic' || role === 'yogakaraka';
  if (!desirable || combust) {
    const reason = role === 'node'
      ? `${planet} does not rule a house, so its ratna is not used to strengthen the period.`
      : combust
        ? `${planet} is combust, so the ratna is not used to strengthen an afflicted graha.`
        : `${planet} is ${roleLabel(role)} for this lagna, so its ratna is not used to strengthen it.`;
    return {
      value: 'Not advised',
      detail: `${ratna} is the navaratna of ${planet}. ${reason} ${scoreText} Shanti is by japa and daan.`,
    };
  }

  if (strong) {
    return {
      value: ratna,
      detail: `${ratna} is the navaratna of ${planet}. ${scoreText} The graha is already strong, so the ratna is named and not added.`,
    };
  }

  return {
    value: ratna,
    detail: `${ratna} is the navaratna of a weak ${roleLabel(role)}. ${scoreText} Strengthening may be considered with a practitioner. This page does not prescribe wearing it.`,
  };
}

function daanReason(role: GrahaRole, strong: boolean, combust: boolean): string {
  if (combust || role === 'malefic' || role === 'maraka' || role === 'node') {
    return 'Given as shanti, to pacify the lord, not to increase it.';
  }
  if (strong) {
    return 'Given to sustain the significations of a lord that is already strong.';
  }
  return 'Given as shanti while the lord is not strong.';
}

function conductLine(planet: string, role: GrahaRole, strong: boolean, combust: boolean): string {
  if (combust) {
    return `Keep the matters of ${planet} modest while it is combust. Do not force a result from this lord.`;
  }
  if (role === 'malefic' || role === 'maraka' || role === 'node') {
    return `Meet the duties of ${planet} on time. Do not expand the houses this lord pressures.`;
  }
  if (strong) {
    return `Use the strength of ${planet} in the houses it rules. Keep the work regular.`;
  }
  return `Support ${planet} by repetition and a steady weekday practice, not by a larger commitment.`;
}

function grahaRole(planet: string, lagnaText: string): GrahaRole {
  if (planet === 'Rahu' || planet === 'Ketu') {
    return 'node';
  }

  const lagna = parseLagna(lagnaText);
  if (lagna == null) {
    return 'neutral';
  }

  const houses = ruledHouses(lagna, planet);
  const trikona = houses.some((house) => house === 1 || house === 5 || house === 9);
  const kendra = houses.some((house) => house === 4 || house === 7 || house === 10);
  const dusthana = houses.some((house) => house === 6 || house === 8 || house === 12);
  const maraka = houses.some((house) => house === 2 || house === 7);
  if (kendra && houses.some((house) => house === 5 || house === 9)) {
    return 'yogakaraka';
  }
  if (trikona) {
    return 'benefic';
  }
  if (dusthana) {
    return 'malefic';
  }
  if (maraka) {
    return 'maraka';
  }
  return 'neutral';
}

function roleLabel(role: GrahaRole): string {
  if (role === 'yogakaraka') return 'yogakaraka';
  if (role === 'benefic') return 'a functional benefic';
  if (role === 'malefic') return 'a functional malefic';
  if (role === 'maraka') return 'a maraka';
  if (role === 'node') return 'a node';
  return 'neutral by ownership';
}

function ruledHouses(lagna: number | null, planet: string): number[] {
  if (lagna == null) {
    return [];
  }

  const houses: number[] = [];
  for (let house = 1; house <= 12; house += 1) {
    if (SIGN_LORDS[(lagna - 1 + house - 1) % 12] === planet) {
      houses.push(house);
    }
  }
  return houses;
}

function isStrong(reading: DashaPlanetReading | undefined, score: number | undefined): boolean {
  const dignity = reading?.strength?.planetStrength ?? '';
  return dignity === 'Very Strong' || dignity === 'Strong' || (score != null && score >= 65);
}

function isCombust(reading: DashaPlanetReading | undefined): boolean {
  return (reading?.facts ?? []).some((fact) => fact.text.includes('Combust'))
    || (reading?.strength?.associationDetails ?? []).some((line) => line.includes('Combust'));
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
