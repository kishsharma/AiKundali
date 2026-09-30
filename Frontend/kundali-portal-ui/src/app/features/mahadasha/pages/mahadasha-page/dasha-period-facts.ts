import { DashaLevelStrength, DashaPeriodFact, DashaPlanetReading } from '../../models/mahadasha.model';

export function factsForPlanet(reading: DashaPlanetReading): DashaPeriodFact[] {
  if (reading.facts?.length) {
    return reading.facts;
  }

  return factsFromStrength(reading.planet, reading.strength);
}

export function factsFromStrength(planet: string, strength?: DashaLevelStrength | null): DashaPeriodFact[] {
  if (!strength?.planet || typeof strength.overallStrength !== 'number') {
    return [];
  }

  const score = strength.overallStrength;
  const tone = score >= 65 ? 'Support' : score < 50 ? 'Pressure' : 'Neutral';
  const sign = strength.sign ? ` in ${strength.sign}` : '';
  const facts: DashaPeriodFact[] = [{
    planet,
    tone,
    score,
    text: `${planet} is ${strength.planetStrength || 'Moderate'} in the ${strength.placement}${sign}. Chart score ${score}.`,
  }];

  for (const detail of strength.associationDetails ?? []) {
    const support = detail.startsWith('Benefic:');
    const pressure = detail.startsWith('Malefic:');
    facts.push({
      planet,
      score,
      tone: support ? 'Support' : pressure ? 'Pressure' : 'Neutral',
      text: detail.replace(/^Benefic:\s*/, '').replace(/^Malefic:\s*/, ''),
    });
  }

  return facts;
}
