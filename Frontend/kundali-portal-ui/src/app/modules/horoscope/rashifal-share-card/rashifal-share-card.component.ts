import { CommonModule } from '@angular/common';
import { Component, ElementRef, Input, ViewChild } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import html2canvas from 'html2canvas';
import { HoroscopeRashifalItem } from '../horoscope.models';

@Component({
  selector: 'app-rashifal-share-card',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './rashifal-share-card.component.html',
  styleUrl: './rashifal-share-card.component.scss'
})
export class RashifalShareCardComponent {
  constructor(private readonly transloco: TranslocoService) {}
  @Input({ required: true }) item!: HoroscopeRashifalItem;
  @Input() horizon = 'Daily';
  @Input() from = '';
  @Input() to = '';
  @ViewChild('shareCard') private shareCard?: ElementRef<HTMLElement>;

  busy = false;
  feedback = '';

  get indianRashiName(): string {
    const key = `horoscope.rashi.${this.item.englishName.toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? this.item.rashi || this.item.englishName : localized;
  }

  get zodiacIconPath(): string {
    return 'assets/horoscope/rashi-icons/' + this.item.englishName.toLowerCase() + '.svg';
  }

  formatRashifalDate(value: string): string {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');
    if (!match) {
      return value ?? '';
    }
    const date = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    const locale = this.transloco.getActiveLang() === 'hi' ? 'hi-IN'
      : this.transloco.getActiveLang() === 'kn' ? 'kn-IN' : 'en-GB';
    return new Intl.DateTimeFormat(locale, { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
  }

  toneLabel(tone: string): string {
    const key = `horoscope.tone.${(tone || 'mixed').toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? tone : localized;
  }

  areaLabel(areaKey: string): string {
    const key = `horoscope.area.${(areaKey || '').toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? areaKey : localized;
  }

  horizonLabel(): string {
    const key = `horoscope.${this.horizon.toLowerCase()}`;
    const localized = this.transloco.translate(key);
    return localized === key ? this.horizon : localized;
  }

  get formattedPeriod(): string {
    const start = this.formatRashifalDate(this.from);
    const end = this.formatRashifalDate(this.to);
    return end && end !== start ? start + ' — ' + end : start;
  }

  async downloadPng(): Promise<void> {
    try {
      const canvas = await this.capture();
      const link = document.createElement('a');
      link.download = `horalife-${this.item.englishName.toLowerCase()}-${this.horizon.toLowerCase()}.png`;
      link.href = canvas.toDataURL('image/png');
      link.click();
      this.feedback = this.transloco.translate('horoscope.share.downloadReady');
    } catch {
      this.feedback = this.transloco.translate('horoscope.share.downloadFailed');
    }
  }

  async share(): Promise<void> {
    const message = this.shareMessage();
    if (typeof navigator.share !== 'function') {
      this.feedback = this.transloco.translate('horoscope.share.unavailable');
      return;
    }
    try {
      if (typeof navigator.canShare === 'function') {
        const canvas = await this.capture();
        const blob = await new Promise<Blob>((resolve, reject) =>
          canvas.toBlob(value => value ? resolve(value) : reject(new Error('Image export failed')), 'image/png'));
        const file = new File([blob], `horalife-${this.item.englishName.toLowerCase()}.png`, { type: 'image/png' });
        if (navigator.canShare({ files: [file] })) {
          await navigator.share({ title: `HoraLife ${this.horizonLabel()} ${this.transloco.translate('horoscope.share.rashifal')}`, text: message, files: [file] });
          this.feedback = this.transloco.translate('horoscope.share.shared');
          return;
        }
      }
      await navigator.share({ title: `HoraLife ${this.horizonLabel()} ${this.transloco.translate('horoscope.share.rashifal')}`, text: message });
      this.feedback = this.transloco.translate('horoscope.share.shared');
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      this.feedback = this.transloco.translate('horoscope.share.failed');
    }
  }

  shareToWhatsApp(): void {
    const message = this.shareMessage();
    window.open(`https://wa.me/?text=${encodeURIComponent(message)}`, '_blank', 'noopener,noreferrer');
    this.feedback = this.transloco.translate('horoscope.share.whatsappOpened');
  }

  private async capture(): Promise<HTMLCanvasElement> {
    if (!this.shareCard || this.busy) {
      throw new Error('Share card is not ready');
    }
    this.busy = true;
    try {
      return await html2canvas(this.shareCard.nativeElement, {
        backgroundColor: '#111a30',
        scale: 2,
        useCORS: true
      });
    } finally {
      this.busy = false;
    }
  }

  private shareMessage(): string {
    const period = this.formattedPeriod;
    return `${this.indianRashiName} ${this.horizonLabel()} ${this.transloco.translate('horoscope.share.rashifal')}${period ? ` · ${period}` : ''}\n${this.toneLabel(this.item.tone)} · ${this.item.score}/100\n\n${this.item.summary}\n\n${this.transloco.translate('horoscope.share.brandLine')}`;
  }
}



