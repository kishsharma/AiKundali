import { describe, expect, it } from 'vitest';

import { mapDhruvaKundaliContext } from './dhruva-kundali-context.mapper';

describe('mapDhruvaKundaliContext', () => {
  it('maps available fields from the loaded generated Kundali response', () => {
    const context = mapDhruvaKundaliContext({
      kundaliId: 'kundali-123',
      summary: {
        ascendant: 'Aries',
        moonSign: 'Taurus',
        sunSign: 'Gemini',
        nakshatra: 'Rohini',
        currentMahadasha: 'Jupiter',
        currentAntardasha: 'Mercury',
        yogas: [{ name: 'Gaja Kesari Yoga' }],
        doshas: [{ Name: 'Manglik Dosha' }],
      },
      panchang: {
        maas: 'Chaitra',
        paksha: 'Shukla',
        tithi: 'Dwitiya',
        nakshatra: 'Rohini',
        pada: 2,
        yoga: 'Siddhi',
        karana: 'Bava',
        vaar: 'Monday',
      },
      planets: [
        { planet: 'Sun', sign: 'Aries', house: 11, degree: 12, minute: 30, nakshatra: 'Ashwini', pada: 4, isRetrograde: false, isCombust: false, detailedStrength: 'Exalted' },
        { planet: 'Moon', sign: 'Taurus', house: 1, degree: 3, minute: 10, nakshatra: 'Rohini', pada: 2, isRetrograde: false, isCombust: false, detailedStrength: 'Neutral' },
        { planet: 'Mars', detailedStrength: 'Own Sign' },
      ],
    });

    expect(context).toMatchObject({
      kundaliId: { value: 'kundali-123', status: 'available' },
      ascendant: { value: 'Aries', status: 'available' },
      moonSign: { value: 'Taurus', status: 'available' },
      sunSign: { value: 'Gemini', status: 'available' },
      birthNakshatra: { value: 'Rohini', status: 'available' },
      currentMahadasha: { value: 'Jupiter', status: 'available' },
      currentAntardasha: { value: 'Mercury', status: 'available' },
      presentYogas: { value: 'Gaja Kesari Yoga', status: 'available' },
      presentDoshas: { value: 'Manglik Dosha', status: 'available' },
      strongestPlanets: { value: 'Sun (Exalted), Mars (Own Sign)', status: 'available' },
      panchangMaas: { value: 'Chaitra', status: 'available' },
      panchangPaksha: { value: 'Shukla', status: 'available' },
      panchangTithi: { value: 'Dwitiya', status: 'available' },
      panchangNakshatraPada: { value: 'Rohini, Pada 2', status: 'available' },
      panchangYoga: { value: 'Siddhi', status: 'available' },
      panchangKarana: { value: 'Bava', status: 'available' },
      panchangVaar: { value: 'Monday', status: 'available' },
    });
    expect(context.planets[0]).toEqual({
      name: 'Sun',
      sign: { value: 'Aries', status: 'available' },
      house: { value: '11', status: 'available' },
      degree: { value: "12° 30'", status: 'available' },
      nakshatraPada: { value: 'Ashwini, Pada 4', status: 'available' },
      retrogradeStatus: { value: 'not retrograde', status: 'available' },
      combustionStatus: { value: 'not combust', status: 'available' },
      strength: { value: 'Exalted', status: 'available' },
    });
  });

  it('maps saved Kundali wrappers without exposing raw chart data', () => {
    const context = mapDhruvaKundaliContext({
      id: 'saved-456',
      chartData: {
        summary: {
          lagna: 'Leo',
          chandraRashi: 'Cancer',
          suryaRashi: 'Sagittarius',
          nakshatra: 'Pushya',
        },
        currentDasha: {
          mahadasha: 'Saturn',
          antardasha: 'Venus',
        },
        panchang: {
          tithi: 'Ekadashi',
          paksha: 'Krishna',
        },
        yogas: 'Budhaditya Yoga, Chandra Mangala Yoga',
        doshas: ['Kala Sarpa Dosha'],
        planetaryPositions: [
          { planetName: 'Venus', sign: 'Pisces', houseNumber: 8, detailedStrength: 'strength.Exalted' },
        ],
      },
    });

    expect(context.kundaliId).toEqual({ value: 'saved-456', status: 'available' });
    expect(context.ascendant).toEqual({ value: 'Leo', status: 'available' });
    expect(context.moonSign).toEqual({ value: 'Cancer', status: 'available' });
    expect(context.sunSign).toEqual({ value: 'Sagittarius', status: 'available' });
    expect(context.birthNakshatra).toEqual({ value: 'Pushya', status: 'available' });
    expect(context.currentMahadasha).toEqual({ value: 'Saturn', status: 'available' });
    expect(context.currentAntardasha).toEqual({ value: 'Venus', status: 'available' });
    expect(context.presentYogas).toEqual({ value: 'Budhaditya Yoga, Chandra Mangala Yoga', status: 'available' });
    expect(context.presentDoshas).toEqual({ value: 'Kala Sarpa Dosha', status: 'available' });
    expect(context.strongestPlanets).toEqual({ value: 'Venus (Exalted)', status: 'available' });
    expect(context.panchangTithi).toEqual({ value: 'Ekadashi', status: 'available' });
    expect(context.panchangPaksha).toEqual({ value: 'Krishna', status: 'available' });
    expect(context.planets[0].sign).toEqual({ value: 'Pisces', status: 'available' });
    expect(context.planets[0].house).toEqual({ value: '8', status: 'available' });
    expect('chartData' in context).toBe(false);
  });

  it('maps the current Kundali report shape used by the report component', () => {
    const context = mapDhruvaKundaliContext({
      id: 'current-report-1',
      responseSchemaVersion: 'report-schema.3',
      engineVersion: 'calculation-engine.9',
      summary: {
        ascendant: 'Taurus',
        rashi: 'Cancer',
        nakshatra: 'Rohini',
        currentMahadasha: 'Saturn',
        currentAntardasha: 'Venus',
      },
      panchang: {
        maas: 'Vaishakha',
        paksha: 'Krishna',
        tithi: 'Dashami',
        nakshatra: 'Rohini',
        pada: '2',
        yoga: 'Shobhana',
        karana: 'Garaja',
        vaar: 'Friday',
      },
      yogas: [{ name: 'Budhaditya Yoga' }],
      doshas: [{ Name: 'Manglik Dosha' }],
      planets: [
        { name: 'Venus', sign: 'Pisces', house: 8, degree: 21, minute: 4, nakshatra: 'Revati', pada: 1, isRetrograde: false, isCombust: true, detailedStrength: 'strength.Exalted' },
      ],
      houses: [{ houseNumber: 1, score: 72 }],
      scores: [{ houseNumber: 1, score: 72 }],
    });

    expect(context).toMatchObject({
      kundaliId: { value: 'current-report-1', status: 'available' },
      schemaVersion: { value: 'report-schema.3', status: 'available' },
      calculationVersion: { value: 'calculation-engine.9', status: 'available' },
      ascendant: { value: 'Taurus', status: 'available' },
      moonSign: { value: 'Cancer', status: 'available' },
      birthNakshatra: { value: 'Rohini', status: 'available' },
      currentMahadasha: { value: 'Saturn', status: 'available' },
      currentAntardasha: { value: 'Venus', status: 'available' },
      panchangTithi: { value: 'Dashami', status: 'available' },
      panchangVaar: { value: 'Friday', status: 'available' },
      presentYogas: { value: 'Budhaditya Yoga', status: 'available' },
      presentDoshas: { value: 'Manglik Dosha', status: 'available' },
      strongestPlanets: { value: 'Venus (Exalted)', status: 'available' },
    });
    expect(context.planets[0]).toMatchObject({
      name: 'Venus',
      sign: { value: 'Pisces', status: 'available' },
      house: { value: '8', status: 'available' },
      combustionStatus: { value: 'combust', status: 'available' },
      strength: { value: 'Exalted', status: 'available' },
    });
    expect(context.houses[0]).toMatchObject({ houseNumber: 1, score: { value: '72/100', status: 'available' } });
    expect('scores' in context).toBe(false);
  });
  it('keeps wrapper response changes inside the mapper', () => {
    const context = mapDhruvaKundaliContext({
      id: 'wrapper-1',
      data: {
        schemaVersion: 'wrapped-schema.1',
        calculationVersion: 'wrapped-calc.1',
        summary: {
          ascendant: 'Libra',
          moonSign: 'Scorpio',
        },
        panchang: {
          tithi: 'Purnima',
        },
      },
    });

    expect(context.kundaliId).toEqual({ value: 'wrapper-1', status: 'available' });
    expect(context.schemaVersion).toEqual({ value: 'wrapped-schema.1', status: 'available' });
    expect(context.calculationVersion).toEqual({ value: 'wrapped-calc.1', status: 'available' });
    expect(context.ascendant).toEqual({ value: 'Libra', status: 'available' });
    expect(context.moonSign).toEqual({ value: 'Scorpio', status: 'available' });
    expect(context.panchangTithi).toEqual({ value: 'Purnima', status: 'available' });
    expect('data' in context).toBe(false);
  });
  it('marks missing fields without throwing', () => {
    const context = mapDhruvaKundaliContext(null);
    const scalarFields = Object.entries(context).filter(([key]) => !['planets', 'yogas', 'doshas', 'houses'].includes(key)).map(([, value]) => value);

    expect(scalarFields.every((field) => field.value === null && field.status === 'missing')).toBe(true);
    expect(context.planets).toEqual([]);
    expect(context.yogas).toEqual([]);
    expect(context.doshas).toEqual([]);
    expect(context.houses).toEqual([]);
  });

  it('does not derive Dasha values from period timelines', () => {
    const context = mapDhruvaKundaliContext({
      summary: {
        ascendant: 'Virgo',
      },
      chartData: {
        mahadashaPeriods: [
          {
            planet: 'Moon',
            start: '2000-01-01',
            end: '2099-01-01',
            antardashas: [{ subPlanet: 'Mars', start: '2000-01-01', end: '2099-01-01' }],
          },
        ],
      },
    });

    expect(context.currentMahadasha).toEqual({ value: null, status: 'missing' });
    expect(context.currentAntardasha).toEqual({ value: null, status: 'missing' });
  });

  it('does not detect Yogas, Doshas or strongest planets when calculated result fields are empty', () => {
    const context = mapDhruvaKundaliContext({
      yogas: [],
      doshas: [],
      planets: [
        { planet: 'Moon', detailedStrength: 'Neutral' },
      ],
    });

    expect(context.presentYogas).toEqual({ value: null, status: 'missing' });
    expect(context.presentDoshas).toEqual({ value: null, status: 'missing' });
    expect(context.strongestPlanets).toEqual({ value: null, status: 'missing' });
  });
});




