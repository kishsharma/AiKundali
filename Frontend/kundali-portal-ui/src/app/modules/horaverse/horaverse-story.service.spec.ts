import { HoraverseSession } from './horaverse-models';
import { HoraverseStoryService } from './horaverse-story.service';

describe('HoraverseStoryService', () => {
  let service: HoraverseStoryService;

  beforeEach(() => {
    service = new HoraverseStoryService();
  });

  it('produces the five scene HoraVerse journey from report data', () => {
    const scenes = service.buildScenes(createSession());

    expect(scenes.length).toBe(5);
    expect(scenes.map(scene => scene.kind)).toEqual([
      'awakening',
      'solar-ring',
      'kaala',
      'inner-focus',
      'door-ahead'
    ]);
    expect(scenes[2].lines.join(' ')).toContain('I do not read your mind');
    expect(scenes[3].title).toContain('Moon');
  });

  it('uses actual dasha and score factors in the door ahead scene', () => {
    const scene = service.buildScenes(createSession()).find(item => item.kind === 'door-ahead');

    expect(scene).toBeTruthy();
    expect(scene?.lines.join(' ')).toContain('Strong memory and pattern recognition');
    expect(scene?.lines.join(' ')).toContain('Responsibility becomes heavy when delayed');
    expect(scene?.lines.join(' ')).toContain('06 Oct 2023');
  });
});

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
          houseName: 'Gains and networks'
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
