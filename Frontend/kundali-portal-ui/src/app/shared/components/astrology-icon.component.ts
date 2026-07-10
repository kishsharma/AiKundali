import { Component, Input } from '@angular/core';

export const ASTROLOGY_ICON_PATHS = {
  sun: 'assets/icons/astrology/sun.svg',
  moon: 'assets/icons/astrology/moon.svg',
  mars: 'assets/icons/astrology/mars.svg',
  mercury: 'assets/icons/astrology/mercury.svg',
  jupiter: 'assets/icons/astrology/jupiter.svg',
  venus: 'assets/icons/astrology/venus.svg',
  saturn: 'assets/icons/astrology/saturn.svg',
  rahu: 'assets/icons/astrology/rahu.svg',
  ketu: 'assets/icons/astrology/ketu.svg',
  aries: 'assets/icons/astrology/aries.svg',
  taurus: 'assets/icons/astrology/taurus.svg',
  gemini: 'assets/icons/astrology/gemini.svg',
  cancer: 'assets/icons/astrology/cancer.svg',
  leo: 'assets/icons/astrology/leo.svg',
  virgo: 'assets/icons/astrology/virgo.svg',
  libra: 'assets/icons/astrology/libra.svg',
  scorpio: 'assets/icons/astrology/scorpio.svg',
  sagittarius: 'assets/icons/astrology/sagittarius.svg',
  capricorn: 'assets/icons/astrology/capricorn.svg',
  aquarius: 'assets/icons/astrology/aquarius.svg',
  pisces: 'assets/icons/astrology/pisces.svg',
  kundali: 'assets/icons/astrology/kundali.svg',
  panchang: 'assets/icons/astrology/panchang.svg',
  dasha: 'assets/icons/astrology/dasha.svg',
  compatibility: 'assets/icons/astrology/compatibility.svg',
  info: 'assets/icons/astrology/info.svg',
  remedies: 'assets/icons/astrology/remedies.svg',
  yogas: 'assets/icons/astrology/yogas.svg',
  'bhava-score': 'assets/icons/astrology/bhava-score.svg',
  reports: 'assets/icons/astrology/reports.svg',
  settings: 'assets/icons/astrology/settings.svg'
} as const;

export type AstrologyIconName = keyof typeof ASTROLOGY_ICON_PATHS;

@Component({
  selector: 'app-astrology-icon',
  standalone: true,
  template: `
    <span
      class="astrology-icon"
      [style.--icon-url]="iconUrl"
      [attr.aria-hidden]="decorative ? 'true' : null"
      [attr.role]="decorative ? null : 'img'"
      [attr.aria-label]="decorative ? null : accessibleLabel">
    </span>
  `,
  styles: [`
    :host {
      display: inline-flex;
      width: 1em;
      height: 1em;
      flex: 0 0 auto;
      color: inherit;
      line-height: 1;
      vertical-align: -0.125em;
    }

    .astrology-icon {
      display: block;
      width: 100%;
      height: 100%;
      background: currentColor;
      mask: var(--icon-url) center / contain no-repeat;
      -webkit-mask: var(--icon-url) center / contain no-repeat;
    }
  `]
})
export class AstrologyIconComponent {
  @Input({ required: true }) name!: AstrologyIconName;
  @Input() decorative = true;
  @Input() label?: string;

  get iconUrl(): string {
    return `url("${ASTROLOGY_ICON_PATHS[this.name]}")`;
  }

  get accessibleLabel(): string {
    return this.label ?? this.name.replace(/-/g, ' ');
  }
}
