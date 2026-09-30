import { blendDashaScores, buildDashaPlanetScores, dashaPeriodTone, isBestDashaPeriod } from './dasha-period-tone';

describe('dasha period tone from the chart', () => {
  it('keeps a weak Jupiter unfavourable and a strong Saturn favourable', () => {
    const scores = buildDashaPlanetScores({
      planetaryStrengths: [
        { planet: 'Jupiter', totalScore: 32 },
        { planet: 'Saturn', totalScore: 88 }
      ],
      shadbala: [
        { planet: 'Jupiter', percent: 40 },
        { planet: 'Saturn', percent: 90 }
      ]
    }, []);

    expect(dashaPeriodTone(scores['jupiter'])).toBe('unfavourable');
    expect(dashaPeriodTone(scores['saturn'])).toBe('favourable');
  });

  it('marks a period best only when both lords are at 65 or above', () => {
    expect(isBestDashaPeriod(78, 82)).toBe(true);
    expect(isBestDashaPeriod(90, 60)).toBe(false);
    expect(isBestDashaPeriod(70, null)).toBe(false);
  });

  it('treats a strong lord inside a weak Mahadasha as neutral', () => {
    expect(dashaPeriodTone(blendDashaScores(82, 40))).toBe('neutral');
  });

  it('stays neutral when this chart has no strength facts', () => {
    expect(dashaPeriodTone(buildDashaPlanetScores(null, [])['venus'])).toBe('neutral');
  });

  it('uses dignity, house, combustion and aspects when Shadbala is absent', () => {
    const scores = buildDashaPlanetScores(null, [
      { planet: 'Mars', detailedStrength: 'Exalted (Strong)', house: 10, hasBeneficAspect: true },
      { planet: 'Moon', detailedStrength: 'Debilitated', house: 8, isCombust: true, hasMaleficAspect: true }
    ]);

    expect(dashaPeriodTone(scores['mars'])).toBe('favourable');
    expect(dashaPeriodTone(scores['moon'])).toBe('unfavourable');
  });
});
