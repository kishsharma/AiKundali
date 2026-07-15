import { Injectable } from '@angular/core';
import { TranslocoService } from '@jsverse/transloco';

type TermGroup = 'planet' | 'sign' | 'nakshatra';

@Injectable({ providedIn: 'root' })
export class ApiTermTranslationService {
  private readonly planets: Record<string, string> = {
    sun: 'Sun', surya: 'Sun', moon: 'Moon', chandra: 'Moon', mars: 'Mars', mangal: 'Mars', kuja: 'Mars',
    mercury: 'Mercury', budha: 'Mercury', jupiter: 'Jupiter', guru: 'Jupiter', brihaspati: 'Jupiter',
    venus: 'Venus', shukra: 'Venus', sukra: 'Venus', saturn: 'Saturn', shani: 'Saturn', rahu: 'Rahu', ketu: 'Ketu',
    ascendant: 'Ascendant', lagna: 'Ascendant', mahadasha: 'Mahadasha'
  };

  private readonly signs: Record<string, string> = {
    aries: 'Aries', mesha: 'Aries', mesh: 'Aries',
    taurus: 'Taurus', vrishabha: 'Taurus', vrishabh: 'Taurus',
    gemini: 'Gemini', mithuna: 'Gemini', mithun: 'Gemini',
    cancer: 'Cancer', karka: 'Cancer', karkataka: 'Cancer',
    leo: 'Leo', simha: 'Leo', sinh: 'Leo',
    virgo: 'Virgo', kanya: 'Virgo',
    libra: 'Libra', tula: 'Libra',
    scorpio: 'Scorpio', vrischika: 'Scorpio', vrishchika: 'Scorpio',
    sagittarius: 'Sagittarius', dhanu: 'Sagittarius', dhanus: 'Sagittarius',
    capricorn: 'Capricorn', makara: 'Capricorn', makar: 'Capricorn',
    aquarius: 'Aquarius', kumbha: 'Aquarius', kumbh: 'Aquarius',
    pisces: 'Pisces', meena: 'Pisces', meen: 'Pisces'
  };

  private readonly signLords: Record<string, string> = {
    Aries: 'Mars',
    Taurus: 'Venus',
    Gemini: 'Mercury',
    Cancer: 'Moon',
    Leo: 'Sun',
    Virgo: 'Mercury',
    Libra: 'Venus',
    Scorpio: 'Mars',
    Sagittarius: 'Jupiter',
    Capricorn: 'Saturn',
    Aquarius: 'Saturn',
    Pisces: 'Jupiter'
  };

  private readonly nakshatras: Record<string, string> = {
    ashwini: 'Ashwini', asvini: 'Ashwini', bharani: 'Bharani', krittika: 'Krittika', kritika: 'Krittika',
    rohini: 'Rohini', mrigashira: 'Mrigashira', mrigasira: 'Mrigashira', ardra: 'Ardra', arudra: 'Ardra',
    punarvasu: 'Punarvasu', pushya: 'Pushya', pusya: 'Pushya', ashlesha: 'Ashlesha', aslesha: 'Ashlesha',
    magha: 'Magha', purvaphalguni: 'PurvaPhalguni', poorvaphalguni: 'PurvaPhalguni', pubba: 'PurvaPhalguni',
    uttaraphalguni: 'UttaraPhalguni', uttaraphalguni1: 'UttaraPhalguni', hasta: 'Hasta', chitra: 'Chitra',
    swati: 'Swati', svati: 'Swati', vishakha: 'Vishakha', visakha: 'Vishakha', anuradha: 'Anuradha',
    jyeshtha: 'Jyeshtha', jyestha: 'Jyeshtha', moola: 'Moola', mula: 'Moola',
    purvashada: 'PurvaAshadha', poorvashada: 'PurvaAshadha', purvaashadha: 'PurvaAshadha', poorvaashadha: 'PurvaAshadha',
    uttarashada: 'UttaraAshadha', uttaraashadha: 'UttaraAshadha', shravana: 'Shravana', sravana: 'Shravana',
    dhanishta: 'Dhanishta', dhanishtha: 'Dhanishta', shatabhisha: 'Shatabhisha', satabhisha: 'Shatabhisha',
    purvabhadrapada: 'PurvaBhadrapada', poorvabhadrapada: 'PurvaBhadrapada',
    uttarabhadrapada: 'UttaraBhadrapada', revati: 'Revati'
  };

  constructor(private readonly translocoService: TranslocoService) {}

  translate(group: TermGroup, value: unknown): string {
    const text = this.cleanValue(value);
    if (!text) {
      return '';
    }

    const canonical = this.resolveCanonical(group, text);
    const key = canonical ? `${group}.${canonical}` : `${group}.${text}`;
    const translated = this.translocoService.translate(key);
    return translated && translated !== key ? translated : text;
  }

  translatePlanet(value: unknown): string {
    return this.translate('planet', value);
  }

  translateSign(value: unknown): string {
    return this.translate('sign', value);
  }

  translateNakshatra(value: unknown): string {
    return this.translate('nakshatra', value);
  }

  translateSignLord(value: unknown): string {
    const text = this.cleanValue(value);
    if (!text) {
      return '';
    }

    const canonicalSign = this.resolveCanonical('sign', text);
    const lord = canonicalSign ? this.signLords[canonicalSign] : undefined;
    return lord ? this.translatePlanet(lord) : '';
  }

  private resolveCanonical(group: TermGroup, value: string): string | undefined {
    const normalized = this.normalize(value);
    const source = group === 'planet' ? this.planets : group === 'sign' ? this.signs : this.nakshatras;
    return source[normalized];
  }

  private cleanValue(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }

    return String(value)
      .replace(/^\d+\s*[-.:)]\s*/, '')
      .trim();
  }

  private normalize(value: string): string {
    return value.toLowerCase().replace(/[^a-z0-9]/g, '');
  }
}