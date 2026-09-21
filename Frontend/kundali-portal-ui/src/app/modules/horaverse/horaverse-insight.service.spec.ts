import { describe, expect, it } from 'vitest';
import { HoraverseInsightService } from './horaverse-insight.service';
import { HoraverseGeometryService } from './horaverse-geometry.service';
import { HoraverseSession } from './horaverse-models';

describe('HoraverseInsightService', () => {
  const insight = new HoraverseInsightService();
  const geometry = new HoraverseGeometryService(insight);

  it('classifies dignity and natural nature from planet fields', () => {
    expect(insight.supportToneOf({ detailedStrength: 'Own Sign', hasBeneficAspect: true })).toBe('supportive');
    expect(insight.supportToneOf({ detailedStrength: 'Debilitated', isCombust: true })).toBe('challenging');
    expect(insight.natureOf('Jupiter')).toBe('benefic');
    expect(insight.natureOf('Saturn')).toBe('malefic');
    expect(insight.natureOf('Rahu')).toBe('shadow');
    expect(insight.degreeLabelOf({ degree: 12, minute: 30 })).toBe('12.5°');
  });

  it('builds house insights and aspect rays from the kundali session', () => {
    const session = createSession();
    const houses = insight.readHouses(session);
    const points = geometry.buildPlanetPoints(session.report.planets, 280);
    const rays = insight.buildAspectRays(houses, points, geometry.buildHouseArcs());

    expect(houses[9].occupants).toContain('Jupiter');
    expect(houses[9].hasBeneficAspect).toBe(true);
    expect(rays.some(ray => ray.toHouse === 10 && ray.fromPlanet === 'Jupiter')).toBe(true);
  });

  it('reads the current mahadasha and antardasha window', () => {
    const dasha = insight.readCurrentDasha(createSession());
    expect(dasha?.mahadasha).toBe('Saturn');
    expect(dasha?.antardasha).toBe('Moon');
    expect(dasha?.antars.length).toBe(2);
    expect(dasha?.antars.find(item => item.planet === 'Moon')?.current).toBe(true);
  });
});

function createSession(): HoraverseSession {
  const now = Date.now();
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
      birthPlace: 'Hingoli',
      latitude: '19.54',
      longitude: '77.17'
    },
    mahadashaPeriods: [
      {
        planet: 'Saturn',
        startDate: new Date(now - 8.64e7).toISOString(),
        endDate: new Date(now + 8.64e7 * 400).toISOString(),
        antardashas: [
          {
            subPlanet: 'Ketu',
            startDate: new Date(now - 8.64e7 * 40).toISOString(),
            endDate: new Date(now - 8.64e7).toISOString()
          },
          {
            subPlanet: 'Moon',
            startDate: new Date(now - 8.64e7).toISOString(),
            endDate: new Date(now + 8.64e7 * 20).toISOString()
          }
        ]
      }
    ],
    report: {
      currentDasha: { mahadasha: 'Saturn', antardasha: 'Moon' },
      planets: [
        {
          planet: 'Jupiter',
          sign: 'Pisces',
          house: 10,
          longitude: 350,
          degree: 20,
          minute: 0,
          detailedStrength: 'Own Sign',
          hasBeneficAspect: true
        }
      ],
      houses: [
        {
          houseNumber: 10,
          houseName: 'Karma',
          occupants: ['Jupiter'],
          aspectedBy: ['Jupiter'],
          hasBeneficAspect: true,
          hasMaleficAspect: false,
          score: 82
        }
      ]
    }
  };
}
