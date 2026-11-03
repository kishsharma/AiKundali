import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { getPanchangPadaLetter } from '../panchang-card/panchang-card.component';
@Component({
  selector: 'app-kundali-summary',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './kundali-summary.html',
  styleUrls: ['./kundali-summary.scss'],
})
export class KundaliSummary {
  constructor(private readonly apiTerms: ApiTermTranslationService) {}

  translateSign(value: unknown): string {
    return this.apiTerms.translateSign(value);
  }

  translatePlanet(value: unknown): string {
    return this.apiTerms.translatePlanet(value);
  }

  translatePanchang(field: 'maas' | 'paksha' | 'tithi' | 'nakshatra' | 'yoga' | 'karana' | 'vaar', value: unknown): string {
    return this.apiTerms.translatePanchangValue(field, value);
  }

  get padaLetter(): string {
    return getPanchangPadaLetter(this.panchang);
  }

  getLagnaLord(): string {
    return this.getSignLord(
      this.summary?.ascendant,
      this.summary?.ascendantLord,
      this.summary?.lagnaLord,
      this.summary?.risingLord
    );
  }

  getMoonSignLord(): string {
    return this.getSignLord(
      this.summary?.moonSign ?? this.summary?.rashi,
      this.summary?.moonSignLord,
      this.summary?.rashiLord,
      this.summary?.chandraLord
    );
  }

  getSunSignLord(): string {
    return this.getSignLord(
      this.summary?.sunSign,
      this.summary?.sunSignLord,
      this.summary?.suryaLord
    );
  }

  getMahadashaLord(): string {
    const explicitLord = this.firstText(this.summary?.currentMahadashaLord, this.summary?.mahadashaLord, this.summary?.dashaLord);
    return this.translatePlanet(explicitLord || this.summary?.currentMahadasha);
  }

  private getSignLord(sign: unknown, ...explicitLordValues: unknown[]): string {
    const explicitLord = this.firstText(...explicitLordValues);
    return explicitLord ? this.translatePlanet(explicitLord) : this.apiTerms.translateSignLord(sign);
  }

  private firstText(...values: unknown[]): string {
    const value = values.find(item => item !== null && item !== undefined && String(item).trim().length > 0);
    return value === undefined ? '' : String(value).trim();
  }
  @Input() summary: any;
  @Input() kundaliFormValue: any;
  @Input() panchang: any;
}
