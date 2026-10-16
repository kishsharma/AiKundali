import { Directive, Input, inject } from '@angular/core';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { SubscriptionModalService } from '../../core/services/subscription-modal.service';
import {
  DoshaAttentionReport,
  rankDoshaAttention,
  rankYogaAttention
} from '../../modules/kundali/dosha-attention';

export interface NatalGroupedItem {
  item: any;
  index: number;
}

export interface ActivationPeriodGroup {
  title: string;
  period: string;
  dasha: string;
  trigger: string;
  isCurrent: boolean;
  items: Array<{ item: any; index: number; activation: any }>;
}

@Directive()
export abstract class KundaliYogaDoshaPanelBase {
  protected readonly apiTerms = inject(ApiTermTranslationService);
  protected readonly subscriptionModal = inject(SubscriptionModalService);

  @Input() items: any[] = [];
  @Input() isPremiumUser = false;

  openIndex: number | null = null;

  protected abstract kind: 'yoga' | 'dosha';
  protected abstract currentTitle: string;
  protected abstract upcomingTitle: string;

  get attention(): DoshaAttentionReport {
    const formatters = {
      formatPeriod: (activation: any) => this.formatActivationPeriod(activation),
      planet: (name: string) => this.translatePlanetName(name)
    };
    return this.kind === 'yoga'
      ? rankYogaAttention(this.items, formatters)
      : rankDoshaAttention(this.items, formatters);
  }

  get quietNames(): string {
    return this.attention.quiet.map(entry => entry.name).join(', ');
  }

