import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { HoraverseInsightService } from './horaverse-insight.service';
import { HoraverseScene, HoraverseSession } from './horaverse-models';

@Injectable({ providedIn: 'root' })
export class HoraverseStoryService {
  constructor(
    private readonly translocoService: TranslocoService,
    private readonly termTranslation: ApiTermTranslationService,
    private readonly insight: HoraverseInsightService
  ) {}

  buildScenes(session: HoraverseSession): HoraverseScene[] {
    const report = session.report ?? {};
    const summary = report.summary ?? {};
    const planets = report.planets ?? [];
    const scores = report.scores ?? {};
    const houses = this.insight.readHouses(session);
    const currentDasha = this.insight.readCurrentDasha(session) ?? report.currentDasha ?? {};
    const birth = session.birthProfile;
    const panchang = report.panchang ?? {};
    const moon = this.findPlanet(planets, 'Moon');
    const saturn = this.findPlanet(planets, 'Saturn');
    const moonHouse = this.insight.findHouse(houses, moon?.house);
    const moonSign = this.translateSign(moon?.sign ?? summary.moonSign);
    const ascendant = this.translateSign(summary.ascendant);
    const dashaMaha = this.translatePlanet(currentDasha.mahadasha ?? summary.currentMahadasha) || this.t('horaverse.kaala.stillUnfolding');
    const dashaAntar = this.translatePlanet(currentDasha.antardasha ?? summary.currentAntardasha) || this.t('horaverse.kaala.smallerCurrent');
    const strongestHouse = [...houses].sort((a, b) => (b.score ?? 0) - (a.score ?? 0))[0];
    const pressuredHouse = [...houses].sort((a, b) => Number(b.hasMaleficAspect) - Number(a.hasMaleficAspect) || (a.score ?? 50) - (b.score ?? 50))[0];

    return [
      {
        kind: 'awakening',
        kicker: this.t('horaverse.awakening.kicker'),
        title: this.t('horaverse.awakening.title', {
          name: birth.name || this.t('horaverse.awakening.thisChart')
        }),
        accent: '#f7bf63',
        lines: [
          this.t('horaverse.awakening.line1', {
            name: birth.name || this.t('horaverse.awakening.thisChart'),
            date: this.formatBirthDate(birth.birthDate),
            time: birth.birthTime || this.t('horaverse.awakening.preciseMoment')
          }),
          this.t('horaverse.awakening.line2', {
            place: birth.birthPlace || this.t('horaverse.awakening.onePlace'),
            latitude: this.formatCoordinate(birth.latitude, this.t('horaverse.awakening.latitude')),
            longitude: this.formatCoordinate(birth.longitude, this.t('horaverse.awakening.longitude'))
          }),
          this.t('horaverse.awakening.line3', {
            panchang: this.formatPanchangDetails(panchang, moon)
          })
        ]
      },
      {
        kind: 'solar-ring',
        kicker: this.t('horaverse.solarRing.kicker'),
        title: this.t('horaverse.solarRing.title', { ascendant: ascendant || this.t('horaverse.solarRing.ascendantFallback') }),
        accent: '#54f0d7',
        lines: [
          this.t('horaverse.solarRing.line1'),
          this.t('horaverse.solarRing.line2', {
            sign: moonSign || this.t('horaverse.solarRing.signFallback'),
            house: moon?.house ?? this.t('horaverse.common.unknown'),
            houseMeaning: this.houseMeaning(moon?.house)
          }),
          this.t('horaverse.solarRing.line3', {
            overall: scores.overall ?? this.t('horaverse.common.a'),
            growth: scores.growth ?? this.t('horaverse.common.unknown'),
            challenge: scores.challenge ?? this.t('horaverse.common.unknown'),
            direction: this.scoreDirection(scores)
          })
        ]
      },
      {
        kind: 'house-walk',
        kicker: this.t('horaverse.houseWalk.kicker'),
        title: this.t('horaverse.houseWalk.title'),
        accent: '#62e9ff',
        focusPlanet: 'Moon',
        focusHouse: Number(moon?.house) || 1,
        lines: [
          this.t('horaverse.houseWalk.line1'),
          this.t('horaverse.houseWalk.line2', {
            house: moonHouse?.house ?? moon?.house ?? this.t('horaverse.common.unknown'),
            meaning: this.houseMeaning(moonHouse?.house ?? moon?.house),
            occupants: this.formatPlanetList(moonHouse?.occupants) || this.t('horaverse.hud.emptyOccupants')
          }),
          this.t('horaverse.houseWalk.line3', {
            house: strongestHouse?.house ?? this.t('horaverse.common.unknown'),
            support: this.houseAspectLabel(strongestHouse),
            pressureHouse: pressuredHouse?.house ?? this.t('horaverse.common.unknown'),
            pressure: this.houseAspectLabel(pressuredHouse)
          }),
          saturn
            ? this.t('horaverse.kaala.saturnLine', {
                house: saturn.house ?? this.t('horaverse.common.unknown'),
                strength: this.cleanStrength(saturn.detailedStrength)
              })
            : this.t('horaverse.kaala.saturnQuiet')
        ]
      },
      {
        kind: 'kaala',
        kicker: this.t('horaverse.kaala.kicker'),
        title: this.t('horaverse.kaala.title'),
        accent: '#c890ff',
        lines: [
          this.t('horaverse.kaala.line1'),
          this.t('horaverse.kaala.line2', {
            mahadasha: dashaMaha
          }),
          this.t('horaverse.kaala.line3', {
            antardasha: dashaAntar
          })
        ]
      },
      {
        kind: 'door-ahead',
        kicker: this.t('horaverse.doorAhead.kicker'),
        title: this.t('horaverse.doorAhead.title'),
        accent: '#67f5bf',
        lines: this.buildDoorAheadLines(session, scores, currentDasha)
      }
    ];
  }

