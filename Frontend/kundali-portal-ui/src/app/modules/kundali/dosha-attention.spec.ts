import { rankDoshaAttention } from './dosha-attention';

describe('rankDoshaAttention', () => {
  const period = () => '01 Jan 2026 to 01 Jan 2027';

  it('puts a strong current dosha ahead of a high lifetime dosha that is quiet now', () => {
    const report = rankDoshaAttention([
      { name: 'Pitra Dosha', severity: 'High' },
      {
        name: 'Mangal Dosha',
        severity: 'Medium',
        remedies: ['Tuesday fast', 'Hanuman worship'],
        activationPeriods: {
          currentlyActive: [{ activationLevel: 'Strong', mahadashaPlanet: 'Mars', antardashaPlanet: 'Mars' }]
        }
      }
    ], { formatPeriod: period });

    expect(report.focus?.name).toBe('Mangal Dosha');
    expect(report.focus?.tags).toContain('strong');
    expect(report.focus?.remedies).toEqual(['Tuesday fast', 'Hanuman worship']);
    expect(report.also.map((entry) => entry.name)).toEqual(['Pitra Dosha']);
    expect(report.also[0].kind).toBe('lifetime');
    expect(report.usedTiming).toBe(true);
  });

  it('keeps a low dosha with only a mild antardasha out of the attention list', () => {
    const report = rankDoshaAttention([
      {
        name: 'Guru Chandal',
        severity: 'Low',
        activationPeriods: {
          currentlyActive: [{ activationLevel: 'Mild', mahadashaPlanet: 'Jupiter', antardashaPlanet: 'Rahu' }]
        }
      },
      {
        name: 'Kaal Sarpa',
        severity: 'High',
        activationPeriods: {
          currentlyActive: [{ activationLevel: 'Active', mahadashaPlanet: 'Rahu', antardashaPlanet: 'Moon' }]
        }
      }
    ], { formatPeriod: period, planet: (name) => name });

    expect(report.focus?.name).toBe('Kaal Sarpa');
    expect(report.focus?.tags).toEqual(['period', 'lifetime']);
    expect(report.focus?.lines[0].params['maha']).toBe('Rahu');
    expect(report.quiet.map((entry) => entry.name)).toEqual(['Guru Chandal']);
    expect(report.also).toEqual([]);
  });

  it('uses birth-chart strength when timing is missing', () => {
    const report = rankDoshaAttention([
      { name: 'Kemadruma', severity: 'Low' },
      { name: 'Shani Dosha', severity: 'High' }
    ]);

    expect(report.focus?.name).toBe('Shani Dosha');
    expect(report.focus?.kind).toBe('lifetime');
    expect(report.quiet.map((entry) => entry.name)).toEqual(['Kemadruma']);
    expect(report.usedTiming).toBe(false);
  });
});
