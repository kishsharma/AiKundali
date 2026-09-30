import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';

const PADA_AKSHARA: Record<string, string[]> = {
  Ashwini: ['चू', 'चे', 'चो', 'ला'],
  Bharani: ['ली', 'लू', 'ले', 'लो'],
  Krittika: ['अ', 'ई', 'उ', 'ए'],
  Rohini: ['ओ', 'वा', 'वी', 'वू'],
  Mrigashira: ['वे', 'वो', 'का', 'की'],
  Ardra: ['कु', 'घ', 'ङ', 'च'],
  Punarvasu: ['के', 'को', 'हा', 'ही'],
  Pushya: ['हु', 'हे', 'हो', 'ड'],
  Ashlesha: ['डि', 'डु,डू', 'डे', 'डो'],
  Magha: ['मा', 'मी', 'मू', 'मे'],
  PurvaPhalguni: ['मो', 'टा', 'टी', 'टू'],
  UttaraPhalguni: ['टे', 'टो', 'पा', 'पी'],
  Hasta: ['पू', 'ष', 'ण', 'ठ'],
  Chitra: ['पे', 'पो', 'रा', 'री'],
  Swati: ['रू', 'रे', 'रो', 'ता'],
  Vishakha: ['ती', 'तू', 'ते', 'तो'],
  Anuradha: ['ना', 'नी', 'नू', 'ने'],
  Jyeshtha: ['नो', 'या', 'यी', 'यू'],
  Mula: ['ये', 'यो', 'भा', 'भी'],
  PurvaAshadha: ['भू', 'धा', 'फा', 'ढा'],
  UttaraAshadha: ['भे', 'भो', 'जा', 'जी'],
  Shravana: ['जू', 'जे', 'जो', 'खी'],
  Dhanishta: ['गा', 'गी', 'गू', 'गे'],
  Shatabhisha: ['गो', 'सा', 'सी', 'सू'],
  PurvaBhadrapada: ['से', 'सो', 'दा', 'दी'],
  UttaraBhadrapada: ['दू', 'थ', 'झ', 'ञ'],
  Revati: ['दे', 'दो', 'चा', 'ची']
};

const NAKSHATRA_KEYS: Record<string, string> = {
  ashwini: 'Ashwini', asvini: 'Ashwini', bharani: 'Bharani', krittika: 'Krittika', kritika: 'Krittika',
  rohini: 'Rohini', mrigashira: 'Mrigashira', mrigasira: 'Mrigashira', ardra: 'Ardra', arudra: 'Ardra',
  punarvasu: 'Punarvasu', pushya: 'Pushya', pusya: 'Pushya', ashlesha: 'Ashlesha', aslesha: 'Ashlesha',
  magha: 'Magha', purvaphalguni: 'PurvaPhalguni', poorvaphalguni: 'PurvaPhalguni', pubba: 'PurvaPhalguni',
  uttaraphalguni: 'UttaraPhalguni', hasta: 'Hasta', chitra: 'Chitra', swati: 'Swati', svati: 'Swati',
  vishakha: 'Vishakha', visakha: 'Vishakha', anuradha: 'Anuradha', jyeshtha: 'Jyeshtha', jyestha: 'Jyeshtha',
  moola: 'Mula', mula: 'Mula', purvaashadha: 'PurvaAshadha', poorvaashadha: 'PurvaAshadha',
  uttaraashadha: 'UttaraAshadha', shravana: 'Shravana', sravana: 'Shravana', dhanishta: 'Dhanishta',
  dhanishtha: 'Dhanishta', shatabhisha: 'Shatabhisha', satabhisha: 'Shatabhisha',
  purvabhadrapada: 'PurvaBhadrapada', poorvabhadrapada: 'PurvaBhadrapada',
  uttarabhadrapada: 'UttaraBhadrapada', revati: 'Revati'
};

@Component({
  selector: 'app-panchang-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './panchang-card.component.html',
  styleUrls: ['./panchang-card.component.scss'],
})
export class PanchangCardComponent {
  @Input() panchang: any;

  constructor(private readonly apiTerms: ApiTermTranslationService) {}

  translatePanchang(field: 'maas' | 'paksha' | 'tithi' | 'nakshatra' | 'yoga' | 'karana' | 'vaar', value: unknown): string {
    return this.apiTerms.translatePanchangValue(field, value);
  }

  get padaLetter(): string {
    const fromChart = String(this.panchang?.akshara ?? this.panchang?.Akshara ?? '').trim();
    if (fromChart) {
      return fromChart;
    }

    const key = NAKSHATRA_KEYS[String(this.panchang?.nakshatra ?? '').toLowerCase().replace(/[^a-z0-9]/g, '')];
    const pada = Number(this.panchang?.pada);
    if (!key || pada < 1 || pada > 4) {
      return '';
    }

    return PADA_AKSHARA[key]?.[pada - 1] ?? '';
  }
}