  private houseMeaning(house: number | string | null | undefined): string {
    const houseNumber = Number(house);
    if (!Number.isFinite(houseNumber)) {
      return this.t('horaverse.houseMeaning.unknown');
    }

    return this.t(`horaverse.houseMeaning.${houseNumber}`);
  }

  private scoreDirection(scores: any): string {
    const growth = Number(scores?.growth ?? 0);
    const challenge = Number(scores?.challenge ?? 0);

    if (growth >= challenge + 15) {
      return this.t('horaverse.solarRing.directionGrowth');
    }

    if (challenge >= growth + 15) {
      return this.t('horaverse.solarRing.directionChallenge');
    }

    return this.t('horaverse.solarRing.directionBalanced');
  }
  private buildDoorAheadLines(session: HoraverseSession, scores: any, currentDasha: any): string[] {
    const strengths = this.toList(scores.dominantStrengths);
    const weaknesses = this.toList(scores.dominantWeaknesses);
    const factors = this.toList(scores.factors ?? scores.growthFactors ?? scores.challengeFactors);
    const dashaWindow = this.formatWindow(
      currentDasha?.mahaStart ?? currentDasha?.startDate,
      currentDasha?.mahaEnd ?? currentDasha?.endDate
    );

    return [
      strengths[0]
        ? this.t('horaverse.doorAhead.strongestSupport', { value: strengths[0] })
        : this.t('horaverse.doorAhead.supportiveWindow'),
      weaknesses[0]
        ? this.t('horaverse.doorAhead.frictionPoint', { value: weaknesses[0] })
        : this.t('horaverse.doorAhead.difficultPart'),
      factors[0]
        ? this.t('horaverse.doorAhead.repeatingClue', { value: factors[0] })
        : this.t('horaverse.doorAhead.currentPeriod', {
            period: this.translatePlanet(currentDasha?.mahadasha ?? session.report?.summary?.currentMahadasha) || this.t('horaverse.doorAhead.currentFallback')
          }),
      dashaWindow ? this.t('horaverse.doorAhead.cycleActive', { window: dashaWindow }) : this.t('horaverse.doorAhead.nextDoor')
    ];
  }

  private findPlanet(planets: any[], planetName: string): any | null {
    return planets.find(item => String(item?.planet ?? item?.planetName ?? '').toLowerCase() === planetName.toLowerCase()) ?? null;
  }

  private formatPlanetList(values: string[] | undefined): string {
    return (values ?? []).map(value => this.translatePlanet(value)).filter(Boolean).join(', ');
  }

