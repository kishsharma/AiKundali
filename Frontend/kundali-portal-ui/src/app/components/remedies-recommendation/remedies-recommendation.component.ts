import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { environment } from '../../config/environment/environment';
import { AuthService } from '../../core/auth/auth.service';
import { ThemeService } from '../../core/services/theme.service';

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

  constructor(
    private readonly themes: ThemeService,
    private readonly auth: AuthService
  ) {}

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
    return !!(this.primary || this.alternative || this.categories.length || this.avoid.length || this.candidateEvidence.length || this.recommendation?.summary || this.recommendation?.Summary);
  }

  get candidateEvidence(): any[] {
    return this.recommendation?.candidateEvidence ?? this.recommendation?.CandidateEvidence ?? [];
  }

  get noGemstoneReason(): string {
    return this.recommendation?.noGemstoneReason ?? this.recommendation?.NoGemstoneReason ?? '';
  }

  get nonGemstoneOptions(): string[] {
    return this.recommendation?.nonGemstoneOptions ?? this.recommendation?.NonGemstoneOptions ?? [];
  }

  get summary(): string {
    return this.recommendation?.summary ?? this.recommendation?.Summary ?? '';
  }

  get certifiedGemstoneUrl(): string | null {
    const catalog = this.getRemedyCatalog(this.primary);
    const name = catalog?.gemstone?.primary?.name || this.primary?.name;
    return this.gemstoneUrl(name, this.primary?.confidence);
  }

  gemstoneUrl(name: string | null | undefined, score: number | null | undefined, area?: string | null): string | null {
    const slug = gemstoneStoreSlug(name);
    if (!slug) {
      return null;
    }

    const safeScore = Math.max(0, Math.min(100, Math.round(Number(score ?? 0))));
    const params = new URLSearchParams({
      kundaliMatch: '1',
      score: String(safeScore)
    });
    const areaLabel = formatRemedyArea(area);
    if (areaLabel) {
      params.set('area', areaLabel);
    }

    const base = environment.storeBaseUrl.replace(/\/$/, '');
    params.set('gemstone', slug);
    params.set('theme', this.themes.mode());
    return `${base}/?${params.toString()}`;
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

  openCertifiedGemstone(event: MouseEvent, url: string): void {
    if (!this.auth.isLoggedIn() || event.button !== 0 || event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
      return;
    }

    event.preventDefault();
    this.auth.issueStoreHandoff().subscribe({
      next: (handoff) => {
        const target = new URL(url);
        target.searchParams.set('handoff', handoff.code);
        window.open(target.toString(), '_blank', 'noopener,noreferrer');
      },
      error: () => window.open(url, '_blank', 'noopener,noreferrer')
    });
  }
}

const GEMSTONE_STORE_SLUGS: Record<string, string> = {
  ruby: 'ruby-manikya',
  manik: 'ruby-manikya',
  manikya: 'ruby-manikya',
  pearl: 'pearl-moti',
  moti: 'pearl-moti',
  redcoral: 'red-coral-moonga',
  moonga: 'red-coral-moonga',
  emerald: 'emerald-panna',
  panna: 'emerald-panna',
  yellowsapphire: 'yellow-sapphire-pukhraj',
  pukhraj: 'yellow-sapphire-pukhraj',
  whitesapphire: 'white-sapphire-safed-pukhraj',
  bluesapphire: 'blue-sapphire-neelam',
  neelam: 'blue-sapphire-neelam',
  hessonite: 'hessonite-gomed',
  gomed: 'hessonite-gomed',
  catseye: 'cats-eye-lehsunia',
  lehsunia: 'cats-eye-lehsunia'
};

function gemstoneStoreSlug(name: string | null | undefined): string | null {
  const key = (name ?? '').toLowerCase().replace(/[^a-z]/g, '');
  return GEMSTONE_STORE_SLUGS[key] ?? null;
}

function formatRemedyArea(area: string | null | undefined): string {
  const label = (area ?? '')
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/\s+based$/i, '')
    .trim();

  return label.slice(0, 40);
}
