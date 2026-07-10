import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

type InsightRow = {
  labelKey: string;
  value: string;
  valueKey?: string;
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
    Sun: '☉',
    Moon: '☾',
    Mars: '♂',
    Mercury: '☿',
    Jupiter: '♃',
    Venus: '♀',
    Saturn: '♄',
    Rahu: '☊',
    Ketu: '☋'
  };

  constructor(private translocoService: TranslocoService) {}

  get rashiRows(): InsightRow[] {
    const item = this.rashiInsight;

    if (!item) {
      return [];
    }

    return [
      { labelKey: 'metadata.ruler', value: item.ruler, valueKey: this.translationKey('planet', item.ruler), icon: this.getPlanetGlyph(item.ruler) },
      { labelKey: 'metadata.element', value: item.element },
      { labelKey: 'metadata.strength', value: item.strength },
      { labelKey: 'metadata.weakness', value: item.weakness },
      { labelKey: 'metadata.gemstone', value: item.gemstone },
      { labelKey: 'metadata.metal', value: item.metal },
      { labelKey: 'metadata.color', value: item.color, color: item.color },
      { labelKey: 'metadata.luckyNumbers', value: this.joinValues(item.luckyNumbers) },
      { labelKey: 'metadata.favorableDay', value: item.favorableDay },
      { labelKey: 'metadata.favorableDirection', value: item.favorableDirection }
    ];
  }

  get nakshatraRows(): InsightRow[] {
    const item = this.nakshatraInsight;

    if (!item) {
      return [];
    }

    return [
      { labelKey: 'metadata.lord', value: item.lord, valueKey: this.translationKey('planet', item.lord), icon: this.getPlanetGlyph(item.lord) },
      { labelKey: 'metadata.symbol', value: item.symbol },
      { labelKey: 'metadata.gana', value: item.gana },
      { labelKey: 'metadata.yoni', value: item.yoni },
      { labelKey: 'metadata.nadi', value: item.nadi },
      { labelKey: 'metadata.varna', value: item.varna },
      { labelKey: 'metadata.vashya', value: item.vashya },
      { labelKey: 'metadata.dosha', value: item.dosha },
      { labelKey: 'metadata.fastingDay', value: item.fastingDay },
      { labelKey: 'metadata.businessSuitability', value: item.businessSuitability }
    ];
  }

  get rashiTitle(): string {
    const name = this.rashiInsight?.englishName || this.rashiInsight?.name;
    const symbol = this.rashiInsight?.symbol;

    return [symbol, name].filter(Boolean).join(' ');
  }

  get nakshatraTitle(): string {
    const name = this.nakshatraInsight?.name;
    const pada = this.nakshatraInsight?.pada;

    return pada ? `${name} Pada ${pada}` : name;
  }

  displayValue(row: InsightRow): string {
    if (!row.value) {
      return '—';
    }

    if (!row.valueKey) {
      return row.value;
    }

    const translated = this.translocoService.translate(row.valueKey);
    return translated && translated !== row.valueKey ? translated : row.value;
  }

  private joinValues(value: any): string {
    if (Array.isArray(value)) {
      return value.join(', ');
    }

    return value?.toString() ?? '';
  }

  private getPlanetGlyph(planet: string | null | undefined): string {
    return this.planetGlyphMap[planet ?? ''] ?? '•';
  }

  private translationKey(prefix: string, value: string | null | undefined): string | undefined {
    return value ? `${prefix}.${value}` : undefined;
  }
}
