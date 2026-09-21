import { beforeEach, describe, expect, it } from 'vitest';
import { HoraverseInsightService } from './horaverse-insight.service';
import { HoraverseSession } from './horaverse-models';
import { HoraverseStoryService } from './horaverse-story.service';

describe('HoraverseStoryService', () => {
  let service: HoraverseStoryService;

  beforeEach(() => {
    service = new HoraverseStoryService(
      {
        getActiveLang: () => 'en',
        getTranslation: () => translations,
        translate: (key: string) => key
      } as any,
      {
        translatePlanet: (value: unknown) => String(value ?? ''),
        translateSign: (value: unknown) => String(value ?? ''),
        translateNakshatra: (value: unknown) => String(value ?? '')
      } as any,
      new HoraverseInsightService()
    );
  });

  it('produces the five scene HoraVerse journey from report data', () => {
    const scenes = service.buildScenes(createSession());

    expect(scenes.length).toBe(5);
    expect(scenes.map(scene => scene.kind)).toEqual([
      'awakening',
      'solar-ring',
      'house-walk',
      'kaala',
      'door-ahead'
    ]);
    expect(scenes[3].lines.join(' ')).toContain('silence around it');
    expect(scenes[2].lines.join(' ')).toContain('house 11');
    expect(scenes[1].lines.join(' ')).toContain('gains, friends');
  });

  it('uses actual dasha and score factors in the door ahead scene', () => {
    const scene = service.buildScenes(createSession()).find(item => item.kind === 'door-ahead');

    expect(scene).toBeTruthy();
    expect(scene?.lines.join(' ')).toContain('Strong memory and pattern recognition');
    expect(scene?.lines.join(' ')).toContain('Responsibility becomes heavy when delayed');
    expect(scene?.lines.join(' ')).toContain('06 Oct 2023');
  });
});

