import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AstrologyIconComponent, AstrologyIconName } from '../../shared/components/astrology-icon.component';
import { forkJoin, of, Subscription } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import {
  LearningDivisionalChartReference,
  LearningDoshaReference,
  LearningKnowledgeReferenceDetails,
  LearningRashiReference,
  LearningNakshatraReference,
  LearningGrahaReference,
  LearningLanguage,
  LearningPredictionAreaReference,
  LearningSessionResponse,
  LearningYogaReference,
  TableBlock
} from './learning.models';
import { LearningService } from './learning.service';

type ReferenceGroup = 'celestial' | 'panchanga' | 'grahas' | 'bhavas';
type ReferenceSection = 'tables' | 'rashis' | 'nakshatras' | 'grahas' | 'yogas' | 'doshas' | 'vargas';

interface GyanKoshaTable {
  id: string;
  sessionId: string;
  sessionNumber: number;
  sessionTitle: string;
  caption: string;
  columns: string[];
  rows: unknown[][];
  note?: string;
  group: ReferenceGroup;
}

@Component({
  selector: 'app-gyan-kosha',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoPipe, AstrologyIconComponent],
  templateUrl: './gyan-kosha.component.html',
  styleUrl: './gyan-kosha.component.scss'
})
export class GyanKoshaComponent implements OnInit, OnDestroy {
  tables: GyanKoshaTable[] = [];
  referenceDetails?: LearningKnowledgeReferenceDetails;
  loading = true;
  errorMessage = '';
  activeSection: ReferenceSection = 'tables';
  activeGroup: ReferenceGroup | 'all' = 'celestial';
  searchTerm = '';
  language: LearningLanguage = 'en';

  readonly groups: Array<ReferenceGroup | 'all'> = ['all', 'celestial', 'panchanga', 'grahas', 'bhavas'];
  readonly sections: ReferenceSection[] = ['tables', 'rashis', 'nakshatras', 'grahas', 'yogas', 'doshas', 'vargas'];

  private readonly subscriptions = new Subscription();
  private loadRequestId = 0;

  // These are the compact lookup tables learners repeatedly need while progressing
  // through the existing Level 1 lessons. Examples and worksheets stay in the lessons.
  private readonly rememberedTableIndexes: Record<number, number[]> = {
    1: [0, 2],
    3: [0],
    4: [0],
    5: [0, 1],
    6: [0],
    7: [0],
    8: [0, 1],
    9: [0, 2, 3],
    10: [0, 1, 2],
    11: [0, 1, 2],
    12: [0, 1],
    13: [0]
  };