  get currentActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.items, 'current', this.currentTitle)
      : [];
  }

  get upcomingActivationGroups(): ActivationPeriodGroup[] {
    return this.isPremiumUser
      ? this.buildActivationPeriodGroups(this.items, 'upcoming', this.upcomingTitle)
      : [];
  }

  get natalItems(): NatalGroupedItem[] {
    const natalItems = this.items.map((item, index) => ({ item, index }));
    return this.isPremiumUser
      ? natalItems.filter(({ item }) => !this.hasActivationPeriod(item))
      : natalItems;
  }

  onUnlockPremium(): void {
    this.subscriptionModal.open();
  }

  translatePlanetName(planet: unknown): string {
    return this.apiTerms.translatePlanet(planet);
  }

  translateControlledValue(group: 'strength' | 'severity' | 'tone' | 'activationLevel', value: unknown): string {
    return this.apiTerms.translateControlledValue(group, value);
  }

  translatePlanetList(value: any): string {
    const planets = Array.isArray(value) ? value : [];
    return planets.length ? planets.map(planet => this.translatePlanetName(planet)).join(', ') : '';
  }

  getCurrentActivation(item: any): any | null {
    return item?.activationPeriods?.currentlyActive?.[0] ?? null;
  }

  getNextActivation(item: any): any | null {
    return item?.activationPeriods?.upcoming?.[0] ?? null;
  }

  hasActivationPeriod(item: any): boolean {
    return !!(this.getCurrentActivation(item) || this.getNextActivation(item));
  }

  hasActivationAccess(item: any): boolean {
    return this.isPremiumUser && !!item?.activationPeriods;
  }

  hasAnyActivationAccess(items: any[]): boolean {
    return (items ?? []).some(item => this.hasActivationAccess(item));
  }

  getActivationDisplayRows(item: any): any[] {
    const current = this.normalizeActivationList(item?.activationPeriods?.currentlyActive);
    const upcoming = this.normalizeActivationList(item?.activationPeriods?.upcoming);

    return [
      ...current.map((activation: any) => ({ activation, labelKey: 'kundali.activation.activeNow', isCurrent: true })),
      ...upcoming.slice(0, 3).map((activation: any, index: number) => ({
        activation,
        labelKey: index === 0 ? 'kundali.activation.nextActivation' : 'kundali.activation.upcomingActivation',
        isCurrent: false
      }))
    ];
  }

  getActivationPlanetRaw(activation: any, key: 'mahadashaPlanet' | 'antardashaPlanet'): string {
    const pascalKey = key.charAt(0).toUpperCase() + key.slice(1);
    return String(activation?.[key] ?? activation?.[pascalKey] ?? '').trim();
  }

  formatActivationDate(value: string | Date | null | undefined): string {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return new Intl.DateTimeFormat('en-IN', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    }).format(date);
  }

  formatActivationPeriod(activation: any): string {
    if (!activation) {
      return '';
    }

    const startDate = activation.startDate ?? activation.StartDate;
    const endDate = activation.endDate ?? activation.EndDate;
    return `${this.formatActivationDate(startDate)} to ${this.formatActivationDate(endDate)}`;
  }

  formatActivationDashaPeriod(activation: any): string {
    if (!activation) {
      return '';
    }

    const maha = activation.mahadashaPlanet ?? activation.MahadashaPlanet;
    const antar = activation.antardashaPlanet ?? activation.AntardashaPlanet;

    return [maha ? `${maha} Mahadasha` : '', antar ? `${antar} Antardasha` : '']
      .filter(Boolean)
      .join(' / ');
  }

  formatActivationTrigger(activation: any): string {
    if (!activation) {
      return '';
    }

    const triggeredBy = activation.triggeredBy ?? activation.TriggeredBy ?? [];
    const maha = activation.mahadashaPlanet ?? activation.MahadashaPlanet;
    const antar = activation.antardashaPlanet ?? activation.AntardashaPlanet;

    if (!Array.isArray(triggeredBy) || !triggeredBy.length) {
      return this.formatActivationDashaPeriod(activation);
    }

    return triggeredBy
      .map((planet: string) => {
        if (maha && String(planet).toLowerCase() === String(maha).toLowerCase()) {
          return `${planet} Mahadasha`;
        }
        if (antar && String(planet).toLowerCase() === String(antar).toLowerCase()) {
          return `${planet} Antardasha`;
        }
        return this.translatePlanetName(planet);
      })
      .join(' / ');
  }

  getActivationLevel(activation: any): string {
    const value = activation?.activationLevel ?? activation?.ActivationLevel ?? '';
    return this.translateControlledValue('activationLevel', value);
  }

  getRawActivationLevel(activation: any): string {
    return activation?.activationLevel ?? activation?.ActivationLevel ?? '';
  }

  isStrongActivation(activation: any): boolean {
    return this.getRawActivationLevel(activation).toLowerCase() === 'strong';
  }

  getActivationLevelClass(activation: any): Record<string, boolean> {
    const level = this.getRawActivationLevel(activation).toLowerCase();
    return {
      strong: level === 'strong',
      active: level === 'active',
      mild: level === 'mild'
    };
  }

  getDoshaSeverityClass(value: string | null | undefined): Record<string, boolean> {
    const severity = (value ?? '').toLowerCase();
    return {
      high: severity.includes('high'),
      medium: severity.includes('medium'),
      low: severity.includes('low') || !severity
    };
  }

  getYogaStrengthClass(value: string | null | undefined): Record<string, boolean> {
    const strength = (value ?? '').toLowerCase();
    return {
      strong: strength.includes('strong'),
      medium: strength.includes('medium') || strength.includes('moderate') || strength.includes('neutral'),
      weak: strength.includes('weak') || strength.includes('low')
    };
  }

  handleDetailsAction(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }
    this.openIndex = this.openIndex === index ? null : index;
  }

  isDetailsOpen(index: number): boolean {
    return this.openIndex === index;
  }

  isPriority(index: number): boolean {
    return this.attention.focus?.index === index;
  }

  openFocus(index: number): void {
    if (!this.isPremiumUser) {
      this.onUnlockPremium();
      return;
    }

    this.openIndex = index;
    const selector = this.kind === 'yoga' ? '.yoga-item.needs-attention' : '.dosha-item.needs-attention';
    document.querySelector(selector)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  hasImpactDetails(item: any): boolean {
    return !!(item?.impactAreas?.length || item?.remedies?.length || item?.impact);
  }

  getImpactIcon(category: string | null | undefined): string {
    const normalized = (category ?? '').trim();
    if (!normalized) {
      return 'IN';
    }

    return normalized
      .split(/[\s&/,-]+/)
      .filter(Boolean)
      .slice(0, 2)
      .map(part => part.charAt(0).toUpperCase())
      .join('');
  }

  getImpactTone(category: string | null | undefined): Record<string, boolean> {
    const normalized = (category ?? '').toLowerCase();
    return {
      mind: normalized.includes('mental') || normalized.includes('emotional') || normalized.includes('mind'),
      relation: normalized.includes('relationship') || normalized.includes('marriage') || normalized.includes('family'),
      career: normalized.includes('career') || normalized.includes('financial') || normalized.includes('wealth'),
      health: normalized.includes('health'),
      spiritual: normalized.includes('spiritual')
    };
  }

  private normalizeActivationList(rawList: any): any[] {
    return Array.isArray(rawList) ? rawList.filter(Boolean) : [];
  }

  private buildActivationPeriodGroups(
    items: any[],
    mode: 'current' | 'upcoming',
    title: string
  ): ActivationPeriodGroup[] {
    const groups = new Map<string, ActivationPeriodGroup>();

    items.forEach((item, index) => {
      const activation = mode === 'current' ? this.getCurrentActivation(item) : this.getNextActivation(item);
      if (!activation) {
        return;
      }

      const period = this.formatActivationPeriod(activation);
      const dasha = this.formatActivationDashaPeriod(activation);
      const trigger = this.formatActivationTrigger(activation);
      const key = [mode, period, dasha, trigger].join('|');

      if (!groups.has(key)) {
        groups.set(key, {
          title,
          period,
          dasha,
          trigger,
          isCurrent: mode === 'current',
          items: []
        });
      }

      groups.get(key)?.items.push({ item, index, activation });
    });

    return Array.from(groups.values());
  }
}
