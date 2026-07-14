import { HoraverseGeometryService } from './horaverse-geometry.service';

describe('HoraverseGeometryService', () => {
  let service: HoraverseGeometryService;

  beforeEach(() => {
    service = new HoraverseGeometryService();
  });

  it('builds twelve 30 degree house arcs', () => {
    const arcs = service.buildHouseArcs();

    expect(arcs.length).toBe(12);
    expect(arcs[0].house).toBe(1);
    expect(arcs[0].startAngle).toBe(-90);
    expect(arcs[0].endAngle).toBe(-60);
    expect(arcs[11].house).toBe(12);
    expect(arcs[11].endAngle).toBe(270);
  });

  it('maps planet longitude and latitude into stable SVG coordinates', () => {
    const [moon] = service.buildPlanetPoints([
      {
        planet: 'Moon',
        sign: 'Cancer',
        house: 11,
        longitude: 120,
        latitude: 2,
        degree: 0,
        minute: 12
      }
    ]);

    expect(moon.planet).toBe('Moon');
    expect(moon.longitude).toBe(120);
    expect(moon.radius).toBeGreaterThan(184);
    expect(moon.x).toBeGreaterThan(0);
    expect(moon.y).toBeGreaterThan(0);
    expect(moon.tone).toBe('moon');
  });

  it('spreads overlapping planets while keeping exact labels intact', () => {
    const points = service.buildPlanetPoints([
      { planet: 'Sun', sign: 'Leo', house: 1, longitude: 120, latitude: 0, degree: 0, minute: 0 },
      { planet: 'Mercury', sign: 'Leo', house: 1, longitude: 121, latitude: 0, degree: 1, minute: 0 }
    ]);

    expect(points.length).toBe(2);
    expect(points[0].x).not.toBe(points[1].x);
    expect(points[0].label).toContain('Sun');
    expect(points[1].label).toContain('Mercury');
  });
});
