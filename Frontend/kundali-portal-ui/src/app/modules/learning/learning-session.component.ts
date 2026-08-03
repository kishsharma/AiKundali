import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit,ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Subscription } from 'rxjs';
import { distinctUntilChanged, finalize } from 'rxjs/operators';
import {
  ComparisonBlock,
  DefinitionBlock,
  DiagramBlock,
  ExampleBlock,
  FormulaBlock,
  HeadingBlock,
  LearningContentBlock,
  LearningGlossaryEntry,
  LearningLanguage,
  LearningLevelManifest,
  LearningSession,
  LearningSessionSummary,
  ListBlock,
  NoteBlock,
  ParagraphBlock,
  ShlokaTriggerBlock,
  TableBlock
} from './learning.models';
import { LearningService } from './learning.service';
import { TranslocoService } from '@jsverse/transloco';

interface GlossaryTextSegment {
  text: string;
  entry?: LearningGlossaryEntry;
}

interface GlossaryMatcher {
  value: string;
  lowerValue: string;
  entry: LearningGlossaryEntry;
}

@Component({
  selector: 'app-learning-session',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './learning-session.component.html',
  styleUrl: './learning-session.component.scss'
})
export class LearningSessionComponent implements OnInit, OnDestroy {
  readonly languages: Array<{ code: LearningLanguage; label: string }> = [
    { code: 'en', label: 'English' },
    { code: 'hi', label: 'Hindi' },
    { code: 'kn', label: 'Kannada' }
  ];

  level?: LearningLevelManifest;
  session?: LearningSession;
  manifestSession?: LearningSessionSummary;
  glossary: LearningGlossaryEntry[] = [];
  activeGlossaryEntry?: LearningGlossaryEntry;
  glossaryTooltipLeft = 16;
  glossaryTooltipTop = 16;
  private glossaryMatchers: GlossaryMatcher[] = [];
  selectedLanguage: LearningLanguage = 'en';
  resolvedLanguage: LearningLanguage = 'en';
  usedFallback = false;
  loading = true;
  refreshing = false;
  errorMessage = '';
  notFound = false;
  private routeSubscription?: Subscription;
  private languageSubscription?: Subscription;
  private sessionRequestId = 0;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly learningService: LearningService,
    private readonly translocoService: TranslocoService,
     private readonly cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.selectedLanguage = this.toLearningLanguage(this.translocoService.getActiveLang());

    this.routeSubscription = this.route.paramMap.subscribe(params => {
      const sessionGuid = params.get('sessionGuid');
      if (sessionGuid) {
        this.loadPage(sessionGuid);
      }
    });

