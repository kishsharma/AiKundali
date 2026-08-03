import { NgIf } from '@angular/common';
import { Component, Input, inject } from '@angular/core';
import { ThemeService } from '../../../core/services/theme.service';

type BrandLogoVariant = 'full' | 'icon' | 'iconText';
type BrandLogoTone = 'auto' | 'color' | 'white';

@Component({
  selector: 'app-brand-logo',
  standalone: true,
  imports: [NgIf],
  templateUrl: './brand-logo.component.html',
  styleUrl: './brand-logo.component.scss',
})
export class BrandLogoComponent {
  private readonly themeService = inject(ThemeService);

  @Input() variant: BrandLogoVariant = 'iconText';
  @Input() tone: BrandLogoTone = 'auto';
  @Input() showDescriptor = true;

  get isFull(): boolean {
    return this.variant === 'full';
  }

  get isIconText(): boolean {
    return this.variant === 'iconText';
  }

  get logoSrc(): string {
    const tone = this.resolvedTone;

    if (this.variant === 'full') {
      return tone === 'white'
        ? 'assets/brand/horalife/horalife-full-white.svg'
        : 'assets/brand/horalife/horalife-full-color.svg';
    }

    return tone === 'white'
      ? 'assets/brand/horalife/horalife-icon-white.svg'
      : 'assets/brand/horalife/horalife-icon-color.svg';
  }

  get resolvedTone(): Exclude<BrandLogoTone, 'auto'> {
    if (this.tone !== 'auto') {
      return this.tone;
    }

    return this.themeService.mode() === 'dark' ? 'white' : 'color';
  }
}
