import { Injectable } from '@angular/core';
import { formatPlanetDegreeInSign } from '../../shared/utils/common.utils';
import {
  HoraverseAspectRay,
  HoraverseDashaInsight,
  HoraverseDashaSlice,
  HoraverseHouseArc,
  HoraverseHouseInsight,
  HoraversePlanetNature,
  HoraversePlanetPoint,
  HoraverseSession,
  HoraverseSupportTone
} from './horaverse-models';

@Injectable({ providedIn: 'root' })
export class HoraverseInsightService {
  private readonly beneficPlanets = new Set(['jupiter', 'venus', 'mercury', 'moon']);
  private readonly shadowPlanets = new Set(['rahu', 'ketu']);

  readHouses(session: HoraverseSession | null): HoraverseHouseInsight[] {
    const houses = session?.report?.houses ?? [];
    const byNumber = new Map<number, any>();

    houses.forEach((house: any) => {
      const number = Number(house?.houseNumber ?? house?.HouseNumber);
      if (Number.isInteger(number) && number >= 1 && number <= 12) {
        byNumber.set(number, house);
      }
    });

    return Array.from({ length: 12 }, (_, index) => {
      const house = index + 1;
      const raw = byNumber.get(house) ?? {};
      return {
        house,
        name: String(raw.houseName ?? raw.HouseName ?? ''),
        sign: String(raw.sign ?? raw.Sign ?? ''),
        lord: String(raw.houseOwner ?? raw.HouseOwner ?? raw.houseSwamy ?? raw.HouseSwamy ?? ''),
        occupants: this.toList(raw.occupants ?? raw.Occupants),
        aspectedBy: this.toList(raw.aspectedBy ?? raw.AspectedBy),
        hasBeneficAspect: Boolean(raw.hasBeneficAspect ?? raw.HasBeneficAspect),
        hasMaleficAspect: Boolean(raw.hasMaleficAspect ?? raw.HasMaleficAspect),
        score: Number.isFinite(Number(raw.score ?? raw.Score)) ? Number(raw.score ?? raw.Score) : null,
        themes: this.toList(raw.dominantThemes ?? raw.DominantThemes),
        lifeAreas: this.toList(raw.lifeAreas ?? raw.LifeAreas)
      };
    });
  }

  findHouse(houses: HoraverseHouseInsight[], houseNumber: number | null | undefined): HoraverseHouseInsight | null {
    const number = Number(houseNumber);
    return houses.find(house => house.house === number) ?? null;
  }

  findPlanetPoint(points: HoraversePlanetPoint[], planetName: string | null | undefined): HoraversePlanetPoint | null {
    const key = this.normalizeName(planetName);
    return points.find(point => this.normalizeName(point.planet) === key) ?? null;
  }

  buildAspectRays(
    houses: HoraverseHouseInsight[],
    planetPoints: HoraversePlanetPoint[],
    houseArcs: HoraverseHouseArc[]
  ): HoraverseAspectRay[] {
    const rays: HoraverseAspectRay[] = [];

    houses.forEach(house => {
      const arc = houseArcs.find(item => item.house === house.house);
      if (!arc) {
        return;
      }

      house.aspectedBy.forEach(planetName => {
        const point = this.findPlanetPoint(planetPoints, planetName);
        if (!point) {
          return;
        }

        rays.push({
          fromPlanet: point.planet,
          toHouse: house.house,
          tone: this.natureOf(point.planet) === 'benefic' ? 'benefic' : 'malefic',
          x1: point.x,
          y1: point.y,
          x2: arc.labelX,
          y2: arc.labelY
        });
      });
    });

    return rays;
  }

  readCurrentDasha(session: HoraverseSession | null): HoraverseDashaInsight | null {
    const periods = session?.mahadashaPeriods ?? [];
    const currentMaha = periods.find(period => this.isCurrentPeriod(this.periodStart(period), this.periodEnd(period)));
    const currentDasha = session?.report?.currentDasha ?? {};
    const summary = session?.report?.summary ?? {};

    const mahadasha = this.planetName(currentMaha)
      || String(currentDasha.mahadasha ?? summary.currentMahadasha ?? '');
    if (!mahadasha) {
      return null;
    }

    const antarsRaw = currentMaha?.antardashas ?? currentMaha?.Antardashas ?? [];
    const antars: HoraverseDashaSlice[] = (Array.isArray(antarsRaw) ? antarsRaw : []).map((antar: any) => ({
      planet: this.planetName(antar),
      start: this.asText(this.periodStart(antar)),
      end: this.asText(this.periodEnd(antar)),
      current: this.isCurrentPeriod(this.periodStart(antar), this.periodEnd(antar))
    })).filter((slice: HoraverseDashaSlice) => !!slice.planet);

    const currentAntar = antars.find(slice => slice.current) ?? null;
    const mahaStart = this.asText(this.periodStart(currentMaha) ?? currentDasha.startDate);
    const mahaEnd = this.asText(this.periodEnd(currentMaha) ?? currentDasha.endDate);
    const antarStart = currentAntar?.start || this.asText(currentDasha.antarStartDate ?? currentDasha.startDate);
    const antarEnd = currentAntar?.end || this.asText(currentDasha.antarEndDate ?? currentDasha.endDate);

    return {
      mahadasha,
      antardasha: currentAntar?.planet || String(currentDasha.antardasha ?? summary.currentAntardasha ?? ''),
      mahaStart,
      mahaEnd,
      antarStart,
      antarEnd,
      mahaProgress: this.progress(mahaStart, mahaEnd),
      antarProgress: this.progress(antarStart, antarEnd),
      antars
    };
  }

