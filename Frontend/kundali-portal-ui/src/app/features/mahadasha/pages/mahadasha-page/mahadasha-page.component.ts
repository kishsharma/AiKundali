import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Observable } from 'rxjs';
import { PageHeaderComponent } from '../../../../shared/components/page-header.component';
import { MahadashaService } from '../../services/mahadasha.service';
import {
  AntardashaPeriod,
  ImportantDate,
  LifeArea,
  MahadashaAnalysis,
  MahadashaPeriod,
  PredictionPhase,
} from '../../models/mahadasha.model';

@Component({
  selector: 'app-mahadasha-page',
  standalone: true,
  imports: [CommonModule, PageHeaderComponent],
  templateUrl: './mahadasha-page.component.html',
  styleUrls: ['./mahadasha-page.component.scss'],
})
export class MahadashaPageComponent {
  private readonly mahadashaService = inject(MahadashaService);

  readonly analysis$: Observable<MahadashaAnalysis> = this.mahadashaService.getAnalysis();

  readonly note =
    'Dasha results vary based on your actions, planetary strength, and transits. Use this guidance to make better decisions and grow in life.';

  readonly phaseOrder: Array<'start' | 'middle' | 'end'> = ['start', 'middle', 'end'];

  readonly strengthRows = [
    { label: 'Dasha Lord', value: 'planetWithNature' },
    { label: 'Dasha Lord Strength', value: 'planetStrength' },
    { label: 'Placement', value: 'placement' },
    { label: 'Aspect on Lagna', value: 'aspectOnLagna' },
    { label: 'Sitting with Benefics', value: 'supportedByBenefics' },
    { label: 'Overall Result', value: 'overallResult' },
  ] as const;

  getStars(rating: number): boolean[] {
    return Array.from({ length: 5 }, (_, index) => index < Math.round(rating));
  }

  getPlanetMark(planet: string): string {
    const marks: Record<string, string> = {
      Sun: 'Su',
      Moon: 'Mo',
      Mars: 'Ma',
      Rahu: 'Ra',
      Jupiter: 'Ju',
      Saturn: 'Sa',
      Mercury: 'Me',
      Ketu: 'Ke',
      Venus: 'Ve',
    };

    return marks[planet] ?? planet.slice(0, 2);
  }

  getAreaMark(area: string): string {
    const marks: Record<string, string> = {
      Career: 'Br',
      Finance: 'Rs',
      Relationships: 'He',
      Marriage: 'Ma',
      Health: 'Ec',
      Education: 'Ed',
      Spirituality: 'Sp',
      Travel: 'Tr',
    };

    return marks[area] ?? area.slice(0, 2);
  }

  getStrengthValue(analysis: MahadashaAnalysis, key: string): string {
    const strength = analysis.strength;

    if (key === 'planetWithNature') {
      return `${strength.planet} (${analysis.currentMahadasha.nature})`;
    }

    if (key === 'aspectOnLagna') {
      return strength.aspectOnLagna ? 'Yes' : 'No';
    }

    if (key === 'supportedByBenefics') {
      const support = strength.supportedBy.length ? ` (${strength.supportedBy.join(', ')})` : '';
      return strength.supportedByBenefics ? `Yes${support}` : 'No';
    }

    return String(strength[key as keyof typeof strength] ?? '');
  }

  formatDate(value: string): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }

  formatPeriod(start?: string, end?: string): string {
    return [start, end].filter(Boolean).join(' - ');
  }

  trackLifeArea(_: number, area: LifeArea): string {
    return area.name;
  }

  trackMahadasha(_: number, period: MahadashaPeriod): string {
    return `${period.planet}-${period.start}-${period.end}`;
  }

  trackAntardasha(_: number, period: AntardashaPeriod): string {
    return `${period.planet}-${period.start}-${period.end}`;
  }

  trackDate(_: number, date: ImportantDate): string {
    return `${date.title}-${date.date}`;
  }

  trackText(_: number, text: string): string {
    return text;
  }

  trackPhase(_: number, phase: PredictionPhase): string {
    return phase.title;
  }
}
