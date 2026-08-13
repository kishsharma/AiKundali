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
}