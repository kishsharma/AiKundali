import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../../core/services/api-term-translation.service';

type StrengthView = 'positions' | 'shadbala' | 'summary' | 'ashtakavarga';

@Component({
  selector: 'app-consultation-planets',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './consultation-planets.section.html',
  styleUrl: './consultation-planets.section.scss'
})
export class ConsultationPlanetsSection {
  private readonly apiTerms = inject(ApiTermTranslationService);
  private readonly transloco = inject(TranslocoService);

  @Input() planets: any[] = [];
  @Input() strengthTablesRaw: any = null;
  @Input() activeStrengthView: StrengthView = 'positions';

  private readonly planetGlyphMap: Record<string, string> = {
    Sun: '\u2609',
    Moon: '\u263E',
    Mars: '\u2642',
    Mercury: '\u263F',
    Jupiter: '\u2643',
    Venus: '\u2640',
    Saturn: '\u2644',
    Rahu: 'Ra',
    Ketu: 'Ke'
  };

  get strengthTables(): {
    planetaryStrengths: any[];
    shadbala: any[];
    houseSupport: any[];
  } | null {
    const raw = this.strengthTablesRaw;
    if (!raw) {
      return null;
    }

    return {
      planetaryStrengths: this.asRows(raw.planetaryStrengths ?? raw.PlanetaryStrengths),
      shadbala: this.resolveShadbalaRows(raw),
      houseSupport: this.asRows(raw.houseSupport ?? raw.HouseSupport)
    };
  }

  getPlanetGlyph(planet: string | null | undefined): string {
    return this.planetGlyphMap[planet ?? ''] ?? '\u2022';
  }

  getPlanetSupportClass(planet: any): Record<string, boolean> {
    const normalized = this.normalizeStrength(planet?.detailedStrength).toLowerCase();
    const isChallenging =
      planet?.isCombust ||
      normalized.includes('weak') ||
      normalized.includes('enemy') ||
      normalized.includes('debilitated');
    const isSupportive =
      !isChallenging && (normalized.includes('own') || normalized.includes('exalted'));

    return {
      supportive: isSupportive,
      neutral: !isSupportive && !isChallenging,
      challenging: !!isChallenging
    };
  }

  hasPlanetAspects(planet: any): boolean {
    return this.normalizePlanetNameList(planet?.aspectedBy).length > 0;
  }

  getAspectedByText(planet: any): string {
    const aspects = this.normalizePlanetNameList(planet?.aspectedBy);
    if (!aspects.length) {
      return this.transloco.translate('common.none');
    }
    return aspects.map(name => this.translatePlanetName(name)).join(', ');
  }

  getStrengthLabel(value: string | null | undefined): string {
    const normalized = this.normalizeStrength(value);
    const key = `strength.${normalized}`;
    const translated = this.transloco.translate(key);
    if (translated && translated !== key) {
      return translated;
    }
    return normalized.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/\s+/g, ' ');
  }

  getStrengthClass(value: string | null | undefined): Record<string, boolean> {
    const normalized = this.normalizeStrength(value).toLowerCase();
    const isWeak =
      normalized.includes('weak') ||
      normalized.includes('enemy') ||
      normalized.includes('debilitated') ||
      normalized.includes('poor') ||
      normalized.includes('low');
    const isStrong =
      !isWeak &&
      (normalized.includes('own') ||
        normalized.includes('exalted') ||
        normalized.includes('strong') ||
        normalized.includes('high') ||
        normalized.includes('good') ||
        normalized.includes('excellent'));

    return {
      strong: isStrong,
      weak: isWeak,
      neutral:
        !isStrong &&
        !isWeak &&
        (normalized.includes('neutral') ||
          normalized.includes('notapplicable') ||
          normalized.includes('moderate') ||
          normalized.includes('medium') ||
          normalized.includes('average'))
    };
  }

  getStrengthToneColor(value: string | null | undefined): string | null {
    const normalized = (value ?? '').toLowerCase();
    if (!normalized) {
      return null;
    }
    if (['weak', 'low', 'poor', 'enemy', 'debilitated'].some(token => normalized.includes(token))) {
      return 'var(--theme-danger)';
    }
    if (['strong', 'high', 'good', 'excellent', 'own', 'exalted'].some(token => normalized.includes(token))) {
      return 'var(--theme-success)';
    }
    if (['moderate', 'medium', 'average', 'neutral'].some(token => normalized.includes(token))) {
      return 'var(--theme-warning)';
    }
    return null;
  }

  formatBalaCell(value: number | string | null | undefined): string {
    if (value == null || value === '') {
      return '—';
    }
    if (typeof value === 'string') {
      const trimmed = value.trim();
      if (!trimmed) {
        return '—';
      }
      if (!/^-?\d+(\.\d+)?$/.test(trimmed)) {
        return trimmed;
      }
      return Number(trimmed).toFixed(1);
    }
    const numeric = Number(value);
    return Number.isFinite(numeric) ? numeric.toFixed(1) : '—';
  }

  formatShadbalaPercent(row: any): string {
    const value = row?.percent ?? row?.Percent;
    return value == null || value === '' ? '—' : `${value}%`;
  }

  translatePlanetName(planet: unknown): string {
    return this.apiTerms.translatePlanet(planet);
  }

  translateSignName(sign: unknown): string {
    return this.apiTerms.translateSign(sign);
  }

  translateNakshatraName(nakshatra: unknown): string {
    return this.apiTerms.translateNakshatra(nakshatra);
  }

  translateHouseName(houseNumber: unknown, fallback?: string): string {
    const number = Number(houseNumber);
    if (!Number.isInteger(number) || number < 1 || number > 12) {
      return fallback || '';
    }
    const key = `house.name.${number}`;
    const translated = this.transloco.translate(key);
    return translated && translated !== key ? translated : fallback || '';
  }

  translateControlledValue(group: 'strength' | 'severity' | 'tone' | 'activationLevel', value: unknown): string {
    return this.apiTerms.translateControlledValue(group, value);
  }

  private resolveShadbalaRows(raw: any): any[] {
    const virupaRows = this.asRows(raw.shadbala ?? raw.Shadbala ?? raw.shadBala ?? raw.ShadBala);
    if (virupaRows.length) {
      return virupaRows;
    }

    return this.asRows(raw.planetaryStrengths ?? raw.PlanetaryStrengths).filter((row: any) => {
      const planet = String(row?.planet ?? row?.Planet ?? '')
        .trim()
        .toLowerCase();
      return planet && planet !== 'rahu' && planet !== 'ketu';
    });
  }

  private asRows(value: unknown): any[] {
    return Array.isArray(value) ? value : [];
  }

  private normalizeStrength(value: string | null | undefined): string {
    if (!value) {
      return 'NotApplicable';
    }
    const normalized = value.replace(/^strength\./, '').trim();
    return normalized === 'Own Sign' ? 'OwnSign' : normalized;
  }

  private normalizePlanetNameList(value: any): string[] {
    if (!value) {
      return [];
    }
    if (Array.isArray(value)) {
      return value.map(item => String(item ?? '').trim()).filter(Boolean);
    }
    return String(value)
      .split(',')
      .map(item => item.trim())
      .filter(Boolean);
  }
}