  constructor(
    private readonly learningService: LearningService,
    private readonly transloco: TranslocoService,
    private readonly router: Router,
    private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.language = this.toLearningLanguage(this.transloco.getActiveLang());
    this.subscriptions.add(this.transloco.langChanges$.subscribe(lang => {
      const nextLanguage = this.toLearningLanguage(lang);
      if (nextLanguage === this.language) return;
      this.language = nextLanguage;
      this.loadTables();
    }));
    this.loadTables();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get filteredTables(): GyanKoshaTable[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return this.tables.filter(table => {
      const matchesGroup = this.activeGroup === 'all' || table.group === this.activeGroup;
      const matchesSearch = !query || `${table.caption} ${table.sessionTitle} ${table.columns.join(' ')} ${table.rows.flat().join(' ')}`
        .toLocaleLowerCase().includes(query);
      return matchesGroup && matchesSearch;
    });
  }

  get filteredYogas(): LearningYogaReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.yogas ?? []).filter(item =>
      !query || `${item.name} ${item.category} ${item.description} ${item.impact}`.toLocaleLowerCase().includes(query));
  }

  get filteredRashis(): LearningRashiReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.rashis ?? []).filter(item =>
      !query || `${item.name} ${item.englishName} ${item.ruler} ${item.element} ${item.modality} ${item.charans.map(pada => `${pada.nakshatra} ${pada.akshara} ${pada.syllable}`).join(' ')}`.toLocaleLowerCase().includes(query));
  }

  get filteredNakshatras(): LearningNakshatraReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.nakshatras ?? []).filter(item =>
      !query || `${item.name} ${item.sanskritName} ${item.lord} ${item.rashi} ${item.tatva} ${item.padas.map(pada => `${pada.akshara} ${pada.syllable} ${pada.navamsa}`).join(' ')}`.toLocaleLowerCase().includes(query));
  }

  get filteredGrahas(): LearningGrahaReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.grahas ?? []).filter(item =>
      !query || `${item.name} ${item.sanskritName} ${item.tattva} ${item.nature} ${item.ownSigns.join(' ')}`.toLocaleLowerCase().includes(query));
  }

  get filteredVimshottari() {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.vimshottari ?? []).filter(item => !query || item.planet.toLocaleLowerCase().includes(query));
  }

  get filteredDoshas(): LearningDoshaReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.doshas ?? []).filter(item =>
      !query || `${item.name} ${item.category} ${item.description}`.toLocaleLowerCase().includes(query));
  }

  get filteredDivisionalCharts(): LearningDivisionalChartReference[] {
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return (this.referenceDetails?.divisionalCharts ?? []).filter(item =>
      !query || `${item.code} ${item.name} ${item.primaryDomains.join(' ')} ${item.predictionRole}`
        .toLocaleLowerCase().includes(query));
  }

  get predictionAreas(): Array<{ key: string; area: LearningPredictionAreaReference }> {
    const areas = Object.entries(this.referenceDetails?.predictionAreas ?? {}).map(([key, area]) => ({ key, area }));
    const query = this.searchTerm.trim().toLocaleLowerCase();
    return areas.filter(item => !query || `${item.key} ${item.area.primaryCharts.join(' ')} ${item.area.supportingCharts.join(' ')}`
      .toLocaleLowerCase().includes(query));
  }

  groupLabel(group: ReferenceGroup | 'all'): string {
    return `gyanKosha.groups.${group}`;
  }

  setGroup(group: ReferenceGroup | 'all'): void {
    this.activeGroup = group;
  }

  setSection(section: ReferenceSection): void {
    this.activeSection = section;
    this.searchTerm = '';
  }

  openLesson(sessionId: string): void {
    this.router.navigate(['/app/learning/session', sessionId]);
  }

  cellText(value: unknown): string {
    return value === null || value === undefined ? '' : String(value);
  }

  topicLabel(key: string): string {
    return key.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/^./, first => first.toUpperCase());
  }

  rashiName(id: number): string {
    return this.referenceDetails?.rashis.find(rashi => rashi.id === id)?.englishName ?? '—';
  }

  birthTimeLabel(value?: string): string {
    if (!value) return '—';
    return value.replaceAll('_', ' ').toLocaleLowerCase().replace(/^./, first => first.toUpperCase());
  }

  entryIcons(description: string, kind: 'yoga' | 'dosha'): AstrologyIconName[] {
    const planetMatches: Array<{ pattern: RegExp; icon: AstrologyIconName }> = [
      { pattern: /\bSun\b/i, icon: 'sun' },
      { pattern: /\bMoon\b/i, icon: 'moon' },
      { pattern: /\bMars\b/i, icon: 'mars' },
      { pattern: /\bMercury\b/i, icon: 'mercury' },
      { pattern: /\bJupiter\b/i, icon: 'jupiter' },
      { pattern: /\bVenus\b/i, icon: 'venus' },
      { pattern: /\bSaturn\b/i, icon: 'saturn' },
      { pattern: /\bRahu\b/i, icon: 'rahu' },
      { pattern: /\bKetu\b/i, icon: 'ketu' }
    ];
    const planets = planetMatches
      .map(item => ({ ...item, index: description.search(item.pattern) }))
      .filter(item => item.index >= 0)
      .sort((left, right) => left.index - right.index)
      .map(item => item.icon);
    return [kind === 'yoga' ? 'yogas' : 'doshas', ...planets.slice(0, 3)];
  }

  iconLabel(icon: AstrologyIconName): string {
    return icon === 'yogas' ? 'Yoga combination' : icon === 'doshas' ? 'Dosha condition' : icon[0].toUpperCase() + icon.slice(1);
  }

  private loadTables(): void {
    const requestId = ++this.loadRequestId;
    this.loading = true;
    this.errorMessage = '';
    forkJoin({
      level: this.learningService.getLevel('level-1').pipe(catchError(error => {
        console.error('Gyan Kosha Level 1 load error:', error);
        return of(null);
      })),
      details: this.learningService.getKnowledgeReferenceDetails().pipe(catchError(error => {
        console.error('Gyan Kosha reference details load error:', error);
        return of(null);
      }))
    }).pipe(
      catchError(error => {
        console.error('Gyan Kosha index load error:', error);
        this.errorMessage = 'gyanKosha.loadError';
        return of({ level: null, details: null });
      })
    ).subscribe(result => {
      if (requestId !== this.loadRequestId) return;
      this.referenceDetails = result.details ?? undefined;
      if (!result.level) {
        if (result.details) {
          this.loading = false;
          this.cdr.markForCheck();
          return;
        }
        this.errorMessage ||= 'gyanKosha.loadError';
        this.loading = false;
        this.cdr.markForCheck();
        return;
      }
      const sessionIds = result.level.sessions
        .filter(session => Object.hasOwn(this.rememberedTableIndexes, session.sessionNumber))
        .map(session => session.id);
      this.loadSessionTables(sessionIds, requestId);
    });
  }

  private loadSessionTables(sessionIds: string[], requestId: number): void {
    const uniqueSessionIds = [...new Set(sessionIds)];
    if (!uniqueSessionIds.length) {
      this.tables = [];
      this.loading = false;
      this.cdr.markForCheck();
      return;
    }

    forkJoin(uniqueSessionIds.map(id => this.learningService.getSession(id, this.language)
      .pipe(catchError(() => of(null)))))
      .pipe(finalize(() => {
        if (requestId !== this.loadRequestId) return;
        this.loading = false;
        this.cdr.markForCheck();
      }))
      .subscribe(responses => {
        if (requestId !== this.loadRequestId) return;
        const sessions = responses.filter((item): item is LearningSessionResponse => item !== null);
        this.tables = sessions.flatMap(response => this.extractTables(response));
        if (!this.tables.length) this.errorMessage = 'gyanKosha.noTables';
        this.cdr.markForCheck();
      });
  }

  private extractTables(response: LearningSessionResponse): GyanKoshaTable[] {
    const session = response.session;
    const allowedIndexes = this.rememberedTableIndexes[session.sessionNumber] ?? [];
    const tableBlocks = session.sections.flatMap(section => section.blocks)
      .filter((block): block is TableBlock => block.type === 'table');

    return allowedIndexes.flatMap(index => {
      const table = tableBlocks[index];
      if (!table) return [];
      return [{
        id: `${session.id}-${index}`,
        sessionId: session.id,
        sessionNumber: session.sessionNumber,
        sessionTitle: session.title,
        caption: table.caption,
        columns: table.columns,
        rows: table.rows,
        note: table.note,
        group: this.groupForSession(session.sessionNumber)
      }];
    });
  }

  private groupForSession(sessionNumber: number): ReferenceGroup {
    if (sessionNumber === 1 || sessionNumber === 6) return 'celestial';
    if ([3, 4, 5, 7, 8].includes(sessionNumber)) return 'panchanga';
    if (sessionNumber === 9) return 'grahas';
    return 'bhavas';
  }

  private toLearningLanguage(value: string): LearningLanguage {
    return value === 'hi' || value === 'kn' ? value : 'en';
  }
}