  private houseAspectLabel(house: { hasBeneficAspect?: boolean; hasMaleficAspect?: boolean } | null | undefined): string {
    if (house?.hasBeneficAspect && house?.hasMaleficAspect) {
      return this.t('horaverse.hud.mixed');
    }
    if (house?.hasBeneficAspect) {
      return this.t('horaverse.hud.beneficAspect');
    }
    if (house?.hasMaleficAspect) {
      return this.t('horaverse.hud.maleficAspect');
    }
    return this.t('horaverse.hud.neutral');
  }

  private toList(value: any): string[] {
    return Array.isArray(value)
      ? value.map(item => String(item).trim()).filter(Boolean)
      : [];
  }

  private cleanStrength(value: any): string {
    const strength = String(value ?? this.t('horaverse.common.quiet'));
    return strength.replace(/^strength\./, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private formatBirthDate(value: string | Date | null | undefined): string {
    if (!value) {
      return this.t('horaverse.awakening.birthDateFallback');
    }

    return this.formatDate(value);
  }

  private formatCoordinate(value: string | number | null | undefined, label: string): string {
    const numericValue = Number(value);

    if (!Number.isFinite(numericValue)) {
      return `${label} ${this.t('horaverse.common.unknown')}`;
    }

    return `${label} ${numericValue.toFixed(4)}`;
  }

  private formatPanchangDetails(panchang: any, moon: any): string {
    const nakshatra = this.translateNakshatra(panchang?.nakshatra ?? moon?.nakshatra) || this.t('horaverse.common.unknown');
    const pada = panchang?.pada ?? moon?.pada;
    const maas = this.cleanText(panchang?.maas);
    const paksha = this.cleanText(panchang?.paksha);
    const tithi = this.cleanText(panchang?.tithi);
    const yoga = this.cleanText(panchang?.yoga);
    const karana = this.cleanText(panchang?.karana);
    const vara = this.cleanText(panchang?.vaar ?? panchang?.vara);
    const parts = [
      maas ? this.t('horaverse.awakening.panchangMaas', { maas, paksha }) : '',
      tithi ? this.t('horaverse.awakening.panchangTithi', { tithi }) : '',
      this.t('horaverse.awakening.panchangNakshatra', {
        nakshatra,
        pada: pada ?? this.t('horaverse.common.unknown')
      }),
      yoga ? this.t('horaverse.awakening.panchangYoga', { yoga }) : '',
      karana ? this.t('horaverse.awakening.panchangKarana', { karana }) : '',
      vara ? this.t('horaverse.awakening.panchangVara', { vara }) : ''
    ].filter(Boolean);

    return parts.join(this.t('horaverse.awakening.detailSeparator'));
  }

  private cleanText(value: unknown): string {
    return String(value ?? '').trim();
  }
  private formatWindow(start: string | Date | null | undefined, end: string | Date | null | undefined): string {
    if (!start || !end) {
      return '';
    }

    return `${this.formatDate(start)} ${this.t('horaverse.common.to')} ${this.formatDate(end)}`;
  }

  private formatDate(value: string | Date): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return date.toLocaleDateString(this.localeForActiveLang(), {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  private translatePlanet(value: unknown): string {
    return this.termTranslation.translatePlanet(value);
  }

  private translateSign(value: unknown): string {
    return this.termTranslation.translateSign(value);
  }

  private translateNakshatra(value: unknown): string {
    return this.termTranslation.translateNakshatra(value);
  }
  private t(key: string, params?: Record<string, unknown>): string {
    const exactValue = this.lookupExactTranslation(key);
    if (exactValue !== undefined) {
      return this.interpolate(exactValue, params);
    }

    if (key.startsWith('horaverse.')) {
      return '';
    }

    return this.translocoService.translate(key, params);
  }

  private lookupExactTranslation(key: string): string | undefined {
    try {
      const lang = this.translocoService.getActiveLang();
      const translations = this.translocoService.getTranslation(lang) as Record<string, unknown>;
      const value = translations?.[key];
      return typeof value === 'string' ? value : undefined;
    } catch {
      return undefined;
    }
  }

  private interpolate(value: string, params?: Record<string, unknown>): string {
    if (!params) {
      return value;
    }

    return value.replace(/{{\s*(\w+)\s*}}/g, (_, paramKey: string) => String(params[paramKey] ?? ''));
  }

  private localeForActiveLang(): string {
    const lang = this.translocoService.getActiveLang();
    return lang === 'hi' ? 'hi-IN' : lang === 'kn' ? 'kn-IN' : 'en-GB';
  }
}
