import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';

type MetadataValueGroup = 'element' | 'direction' | 'metal' | 'gemstone' | 'gana' | 'yoni' | 'nadi' | 'varna' | 'vashya' | 'dosha';

type InsightRow = {
  labelKey: string;
  value: string;
  valueGroup?: MetadataValueGroup;
  color?: string;
  icon?: string;
};

const NAKSHATRA_ORDER = [
  'Ashwini', 'Bharani', 'Krittika', 'Rohini', 'Mrigashira', 'Ardra', 'Punarvasu', 'Pushya', 'Ashlesha',
  'Magha', 'PurvaPhalguni', 'UttaraPhalguni', 'Hasta', 'Chitra', 'Swati', 'Vishakha', 'Anuradha', 'Jyeshtha',
  'Mula', 'PurvaAshadha', 'UttaraAshadha', 'Shravana', 'Dhanishta', 'Shatabhisha', 'PurvaBhadrapada', 'UttaraBhadrapada', 'Revati'
];

const NAKSHATRA_ALIASES: Record<string, number> = {
  ashwini: 0, asvini: 0, bharani: 1, krittika: 2, kritika: 2, rohini: 3,
  mrigashira: 4, mrigasira: 4, mrigashirsha: 4, ardra: 5, arudra: 5,
  punarvasu: 6, pushya: 7, pusya: 7, ashlesha: 8, aslesha: 8, magha: 9,
  purvaphalguni: 10, poorvaphalguni: 10, pubba: 10,
  uttaraphalguni: 11, uttaraphalguni1: 11, hasta: 12, chitra: 13,
  swati: 14, svati: 14, vishakha: 15, visakha: 15, anuradha: 16,
  jyeshtha: 17, jyestha: 17, moola: 18, mula: 18,
  purvashada: 19, poorvashada: 19, purvaashadha: 19, poorvaashadha: 19,
  uttarashada: 20, uttaraashadha: 20, shravana: 21, sravana: 21,
  dhanishta: 22, dhanishtha: 22, shatabhisha: 23, satabhisha: 23,
  purvabhadrapada: 24, poorvabhadrapada: 24, uttarabhadrapada: 25, revati: 26
};

