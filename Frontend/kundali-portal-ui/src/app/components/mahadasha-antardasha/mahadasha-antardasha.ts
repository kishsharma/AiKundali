import { Component, Input } from '@angular/core';
import { CommonModule} from "@angular/common";
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { DashaPeriodTone, blendDashaScores, dashaPeriodTone, isBestDashaPeriod } from './dasha-period-tone';
@Component({
  selector: 'app-mahadasha-antardasha',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './mahadasha-antardasha.html',
  styleUrls: ['./mahadasha-antardasha.scss'],
  host: {
    '[class.compact-dasha]': 'compact'
  }
})
export class MahadashaAntardasha {

  constructor(private translocoService: TranslocoService) {}

  @Input() mahadashas: any[] = [];
  @Input() planetScores: Record<string, number> | null = null;
  @Input() compact = false;

  get displayedMahadashas(): any[] {
    if (!this.compact) {
      return (this.mahadashas ?? []).slice(0, 6);
    }

    return this.mahadashas ?? [];
  }

  isCurrentPeriod(start: string | Date | null, end: string | Date | null): boolean {
    const startDate = this.toDate(start);
    const endDate = this.toDate(end);

    if (!startDate || !endDate) {
      return false;
    }

    const today = this.startOfDay(new Date());

    return today >= this.startOfDay(startDate)
      && today <= this.startOfDay(endDate);
}

getCurrentDashaSummary(): { mahadasha: any; antardasha: any | null; remainingEnd: string | Date | null } | null {
  const mahadasha = this.mahadashas.find(maha =>
    this.isCurrentPeriod(this.getPeriodStart(maha), this.getPeriodEnd(maha))
  );

  if (!mahadasha) {
    return null;
  }

  const antardasha = mahadasha.antardashas?.find((antar: any) =>
    this.isCurrentPeriod(this.getPeriodStart(antar), this.getPeriodEnd(antar))
  ) ?? null;

  return {
    mahadasha,
    antardasha,
    remainingEnd: antardasha ? this.getPeriodEnd(antardasha) : this.getPeriodEnd(mahadasha)
  };
}

getPlanetName(period: any): string {
  return period?.subPlanet || period?.planet || '';
}

periodScore(planet: string | null | undefined, companion?: string | null): number | null {
  return blendDashaScores(this.scoreOf(planet), companion ? this.scoreOf(companion) : null);
}

periodTone(planet: string | null | undefined, companion?: string | null): DashaPeriodTone {
  return dashaPeriodTone(this.periodScore(planet, companion));
}

isBestPeriod(planet: string | null | undefined, companion?: string | null): boolean {
  return isBestDashaPeriod(this.scoreOf(planet), companion ? this.scoreOf(companion) : null);
}

periodTitle(planet: string | null | undefined, companion?: string | null): string {
  if (companion && this.isBestPeriod(planet, companion)) {
    return this.translocoService.translate('mahadasha.toneBestTitle', {
      maha: this.scoreOf(companion),
      antar: this.scoreOf(planet)
    });
  }

  const label = this.toneLabel(this.periodTone(planet, companion));
  const score = this.periodScore(planet, companion);
  return score == null ? label : `${label} · ${score}`;
}

toneLabel(tone: DashaPeriodTone): string {
  const key = tone === 'favourable'
    ? 'mahadasha.toneFavourable'
    : tone === 'unfavourable'
      ? 'mahadasha.toneUnfavourable'
      : 'mahadasha.toneNeutral';
  const translated = this.translocoService.translate(key);
  return translated && translated !== key ? translated : tone;
}

private scoreOf(planet: string | null | undefined): number | null {
  const key = (planet ?? '').trim().toLowerCase();
  const value = key ? this.planetScores?.[key] : undefined;
  return typeof value === 'number' && !Number.isNaN(value) ? value : null;
}

getCurrentDashaSentence(currentDasha: { mahadasha: any; antardasha: any | null; remainingEnd: string | Date | null }): string {
  const maha = this.getTranslatedPlanetName(this.getPlanetName(currentDasha.mahadasha));
  const remaining = this.getRemainingText(currentDasha.remainingEnd);

  if (!currentDasha.antardasha) {
    return this.translocoService.translate('mahadasha.currentMahadashaSentence', {
      maha,
      remaining
    });
  }

  return this.translocoService.translate('mahadasha.currentDashaSentence', {
    maha,
    antar: this.getTranslatedPlanetName(this.getPlanetName(currentDasha.antardasha)),
    remaining
  });
}

getRemainingText(end: string | Date | null): string {
  return this.getDurationText(new Date(), end);
}

getPeriodStart(period: any): string | Date | null {
  return period?.startDate || period?.start || null;
}

getPeriodEnd(period: any): string | Date | null {
  return period?.endDate || period?.end || null;
}

getDurationText(start: string | Date | null, end: string | Date | null): string {
  if (!start || !end) {
    return '—';
  }

  const startDate = new Date(start);
  const endDate = new Date(end);

  if (Number.isNaN(startDate.getTime()) || Number.isNaN(endDate.getTime()) || endDate < startDate) {
    return '—';
  }

  let years = endDate.getFullYear() - startDate.getFullYear();
  let months = endDate.getMonth() - startDate.getMonth();

  if (endDate.getDate() < startDate.getDate()) {
    months--;
  }

  if (months < 0) {
    years--;
    months += 12;
  }

  const yearText = years > 0
    ? `${years} ${this.getDurationUnit(years, 'mahadasha.year', 'mahadasha.years', 'year', 'years')}`
    : '';

  const monthText = months > 0
    ? `${months} ${this.getDurationUnit(months, 'mahadasha.month', 'mahadasha.months', 'month', 'months')}`
    : '';

  return [yearText, monthText].filter(Boolean).join(' ')
    || `0 ${this.getDurationUnit(0, 'mahadasha.month', 'mahadasha.months', 'month', 'months')}`;
}

private getDurationUnit(
  count: number,
  singularKey: string,
  pluralKey: string,
  singularFallback: string,
  pluralFallback: string
): string {
  const key = count === 1 ? singularKey : pluralKey;
  const translated = this.translocoService.translate(key);
  return translated && translated !== key
    ? translated
    : count === 1 ? singularFallback : pluralFallback;
}

private getTranslatedPlanetName(planet: string): string {
  if (!planet) {
    return '-';
  }

  const key = `planet.${planet}`;
  const translated = this.translocoService.translate(key);
  return translated && translated !== key ? translated : planet;
}

private toDate(value: string | Date | null): Date | null {
  if (!value) {
    return null;
  }

  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

private startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}
}
