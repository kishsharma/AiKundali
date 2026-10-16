import { CommonModule } from '@angular/common';
import { Component, ElementRef, Input, ViewChild } from '@angular/core';
import html2canvas from 'html2canvas';
import { HoroscopeRashifalItem } from '../horoscope.models';

@Component({
  selector: 'app-rashifal-share-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './rashifal-share-card.component.html',
  styleUrl: './rashifal-share-card.component.scss'
})
export class RashifalShareCardComponent {
  @Input({ required: true }) item!: HoroscopeRashifalItem;
  @Input() horizon = 'Daily';
  @Input() from = '';
  @Input() to = '';
  @ViewChild('shareCard') private shareCard?: ElementRef<HTMLElement>;

  busy = false;
  feedback = '';

  get indianRashiName(): string {
    const indianNames: Record<string, string> = {
      aries: 'Mesha', taurus: 'Vrishabha', gemini: 'Mithuna', cancer: 'Karka',
      leo: 'Simha', virgo: 'Kanya', libra: 'Tula', scorpio: 'Vrischika',
      sagittarius: 'Dhanu', capricorn: 'Makara', aquarius: 'Kumbha', pisces: 'Meena'
    };
    return indianNames[this.item.englishName.toLowerCase()] ?? this.item.rashi ?? this.item.englishName;
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
    return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
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
      this.feedback = 'Your share card is ready to save.';
    } catch {
      this.feedback = 'Could not create the image. Please try again.';
    }
  }

  async share(): Promise<void> {
    const message = this.shareMessage();
    if (typeof navigator.share !== 'function') {
      this.feedback = 'Image sharing is unavailable in this browser. Download the PNG, or use WhatsApp below.';
      return;
    }
    try {
      if (typeof navigator.canShare === 'function') {
        const canvas = await this.capture();
        const blob = await new Promise<Blob>((resolve, reject) =>
          canvas.toBlob(value => value ? resolve(value) : reject(new Error('Image export failed')), 'image/png'));
        const file = new File([blob], `horalife-${this.item.englishName.toLowerCase()}.png`, { type: 'image/png' });
        if (navigator.canShare({ files: [file] })) {
          await navigator.share({ title: `HoraLife ${this.horizon} Rashifal`, text: message, files: [file] });
          this.feedback = 'Your Rashifal card has been shared.';
          return;
        }
      }
      await navigator.share({ title: `HoraLife ${this.horizon} Rashifal`, text: message });
      this.feedback = 'Your Rashifal reading has been shared.';
    } catch (error) {
      if (error instanceof DOMException && error.name === 'AbortError') {
        return;
      }
      this.feedback = 'Could not share just now. Download the PNG and try again.';
    }
  }

  shareToWhatsApp(): void {
    const message = this.shareMessage();
    window.open(`https://wa.me/?text=${encodeURIComponent(message)}`, '_blank', 'noopener,noreferrer');
    this.feedback = 'WhatsApp opened with your Rashifal message. Attach the downloaded card image to share the artwork.';
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
    const period = this.to && this.to !== this.from ? `${this.from} – ${this.to}` : this.from;
    return `${this.item.englishName} ${this.horizon} Rashifal${period ? ` · ${period}` : ''}\n${this.item.tone} · ${this.item.score}/100\n\n${this.item.summary}\n\nRead your stars with HoraLife.`;
  }
}