const SIGN_ALIASES: Record<string, string> = {
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

// Same rows as Data/Rashi/*.json ghatChakra. Used when the chart payload has no ghat block yet.
const GHAT_BY_SIGN: Record<string, {
  month: string; tithi: string; tithiNumbers: string; weekday: string;
  nakshatra: string; yoga: string; karana: string; moonMale: string; moonFemale: string;
}> = {
  Aries: { month: 'kartik', tithi: 'nanda', tithiNumbers: '1, 6, 11', weekday: 'sunday', nakshatra: 'Magha', yoga: 'vishkambha', karana: 'bava', moonMale: 'Aries', moonFemale: 'Aries' },
  Taurus: { month: 'margashirsha', tithi: 'purna', tithiNumbers: '5, 10, 15', weekday: 'saturday', nakshatra: 'Hasta', yoga: 'shukla', karana: 'shakuni', moonMale: 'Virgo', moonFemale: 'Sagittarius' },
  Gemini: { month: 'ashadha', tithi: 'bhadra', tithiNumbers: '2, 7, 12', weekday: 'monday', nakshatra: 'Swati', yoga: 'parigha', karana: 'kaulava', moonMale: 'Aquarius', moonFemale: 'Sagittarius' },
  Cancer: { month: 'pausha', tithi: 'bhadra', tithiNumbers: '2, 7, 12', weekday: 'wednesday', nakshatra: 'Anuradha', yoga: 'vyaghata', karana: 'naga', moonMale: 'Leo', moonFemale: 'Pisces' },
  Leo: { month: 'jyeshtha', tithi: 'jaya', tithiNumbers: '3, 8, 13', weekday: 'saturday', nakshatra: 'Mula', yoga: 'dhriti', karana: 'bava', moonMale: 'Capricorn', moonFemale: 'Scorpio' },
  Virgo: { month: 'bhadrapada', tithi: 'purna', tithiNumbers: '5, 10, 15', weekday: 'saturday', nakshatra: 'Shravana', yoga: 'shukla', karana: 'kaulava', moonMale: 'Gemini', moonFemale: 'Scorpio' },
  Libra: { month: 'magha', tithi: 'rikta', tithiNumbers: '4, 9, 14', weekday: 'thursday', nakshatra: 'Shatabhisha', yoga: 'shukla', karana: 'taitila', moonMale: 'Sagittarius', moonFemale: 'Pisces' },
  Scorpio: { month: 'ashwin', tithi: 'nanda', tithiNumbers: '1, 6, 11', weekday: 'friday', nakshatra: 'Revati', yoga: 'vyatipata', karana: 'gara', moonMale: 'Taurus', moonFemale: 'Sagittarius' },
  Sagittarius: { month: 'shravana', tithi: 'jaya', tithiNumbers: '3, 8, 13', weekday: 'friday', nakshatra: 'Bharani', yoga: 'vajra', karana: 'taitila', moonMale: 'Pisces', moonFemale: 'Virgo' },
  Capricorn: { month: 'vaishakha', tithi: 'rikta', tithiNumbers: '4, 9, 14', weekday: 'tuesday', nakshatra: 'Rohini', yoga: 'vaidhriti', karana: 'shakuni', moonMale: 'Leo', moonFemale: 'Scorpio' },
  Aquarius: { month: 'chaitra', tithi: 'jaya', tithiNumbers: '3, 8, 13', weekday: 'thursday', nakshatra: 'Ardra', yoga: 'ganda', karana: 'kimstughna', moonMale: 'Sagittarius', moonFemale: 'Gemini' },
  Pisces: { month: 'phalguna', tithi: 'purna', tithiNumbers: '5, 10, 15', weekday: 'friday', nakshatra: 'Ashlesha', yoga: 'vajra', karana: 'chatushpada', moonMale: 'Aquarius', moonFemale: 'Aquarius' }
};

@Component({
  selector: 'app-kundali-metadata-insights',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './kundali-metadata-insights.component.html',
  styleUrls: ['./kundali-metadata-insights.component.scss']
})
export class KundaliMetadataInsightsComponent {
  @Input() rashiInsight: any;
  @Input() nakshatraInsight: any;
  @Input() gender = '';

  private readonly planetGlyphMap: Record<string, string> = {
    Sun: '\u2609',
    Moon: '\u263E',
    Mars: '\u2642',
    Mercury: '\u263F',
    Jupiter: '\u2643',
    Venus: '\u2640',
    Saturn: '\u2644',
    Rahu: '\u260A',
    Ketu: '\u260B'
  };

  constructor(private translocoService: TranslocoService, private readonly apiTerms: ApiTermTranslationService) {}

  get rashiRows(): InsightRow[] {
    const item = this.rashiInsight;

    if (!item) {
      return [];
    }

    return [
      { labelKey: 'metadata.ruler', value: this.apiTerms.translatePlanet(item.ruler), icon: this.getPlanetGlyph(item.ruler) },
      { labelKey: 'metadata.element', value: item.element, valueGroup: 'element' },
      { labelKey: 'metadata.strength', value: item.strength },
      { labelKey: 'metadata.weakness', value: item.weakness },
      { labelKey: 'metadata.gemstone', value: item.gemstone, valueGroup: 'gemstone' },
      { labelKey: 'metadata.metal', value: item.metal, valueGroup: 'metal' },
      { labelKey: 'metadata.color', value: item.color, color: item.color },
      { labelKey: 'metadata.luckyNumbers', value: this.joinValues(item.luckyNumbers) },
      { labelKey: 'metadata.favorableDay', value: item.favorableDay },
      { labelKey: 'metadata.favorableDirection', value: item.favorableDirection, valueGroup: 'direction' }
    ];
  }

  get nakshatraRows(): InsightRow[] {
    const item = this.nakshatraInsight;

    if (!item) {
      return [];
    }

    return [
      { labelKey: 'metadata.lord', value: this.apiTerms.translatePlanet(item.lord), icon: this.getPlanetGlyph(item.lord) },
      { labelKey: 'metadata.symbol', value: item.symbol },
      { labelKey: 'metadata.gana', value: item.gana, valueGroup: 'gana' },
      { labelKey: 'metadata.yoni', value: item.yoni, valueGroup: 'yoni' },
      { labelKey: 'metadata.nadi', value: item.nadi, valueGroup: 'nadi' },
      { labelKey: 'metadata.varna', value: item.varna, valueGroup: 'varna' },
      { labelKey: 'metadata.vashya', value: item.vashya, valueGroup: 'vashya' },
      { labelKey: 'metadata.dosha', value: item.dosha, valueGroup: 'dosha' },
      { labelKey: 'metadata.fastingDay', value: item.fastingDay },
      { labelKey: 'metadata.businessSuitability', value: item.businessSuitability }
    ];
  }

  get ghatFacts(): InsightRow[] {
    const ghat = this.rashiInsight?.ghatChakra ?? this.rashiInsight?.GhatChakra ?? this.ghatFromBirthRashi();
    if (!ghat) {
      return [];
    }

    const tithi = this.ghatValue(ghat, 'tithi', 'Tithi');
    const tithiName = this.translocoService.translate(`metadata.ghat.tithi.${tithi}`);
    const moonMale = this.apiTerms.translateSign(this.ghatValue(ghat, 'moonMale', 'MoonMale'));
    const moonFemale = this.apiTerms.translateSign(this.ghatValue(ghat, 'moonFemale', 'MoonFemale'));
    const moon = this.isFemale()
      ? moonFemale
      : this.isMale() || moonMale === moonFemale
        ? moonMale
        : `${moonMale} · ${moonFemale}`;

    return [
      { labelKey: 'metadata.ghat.weekday', value: this.apiTerms.translatePanchangValue('vaar', this.ghatValue(ghat, 'weekday', 'Weekday')) },
      { labelKey: 'metadata.ghat.tithi', value: `${tithiName} ${this.ghatValue(ghat, 'tithiNumbers', 'TithiNumbers')}`.trim() },
      { labelKey: 'metadata.ghat.nakshatra', value: this.apiTerms.translateNakshatra(this.ghatValue(ghat, 'nakshatra', 'Nakshatra')) },
      { labelKey: 'metadata.ghat.month', value: this.apiTerms.translatePanchangValue('maas', this.ghatValue(ghat, 'month', 'Month')) },
      { labelKey: 'metadata.ghat.moon', value: moon },
      { labelKey: 'metadata.ghat.yoga', value: this.apiTerms.translatePanchangValue('yoga', this.ghatValue(ghat, 'yoga', 'Yoga')) },
      { labelKey: 'metadata.ghat.karana', value: this.apiTerms.translatePanchangValue('karana', this.ghatValue(ghat, 'karana', 'Karana')) }
    ];
  }

  get taraGroups(): Array<{ labelKey: string; stars: string }> {
    const index = this.nakshatraIndex(this.nakshatraInsight?.name ?? this.nakshatraInsight?.Name);
    if (index < 0) {
      return [];
    }

    const starsAt = (offset: number) => [0, 9, 18]
      .map(step => this.apiTerms.translateNakshatra(NAKSHATRA_ORDER[(index + offset + step) % 27]))
      .join(', ');

    return [
      { labelKey: 'metadata.tara.vipat', stars: starsAt(2) },
      { labelKey: 'metadata.tara.pratyari', stars: starsAt(4) },
      { labelKey: 'metadata.tara.vadha', stars: starsAt(6) }
    ];
  }

  get rashiTitle(): string {
    const name = this.rashiInsight?.englishName || this.rashiInsight?.name;
    const symbol = this.rashiInsight?.symbol;

    return [symbol, this.apiTerms.translateSign(name)].filter(Boolean).join(' ');
  }

  get nakshatraTitle(): string {
    const name = this.nakshatraInsight?.name;
    const pada = this.nakshatraInsight?.pada;

    const translatedName = this.apiTerms.translateNakshatra(name);
    return pada ? `${translatedName} ${this.translocoService.translate('janmaStory.padaLabel')} ${pada}` : translatedName;
  }

  displayValue(row: InsightRow): string {
    if (!row.value) {
      return '-';
    }

    if (!row.valueGroup) {
      return row.value;
    }

    return this.apiTerms.translateMetadataValue(row.valueGroup, row.value);
  }

  private joinValues(value: any): string {
    if (Array.isArray(value)) {
      return value.join(', ');
    }

    return value?.toString() ?? '';
  }

  private getPlanetGlyph(planet: string | null | undefined): string {
    return this.planetGlyphMap[planet ?? ''] ?? '\u2022';
  }

  private ghatFromBirthRashi(): (typeof GHAT_BY_SIGN)[string] | undefined {
    const sign = this.canonicalSign(this.rashiInsight?.englishName || this.rashiInsight?.name || this.rashiInsight?.EnglishName || this.rashiInsight?.Name);
    return sign ? GHAT_BY_SIGN[sign] : undefined;
  }

  private canonicalSign(value: unknown): string {
    const key = String(value ?? '').toLowerCase().replace(/[^a-z]/g, '');
    return SIGN_ALIASES[key] ?? '';
  }

  private ghatValue(ghat: any, camel: string, pascal: string): string {
    return String(ghat?.[camel] ?? ghat?.[pascal] ?? '');
  }

  private nakshatraIndex(value: unknown): number {
    const key = String(value ?? '').toLowerCase().replace(/[^a-z0-9]/g, '');
    return NAKSHATRA_ALIASES[key] ?? -1;
  }

  private isFemale(): boolean {
    return String(this.gender ?? '').trim().toLowerCase().startsWith('f');
  }

  private isMale(): boolean {
    return String(this.gender ?? '').trim().toLowerCase().startsWith('m');
  }
}