  natureOf(planetName: string): HoraversePlanetNature {
    const key = this.normalizeName(planetName);
    if (this.beneficPlanets.has(key)) {
      return 'benefic';
    }
    if (this.shadowPlanets.has(key)) {
      return 'shadow';
    }
    return 'malefic';
  }

  supportToneOf(planet: any): HoraverseSupportTone {
    const condition = String(planet?.conditionTone ?? planet?.ConditionTone ?? '').toLowerCase();
    if (condition.includes('support')) {
      return 'supportive';
    }
    if (condition.includes('challeng')) {
      return 'challenging';
    }
    if (condition.includes('mixed')) {
      return 'mixed';
    }
    if (condition.includes('special')) {
      return 'special';
    }

    const strength = String(planet?.detailedStrength ?? planet?.DetailedStrength ?? '');
    const weak = /Weak|Enemy|Debilitated|Challenging/i.test(strength);
    const strong = /Own|Exalted|Friendly|Supportive/i.test(strength);
    const benefic = Boolean(planet?.hasBeneficAspect ?? planet?.HasBeneficAspect);
    const malefic = Boolean(planet?.hasMaleficAspect ?? planet?.HasMaleficAspect);

    if (planet?.isCombust || planet?.IsCombust || (weak && !strong)) {
      return 'challenging';
    }
    if (strong || (benefic && !malefic)) {
      return 'supportive';
    }
    if (benefic && malefic) {
      return 'mixed';
    }
    return 'neutral';
  }

  degreeLabelOf(planet: any, longitude?: number): string {
    const formatted = formatPlanetDegreeInSign(planet);
    if (formatted) {
      return `${formatted}°`;
    }
    if (Number.isFinite(Number(longitude))) {
      return `${((((Number(longitude) % 30) + 30) % 30)).toFixed(1)}°`;
    }
    return '';
  }

  toList(value: unknown): string[] {
    if (!value) {
      return [];
    }
    if (Array.isArray(value)) {
      return value.map(item => String(item).trim()).filter(Boolean);
    }
    if (typeof value === 'string') {
      return value.split(',').map(item => item.trim()).filter(Boolean);
    }
    return [];
  }

  private planetName(period: any): string {
    return String(period?.subPlanet ?? period?.planet ?? period?.Planet ?? period?.SubPlanet ?? '').trim();
  }

  private periodStart(period: any): string | Date | null {
    return period?.startDate ?? period?.start ?? period?.from ?? period?.StartDate ?? null;
  }

  private periodEnd(period: any): string | Date | null {
    return period?.endDate ?? period?.end ?? period?.to ?? period?.EndDate ?? null;
  }

  private isCurrentPeriod(start: string | Date | null, end: string | Date | null): boolean {
    const startDate = this.toDate(start);
    const endDate = this.toDate(end);
    if (!startDate || !endDate) {
      return false;
    }
    const now = Date.now();
    return now >= startDate.getTime() && now <= endDate.getTime();
  }

  private progress(start: string, end: string): number {
    const startDate = this.toDate(start);
    const endDate = this.toDate(end);
    if (!startDate || !endDate) {
      return 0;
    }
    const span = endDate.getTime() - startDate.getTime();
    if (span <= 0) {
      return 0;
    }
    return Math.max(0, Math.min(100, ((Date.now() - startDate.getTime()) / span) * 100));
  }

  private toDate(value: string | Date | null | undefined): Date | null {
    if (!value) {
      return null;
    }
    const date = value instanceof Date ? value : new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private asText(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }
    return value instanceof Date ? value.toISOString() : String(value);
  }

  private normalizeName(value: string | null | undefined): string {
    return String(value ?? '').toLowerCase().replace(/[^a-z]/g, '');
  }
}
