import { Injectable } from '@angular/core';
import { HoraverseHouseArc, HoraversePlanetPoint } from './horaverse-models';

@Injectable({ providedIn: 'root' })
export class HoraverseGeometryService {
  readonly center = 300;
  readonly viewBox = '0 0 600 600';
  private readonly basePlanetRadius = 184;
  private readonly minPlanetRadius = 152;
  private readonly maxPlanetRadius = 216;

  buildHouseArcs(): HoraverseHouseArc[] {
    return Array.from({ length: 12 }, (_, index) => {
      const house = index + 1;
      const startAngle = -90 + index * 30;
      const endAngle = startAngle + 30;
      const label = this.pointOnCircle(startAngle + 15, 250);

      return {
        house,
        startAngle,
        endAngle,
        path: this.describeArcSector(startAngle, endAngle, 276, 118),
        labelX: label.x,
        labelY: label.y
      };
    });
  }

  buildPlanetPoints(planets: any[] | null | undefined, ascendantReferenceLongitude?: number | null): HoraversePlanetPoint[] {
    const rawPoints = (planets ?? [])
      .map((planet, index) => this.toPlanetPoint(planet, index, ascendantReferenceLongitude))
      .filter((point): point is HoraversePlanetPoint => !!point);

    return this.spreadOverlaps(rawPoints);
  }

  pointOnCircle(angle: number, radius: number): { x: number; y: number } {
    const radians = angle * Math.PI / 180;

    return {
      x: this.center + radius * Math.cos(radians),
      y: this.center + radius * Math.sin(radians)
    };
  }

  normalizeAngle(value: number): number {
    return ((value % 360) + 360) % 360;
  }

  private toPlanetPoint(planet: any, index: number, ascendantReferenceLongitude?: number | null): HoraversePlanetPoint | null {
    const longitude = Number(planet?.longitude);

    if (!Number.isFinite(longitude)) {
      return null;
    }

    const latitude = Number(planet?.latitude ?? 0);
    const reference = Number.isFinite(Number(ascendantReferenceLongitude)) ? Number(ascendantReferenceLongitude) : 0;
    const zodiacAngle = this.normalizeAngle(longitude - reference);
    const angle = zodiacAngle - 90;
    const latitudeOffset = Math.max(-24, Math.min(24, (Number.isFinite(latitude) ? latitude : 0) * 3));
    const radius = Math.max(this.minPlanetRadius, Math.min(this.maxPlanetRadius, this.basePlanetRadius + latitudeOffset));
    const point = this.pointOnCircle(angle, radius);
    const planetName = String(planet?.planet ?? planet?.planetName ?? `P${index + 1}`);
    const degree = planet?.degree != null ? `${planet.degree}deg` : `${longitude.toFixed(2)}deg`;
    const minute = planet?.minute != null ? ` ${planet.minute}'` : '';

    return {
      planet: planetName,
      sign: String(planet?.sign ?? ''),
      house: Number.isFinite(Number(planet?.house)) ? Number(planet.house) : null,
      longitude,
      latitude: Number.isFinite(latitude) ? latitude : 0,
      angle,
      radius,
      x: point.x,
      y: point.y,
      tone: this.inferTone(planet),
      label: `${planetName} ${degree}${minute}`
    };
  }

  private spreadOverlaps(points: HoraversePlanetPoint[]): HoraversePlanetPoint[] {
    const grouped = new Map<string, HoraversePlanetPoint[]>();

    points.forEach(point => {
      const key = `${Math.round(point.angle / 4) * 4}:${Math.round(point.radius / 12) * 12}`;
      grouped.set(key, [...(grouped.get(key) ?? []), point]);
    });

    grouped.forEach(group => {
      if (group.length < 2) {
        return;
      }

      const centerIndex = (group.length - 1) / 2;
      group.forEach((point, index) => {
        const spread = index - centerIndex;
        point.angle += spread * 4.5;
        point.radius = Math.max(this.minPlanetRadius, Math.min(this.maxPlanetRadius, point.radius + spread * 8));
        const next = this.pointOnCircle(point.angle, point.radius);
        point.x = next.x;
        point.y = next.y;
      });
    });

    return points;
  }

  private describeArcSector(startAngle: number, endAngle: number, outerRadius: number, innerRadius: number): string {
    const outerStart = this.pointOnCircle(startAngle, outerRadius);
    const outerEnd = this.pointOnCircle(endAngle, outerRadius);
    const innerEnd = this.pointOnCircle(endAngle, innerRadius);
    const innerStart = this.pointOnCircle(startAngle, innerRadius);

    return [
      `M ${outerStart.x} ${outerStart.y}`,
      `A ${outerRadius} ${outerRadius} 0 0 1 ${outerEnd.x} ${outerEnd.y}`,
      `L ${innerEnd.x} ${innerEnd.y}`,
      `A ${innerRadius} ${innerRadius} 0 0 0 ${innerStart.x} ${innerStart.y}`,
      'Z'
    ].join(' ');
  }

  private inferTone(planet: any): string {
    const strength = String(planet?.detailedStrength ?? planet?.conditionTone ?? '');

    if (planet?.planet === 'Moon') {
      return 'moon';
    }

    if (planet?.planet === 'Saturn') {
      return 'saturn';
    }

    if (planet?.isCombust || /Weak|Enemy|Debilitated|Challenging/i.test(strength)) {
      return 'challenging';
    }

    if (/Own|Exalted|Friendly|Supportive/i.test(strength)) {
      return 'supportive';
    }

    return 'neutral';
  }
}