    this.languageSubscription = this.translocoService.langChanges$
      .pipe(distinctUntilChanged())
      .subscribe(lang => {
        const nextLanguage = this.toLearningLanguage(lang);
        if (nextLanguage === this.selectedLanguage) {
          return;
        }

        const currentSectionId = this.getCurrentSectionId();
        this.selectedLanguage = nextLanguage;
        const sessionGuid = this.route.snapshot.paramMap.get('sessionGuid');
        if (sessionGuid) {
          this.loadSession(sessionGuid, currentSectionId, true);
        }
      });
  }

  ngOnDestroy(): void {
    this.routeSubscription?.unsubscribe();
    this.languageSubscription?.unsubscribe();
  }

  onLanguageChange(event: Event): void {
    const value = this.toLearningLanguage((event.target as HTMLSelectElement).value);
    if (value === this.selectedLanguage && value === this.toLearningLanguage(this.translocoService.getActiveLang())) {
      return;
    }

    this.translocoService.setActiveLang(value);
  }

  navigateToSession(session: LearningSessionSummary): void {
    if (!this.isSessionAvailable(session)) {
      return;
    }

    this.router.navigate(['/app/learning/session', session.id]);
  }

  goToAdjacent(direction: -1 | 1): void {
    const target = direction < 0 ? this.previousSession : this.nextSession;
    if (target) {
      this.navigateToSession(target);
    }
  }

  isSessionAvailable(session: LearningSessionSummary): boolean {
    return session.isPreviewAvailable === true || session.status === 'published' || session.status === 'review' || session.status === 'content-in-progress';
  }

  get previousSession(): LearningSessionSummary | undefined {
    if (!this.level || !this.manifestSession) {
      return undefined;
    }

    const index = this.level.sessions.findIndex(item => item.id === this.manifestSession?.id);
    return index > 0 ? this.level.sessions[index - 1] : undefined;
  }

  get nextSession(): LearningSessionSummary | undefined {
    if (!this.level || !this.manifestSession) {
      return undefined;
    }

    const index = this.level.sessions.findIndex(item => item.id === this.manifestSession?.id);
    const next = index >= 0 ? this.level.sessions[index + 1] : undefined;
    return next && this.isSessionAvailable(next) ? next : undefined;
  }

  asHeading(block: LearningContentBlock): HeadingBlock { return block as HeadingBlock; }
  asParagraph(block: LearningContentBlock): ParagraphBlock { return block as ParagraphBlock; }
  asList(block: LearningContentBlock): ListBlock { return block as ListBlock; }
  asDefinition(block: LearningContentBlock): DefinitionBlock { return block as DefinitionBlock; }
  asFormula(block: LearningContentBlock): FormulaBlock { return block as FormulaBlock; }
  asDiagram(block: LearningContentBlock): DiagramBlock { return block as DiagramBlock; }
  asTable(block: LearningContentBlock): TableBlock { return block as TableBlock; }
  asExample(block: LearningContentBlock): ExampleBlock { return block as ExampleBlock; }
  asNote(block: LearningContentBlock): NoteBlock { return block as NoteBlock; }
  asComparison(block: LearningContentBlock): ComparisonBlock { return block as ComparisonBlock; }
  asShloka(block: LearningContentBlock): ShlokaTriggerBlock { return block as ShlokaTriggerBlock; }

  formatCell(value: unknown): string {
    if (value === null || value === undefined) {
      return '';
    }

    return String(value);
  }

  renderGlossaryText(text: string): GlossaryTextSegment[] {
    if (!text || !this.glossaryMatchers.length) {
      return [{ text }];
    }

    const segments: GlossaryTextSegment[] = [];
    let index = 0;

    while (index < text.length) {
      const match = this.findGlossaryMatch(text, index);
      if (!match) {
        const previous = segments.at(-1);
        if (previous && !previous.entry) {
          previous.text += text[index];
        } else {
          segments.push({ text: text[index] });
        }
        index += 1;
        continue;
      }

      segments.push({
        text: text.slice(index, index + match.value.length),
        entry: match.entry
      });
      index += match.value.length;
    }

    return segments;
  }

  openGlossary(entry: LearningGlossaryEntry, event: MouseEvent): void {
    event.stopPropagation();
    this.activeGlossaryEntry = entry;

    const target = event.currentTarget as HTMLElement;
    const rect = target.getBoundingClientRect();
    const margin = 12;
    const maxTooltipWidth = Math.min(360, window.innerWidth - margin * 2);
    const preferredLeft = rect.left + rect.width / 2 - maxTooltipWidth / 2;

    this.glossaryTooltipLeft = Math.round(Math.min(
      Math.max(margin, preferredLeft),
      window.innerWidth - maxTooltipWidth - margin
    ));
    this.glossaryTooltipTop = Math.round(rect.bottom + 8);
  }

  closeGlossary(): void {
    this.activeGlossaryEntry = undefined;
  }

  glossaryTerm(entry: LearningGlossaryEntry, language: LearningLanguage): string {
    return entry.term?.[language] || entry.term?.en || entry.transliteration || entry.key;
  }

  glossaryDefinition(entry: LearningGlossaryEntry, language: LearningLanguage): string {
    return entry.definition?.[language] || entry.definition?.en || '';
  }
  private buildGlossaryMatchers(): void {
    const seen = new Set<string>();
    const matchers: GlossaryMatcher[] = [];

    for (const entry of this.glossary) {
      const candidates = [
        entry.term?.[this.resolvedLanguage],
        entry.term?.en,
        entry.term?.hi,
        entry.term?.kn,
        entry.transliteration
      ];

      for (const candidate of candidates) {
        const value = candidate?.trim();
        if (!value || value.length < 2) {
          continue;
        }

        const lowerValue = value.toLocaleLowerCase();
        if (seen.has(lowerValue)) {
          continue;
        }

        seen.add(lowerValue);
        matchers.push({ value, lowerValue, entry });
      }
    }

    this.glossaryMatchers = matchers.sort((left, right) => right.value.length - left.value.length);
  }

  private findGlossaryMatch(text: string, index: number): GlossaryMatcher | undefined {
    const remaining = text.slice(index).toLocaleLowerCase();
    return this.glossaryMatchers.find(matcher => {
      if (!remaining.startsWith(matcher.lowerValue)) {
        return false;
      }

      return this.isGlossaryBoundary(text[index - 1]) && this.isGlossaryBoundary(text[index + matcher.value.length]);
    });
  }

  private isGlossaryBoundary(character: string | undefined): boolean {
    return !character || !/[\p{L}\p{N}]/u.test(character);
  }

  private toLearningLanguage(language: string | null | undefined): LearningLanguage {
    return language === 'hi' || language === 'kn' ? language : 'en';
  }

  private getCurrentSectionId(): string | undefined {
    const hash = window.location.hash.replace('#', '').trim();
    if (hash) {
      return hash;
    }

    const sections = Array.from(document.querySelectorAll<HTMLElement>('.lesson-section[id]'));
    const current = sections
      .filter(section => section.getBoundingClientRect().top <= 120)
      .at(-1);
    return current?.id;
  }

  private restoreSection(sectionId: string | undefined): void {
    if (!sectionId) {
      return;
    }

    window.setTimeout(() => {
      document.getElementById(sectionId)?.scrollIntoView({ block: 'start' });
    });
  }
  private loadPage(sessionGuid: string): void {
    this.loadSession(sessionGuid);
  }

  private loadNavigationLevel(levelCode: string): void {
    if (!levelCode || this.level?.code === levelCode) {
      return;
    }

    this.learningService.getLevel(levelCode).subscribe({
      next: level => {
        this.level = level;
        this.manifestSession = level.sessions.find(item => item.id === this.session?.id) ?? this.manifestSession;
        this.cdr.markForCheck();
      },
      error: error => {
        console.error('Learning level navigation load error:', error);
      }
    });
  }

  private loadSession(sessionGuid: string, preserveSectionId?: string, keepCurrentSession = false): void {
    const requestId = ++this.sessionRequestId;
    this.errorMessage = '';
    this.notFound = false;
    this.usedFallback = false;
    this.activeGlossaryEntry = undefined;

    if (keepCurrentSession && this.session) {
      this.loading = false;
      this.refreshing = true;
    } else {
      this.loading = true;
      this.refreshing = false;
      this.session = undefined;
      this.manifestSession = this.level?.sessions.find(item => item.id === sessionGuid);
      this.glossary = [];
      this.glossaryMatchers = [];
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    this.learningService.getSession(sessionGuid, this.selectedLanguage).pipe(
      finalize(() => {
        if (requestId !== this.sessionRequestId) {
          return;
        }

        this.loading = false;
        this.refreshing = false;
        this.cdr.markForCheck();
      })
    ).subscribe({
      next: response => {
        if (requestId !== this.sessionRequestId) {
          return;
        }

        this.session = response.session;
        this.glossary = response.glossary ?? [];
        this.manifestSession = response.manifestSession;
        this.resolvedLanguage = response.resolvedLanguage;
        this.usedFallback = response.usedFallback;
        this.loadNavigationLevel(response.session.levelCode);
        this.buildGlossaryMatchers();
        this.restoreSection(preserveSectionId);

      this.cdr.markForCheck();
      },
      error: error => {
        if (requestId !== this.sessionRequestId) {
          return;
        }

        console.error('Learning session load error:', error);
        this.session = undefined;
        this.glossary = [];
        this.glossaryMatchers = [];
        this.activeGlossaryEntry = undefined;
        this.notFound = error?.status === 404 || error?.message?.includes('not found');
        this.errorMessage = this.notFound
          ? 'This session is not available yet.'
          : 'Unable to load this learning session right now.';


      this.cdr.markForCheck();
      }
    });
  }
}