const translations: Record<string, string> = {
  'horaverse.awakening.kicker': 'The Moment Opens',
  'horaverse.awakening.title': 'Welcome, {{name}}. The Hora opens.',
  'horaverse.awakening.thisChart': 'this chart',
  'horaverse.awakening.preciseMoment': 'a precise moment',
  'horaverse.awakening.onePlace': 'one place',
  'horaverse.awakening.line1': 'At {{time}} on {{date}}, the birth moment settles into the wheel of time.',
  'horaverse.awakening.line2': 'From {{place}}, under {{latitude}} and {{longitude}}.',
  'horaverse.awakening.line3': 'The Panchang brings its first signal: {{panchang}}.',
  'horaverse.awakening.birthDateFallback': 'the recorded birth date',
  'horaverse.awakening.latitude': 'Latitude',
  'horaverse.awakening.longitude': 'Longitude',
  'horaverse.awakening.detailSeparator': ', ',
  'horaverse.awakening.panchangNakshatra': 'Nakshatra {{nakshatra}}, Pada {{pada}}',
  'horaverse.common.unknown': 'unknown',
  'horaverse.common.a': 'a',
  'horaverse.common.quiet': 'quiet',
  'horaverse.common.to': 'to',
  'horaverse.solarRing.kicker': 'The Houses Turn',
  'horaverse.solarRing.title': '{{ascendant}} rises at the eastern gate',
  'horaverse.solarRing.ascendantFallback': 'The ascendant',
  'horaverse.solarRing.signFallback': 'the Moon sign',
  'horaverse.solarRing.line1': 'Each house becomes a chamber.',
  'horaverse.solarRing.line2': 'The Moon rests in {{sign}}, inside house {{house}}: the field of {{houseMeaning}}.',
  'horaverse.solarRing.line3': 'overall {{overall}}, growth {{growth}}, challenge {{challenge}}. {{direction}}',
  'horaverse.solarRing.directionBalanced': 'Growth and challenge stand close together.',
  'horaverse.houseMeaning.unknown': 'an unnamed life area',
  'horaverse.houseMeaning.1': 'identity and body',
  'horaverse.houseMeaning.11': 'gains, friends, networks, and future hopes',
  'horaverse.houseWalk.kicker': 'The Twelve Chambers',
  'horaverse.houseWalk.title': 'Each house is a lived room, not a number',
  'horaverse.houseWalk.line1': 'Walk the wheel house by house.',
  'horaverse.houseWalk.line2': 'The Moon currently occupies house {{house}}, the field of {{meaning}}. Occupants here: {{occupants}}.',
  'horaverse.houseWalk.line3': 'House {{house}} currently carries the strongest support ({{support}}). House {{pressureHouse}} holds more friction ({{pressure}}).',
  'horaverse.hud.emptyOccupants': 'no graha sitting here',
  'horaverse.hud.mixed': 'mixed light',
  'horaverse.hud.beneficAspect': 'benefic aspect',
  'horaverse.hud.maleficAspect': 'malefic aspect',
  'horaverse.hud.neutral': 'quiet balance',
  'horaverse.kaala.kicker': 'KAALA Reads',
  'horaverse.kaala.title': 'The witness behind the Hora opens one eye',
  'horaverse.kaala.line1': 'KAALA is not a person in the chart. He is the silence around it, reading pattern without fear or praise.',
  'horaverse.kaala.line2': 'The larger current is {{mahadasha}}.',
  'horaverse.kaala.line3': 'Inside it, {{antardasha}} moves like a smaller tide.',
  'horaverse.kaala.stillUnfolding': 'still unfolding',
  'horaverse.kaala.smallerCurrent': 'a smaller current',
  'horaverse.kaala.saturnLine': 'Saturn stands in house {{house}} with {{strength}} weight.',
  'horaverse.kaala.saturnQuiet': 'Saturn stays quiet.',
  'horaverse.doorAhead.kicker': 'The Door Ahead',
  'horaverse.doorAhead.title': 'The chart points toward the next usable opening',
  'horaverse.doorAhead.strongestSupport': 'Strongest support: {{value}}.',
  'horaverse.doorAhead.frictionPoint': 'Friction point: {{value}}.',
  'horaverse.doorAhead.repeatingClue': 'Repeating clue: {{value}}.',
  'horaverse.doorAhead.cycleActive': 'This cycle is active from {{window}}.',
  'horaverse.doorAhead.currentFallback': 'the active dasha'
};

function createSession(): HoraverseSession {
  return {
    version: 1,
    createdAt: '2026-07-13T00:00:00.000Z',
    tone: 'mysterious-safe',
    theme: 'default-neuro-cosmic',
    birthProfile: {
      name: 'Kishore',
      gender: 'Male',
      birthDate: '1986-10-06',
      birthTime: '11:45',
      birthPlace: 'Hingoli, Maharashtra, India',
      latitude: '19.5431163',
      longitude: '77.1739432'
    },
    mahadashaPeriods: [],
    report: {
      summary: {
        ascendant: 'Capricorn',
        moonSign: 'Cancer',
        currentMahadasha: 'Saturn',
        currentAntardasha: 'Moon'
      },
      currentDasha: {
        mahadasha: 'Saturn',
        antardasha: 'Moon',
        startDate: '2023-10-06T00:00:00',
        endDate: '2042-10-06T00:00:00'
      },
      planets: [
        {
          planet: 'Moon',
          sign: 'Cancer',
          house: 11,
          nakshatra: 'Pushya',
          pada: 2,
          detailedStrength: 'Friendly'
        },
        {
          planet: 'Saturn',
          sign: 'Scorpio',
          house: 4,
          detailedStrength: 'Weak'
        }
      ],
      houses: [
        {
          houseNumber: 11,
          houseName: 'Gains and networks',
          occupants: ['Moon'],
          hasBeneficAspect: true,
          score: 74
        }
      ],
      scores: {
        overall: 71,
        growth: 78,
        challenge: 63,
        dominantStrengths: ['Strong memory and pattern recognition'],
        dominantWeaknesses: ['Responsibility becomes heavy when delayed'],
        factors: ['Moon and Saturn are both active in the current story']
      }
    }
  };
}
