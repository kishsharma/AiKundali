import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

type RemedySelection = {
  remedyType?: string;
  recommendationRole?: string;
  planet?: string;
  name?: string;
  sanskritName?: string;
  englishName?: string;
  category?: string;
  strengthLevel?: string;
  confidence?: number;
  suitability?: string;
  reason?: string;
  factors?: string[];
  remedies?: RemedyCatalog;
};

type RemedyCatalog = {
  gemstone?: {
    primary?: RemedyStone;
    alternatives?: RemedyStone[];
    wearingRules?: {
      finger?: string;
      dayToWear?: string;
      time?: string;
      metal?: string[];
    };
    caution?: string;
  };
  yantra?: {
    name?: string;
    purpose?: string;
    applicableWhen?: string[];
  };
  daan?: string[];
  japa?: {
    mantra?: string;
    count?: number;
    day?: string;
  };
};

type RemedyStone = {
  name?: string;
  sanskritName?: string;
  englishName?: string;
  purpose?: string;
  recommendedFor?: string[];
};

@Component({
  selector: 'app-remedies-recommendation',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './remedies-recommendation.component.html',
  styleUrls: ['./remedies-recommendation.component.scss']
})
export class RemediesRecommendationComponent {
  private recommendationData: any;
  expandedPanel: 'analysis' | 'remedies' | 'avoid' | null = null;

  @Input() set recommendation(value: any) {
    this.recommendationData = value;
  }

  get recommendation(): any {
    return this.recommendationData;
  }

  get primary(): RemedySelection | null {
    return this.recommendation?.primary ?? this.recommendation?.Primary ?? null;
  }

  get alternative(): RemedySelection | null {
    return this.recommendation?.alternative ?? this.recommendation?.Alternative ?? null;
  }

  get categories(): any[] {
    return this.recommendation?.categories ?? this.recommendation?.Categories ?? [];
  }

  get avoid(): any[] {
    return this.recommendation?.avoid ?? this.recommendation?.Avoid ?? [];
  }

  get hasRecommendation(): boolean {
    return !!(this.primary || this.alternative || this.categories.length || this.avoid.length);
  }

  getRemedyCatalog(selection: RemedySelection | null): RemedyCatalog | null {
    return selection?.remedies ?? (selection as any)?.Remedies ?? null;
  }

  togglePanel(panel: 'analysis' | 'remedies' | 'avoid'): void {
    this.expandedPanel = this.expandedPanel === panel ? null : panel;
  }

  isPanelOpen(panel: 'analysis' | 'remedies' | 'avoid'): boolean {
    return this.expandedPanel === panel;
  }

  getConfidenceStyle(value: number | null | undefined): Record<string, string> {
    const safeValue = Math.max(0, Math.min(100, Number(value ?? 0)));
    return { width: `${safeValue}%` };
  }

  getSuitabilityLabel(value: string | null | undefined): string {
    const normalized = value || 'ReviewRequired';
    return normalized.replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  getSuitabilityClass(value: string | null | undefined): Record<string, boolean> {
    const normalized = (value ?? '').toLowerCase();

    return {
      strong: normalized.includes('highly') || normalized === 'recommended',
      caution: normalized.includes('caution') || normalized.includes('alternative'),
      avoid: normalized.includes('not')
    };
  }

  joinValues(values: string[] | null | undefined): string {
    return values?.filter(Boolean).join(', ') || '-';
  }
}
