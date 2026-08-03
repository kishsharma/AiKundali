import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, DoCheck, HostBinding, HostListener, OnDestroy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';

import { DhruvaHouseContext, DhruvaKundaliContext, DhruvaKundaliField, DhruvaPlanetContext, DhruvaResultDetailContext } from './dhruva-kundali-context.mapper';
import { DhruvaKundaliContextService } from './dhruva-kundali-context.service';

interface DhruvaTourStep {
  id: string;
  titleKey: string;
  bodyKey: string;
  selector: string;
}

interface DhruvaGeneralQuestion {
  id: string;
  questionKey: string;
  answerKey: string;
}

interface DhruvaPersonalQuestion {
  id: string;
  questionKey: string;
  fieldKey?: keyof DhruvaKundaliContext;
  answerKey: string;
  unavailableKey: string;
  aliases: string[];
  planetExample?: { planet: string; fact: DhruvaPlanetFact };
  houseExample?: number;
}

interface DhruvaAnswer {
  key?: string;
  params?: Record<string, string>;
  kind?: 'story';
  title?: string;
  lines?: string[];
}

type DhruvaPlanetFact =
  | 'sign'
  | 'house'
  | 'degree'
  | 'nakshatraPada'
  | 'retrogradeStatus'
  | 'combustionStatus'
  | 'strength';

type DhruvaPanchangFieldKey =
  | 'panchangMaas'
  | 'panchangPaksha'
  | 'panchangTithi'
  | 'panchangNakshatraPada'
  | 'panchangYoga'
  | 'panchangKarana'
  | 'panchangVaar';

type DhruvaResultKind = 'yoga' | 'dosha';
type DhruvaHouseQuery =
  | { kind: 'house'; houseNumber: number }
  | { kind: 'strongest' }
  | { kind: 'support' }
  | { kind: 'ambiguous' };

interface DhruvaTourPosition {
  top: number;
  left: number;
  width: number;
  height: number;
}

const TOUR_COMPLETED_KEY = 'dhruva.portalTour.completed';
const REPORT_TOUR_COMPLETED_KEY = 'dhruva.kundaliReportTour.completed';
type DhruvaTourKind = 'portal' | 'kundali-report';

const PLANET_NAMES = ['sun', 'moon', 'mars', 'mercury', 'jupiter', 'venus', 'saturn', 'rahu', 'ketu'];
const HOUSE_LIFE_AREA_ALIASES: Array<{ houseNumber: number; aliases: string[] }> = [
  { houseNumber: 2, aliases: ['wealth', 'money', 'finance', 'family wealth'] },
  { houseNumber: 3, aliases: ['siblings', 'brother', 'sister', 'courage'] },
  { houseNumber: 4, aliases: ['property', 'home', 'mother', 'land'] },
  { houseNumber: 5, aliases: ['children', 'child', 'education', 'creativity'] },
  { houseNumber: 7, aliases: ['marriage', 'spouse', 'partner', 'relationship'] },
  { houseNumber: 10, aliases: ['career', 'profession', 'job', 'work'] },
  { houseNumber: 11, aliases: ['gains', 'income', 'network'] },
];

const AMBIGUOUS_HOUSE_TERMS = ['growth'];

const PLANET_FACT_ALIASES: Array<{ fact: DhruvaPlanetFact; aliases: string[] }> = [
  { fact: 'nakshatraPada', aliases: ['nakshatra pada', 'nakshatra and pada', 'nakshatra', 'pada'] },
  { fact: 'retrogradeStatus', aliases: ['retrograde', 'vakri'] },
  { fact: 'combustionStatus', aliases: ['combustion', 'combust', 'asta'] },
  { fact: 'strength', aliases: ['strength label', 'strength', 'strong'] },
  { fact: 'degree', aliases: ['degree', 'degrees'] },
  { fact: 'house', aliases: ['house', 'bhava'] },
  { fact: 'sign', aliases: ['sign', 'rashi'] },
];

const PLANET_FACT_LABELS: Record<DhruvaPlanetFact, string> = {
  sign: 'sign',
  house: 'house',
  degree: 'degree',
  nakshatraPada: 'Nakshatra and Pada',
  retrogradeStatus: 'retrograde status',
  combustionStatus: 'combustion status',
  strength: 'strength label',
};

const SIGN_DISPLAY_BY_NUMBER: Record<string, string> = {
  '1': 'Aries (Mesha)',
  '2': 'Taurus (Vrishabha)',
  '3': 'Gemini (Mithuna)',
  '4': 'Cancer (Karka)',
  '5': 'Leo (Simha)',
  '6': 'Virgo (Kanya)',
  '7': 'Libra (Tula)',
  '8': 'Scorpio (Vrischika)',
  '9': 'Sagittarius (Dhanu)',
  '10': 'Capricorn (Makara)',
  '11': 'Aquarius (Kumbha)',
  '12': 'Pisces (Meena)',
};

const SIGN_DISPLAY_BY_NAME: Record<string, string> = {
  aries: 'Aries (Mesha)',
  mesha: 'Aries (Mesha)',
  taurus: 'Taurus (Vrishabha)',
  vrishabha: 'Taurus (Vrishabha)',
  gemini: 'Gemini (Mithuna)',
  mithuna: 'Gemini (Mithuna)',
  cancer: 'Cancer (Karka)',
  karka: 'Cancer (Karka)',
  leo: 'Leo (Simha)',
  simha: 'Leo (Simha)',
  virgo: 'Virgo (Kanya)',
  kanya: 'Virgo (Kanya)',
  libra: 'Libra (Tula)',
  tula: 'Libra (Tula)',
  scorpio: 'Scorpio (Vrischika)',
  vrischika: 'Scorpio (Vrischika)',
  sagittarius: 'Sagittarius (Dhanu)',
  dhanu: 'Sagittarius (Dhanu)',
  capricorn: 'Capricorn (Makara)',
  makara: 'Capricorn (Makara)',
  aquarius: 'Aquarius (Kumbha)',
  kumbha: 'Aquarius (Kumbha)',
  pisces: 'Pisces (Meena)',
  meena: 'Pisces (Meena)',
};

@Component({
  selector: 'app-dhruva-assistant',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslocoPipe],
  templateUrl: './dhruva-assistant.component.html',
  styleUrl: './dhruva-assistant.component.scss',
})
export class DhruvaAssistantComponent implements AfterViewInit, DoCheck, OnDestroy {
  constructor(
    private readonly kundaliContextService: DhruvaKundaliContextService,
    private readonly changeDetector: ChangeDetectorRef = { markForCheck: () => undefined } as ChangeDetectorRef,
  ) {}
  isOpen = false;
  isExpanded = false;
  isTourOpen = false;
  isGeneralQuestionsOpen = false;
  currentTourIndex = 0;
  activeTourKind: DhruvaTourKind = 'portal';
  activePortalTourSteps: DhruvaTourStep[] = [];
  activeReportTourSteps: DhruvaTourStep[] = [];
  tourPosition: DhruvaTourPosition | null = null;
  selectedGeneralQuestionId = 'kundali';
  selectedPersonalQuestionId: string | null = null;
  selectedStoryActive = false;
  typedQuestion = '';
  askInputTouched = false;
  typedAnswer: DhruvaAnswer | null = null;
  latestAnswer: DhruvaAnswer | null = null;
  isTypingResponse = false;
  responseVisibleLength = Number.MAX_SAFE_INTEGER;
  private responseTypingStartTimer: ReturnType<typeof setTimeout> | null = null;
  private responseTypingInterval: ReturnType<typeof setInterval> | null = null;
  private hadKundaliContext = false;

  readonly generalQuestions: DhruvaGeneralQuestion[] = [
    {
      id: 'kundali',
      questionKey: 'dhruva.general.kundaliQuestion',
      answerKey: 'dhruva.general.kundaliAnswer',
    },
    {
      id: 'ascendant',
      questionKey: 'dhruva.general.ascendantQuestion',
      answerKey: 'dhruva.general.ascendantAnswer',
    },
    {
      id: 'nakshatra',
      questionKey: 'dhruva.general.nakshatraQuestion',
      answerKey: 'dhruva.general.nakshatraAnswer',
    },
    {
      id: 'dasha',
      questionKey: 'dhruva.general.dashaQuestion',
      answerKey: 'dhruva.general.dashaAnswer',
    },
    {
      id: 'mahadasha-antardasha',
      questionKey: 'dhruva.general.mahadashaAntardashaQuestion',
      answerKey: 'dhruva.general.mahadashaAntardashaAnswer',
    },
    {
      id: 'yoga',
      questionKey: 'dhruva.general.yogaQuestion',
      answerKey: 'dhruva.general.yogaAnswer',
    },
    {
      id: 'dosha',
      questionKey: 'dhruva.general.doshaQuestion',
      answerKey: 'dhruva.general.doshaAnswer',
    },
  ];


  readonly personalQuestions: DhruvaPersonalQuestion[] = [
    {
      id: 'ascendant',
      questionKey: 'dhruva.personal.ascendantQuestion',
      fieldKey: 'ascendant',
      answerKey: 'dhruva.personal.ascendantAnswer',
      unavailableKey: 'dhruva.personal.ascendantUnavailableAnswer',
      aliases: ['ascendant', 'lagna', 'rising sign'],
    },
    {
      id: 'moon-sign',
      questionKey: 'dhruva.personal.moonSignQuestion',
      fieldKey: 'moonSign',
      answerKey: 'dhruva.personal.moonSignAnswer',
      unavailableKey: 'dhruva.personal.moonSignUnavailableAnswer',
      aliases: ['moon sign', 'moon rashi', 'chandra rashi', 'rashi'],
    },
    {
      id: 'sun-sign',
      questionKey: 'dhruva.personal.sunSignQuestion',
      fieldKey: 'sunSign',
      answerKey: 'dhruva.personal.sunSignAnswer',
      unavailableKey: 'dhruva.personal.sunSignUnavailableAnswer',
      aliases: ['sun sign', 'surya rashi', 'sun rashi'],
    },
    {
      id: 'nakshatra',
      questionKey: 'dhruva.personal.nakshatraQuestion',
      fieldKey: 'birthNakshatra',
      answerKey: 'dhruva.personal.nakshatraAnswer',
      unavailableKey: 'dhruva.personal.nakshatraUnavailableAnswer',
      aliases: ['nakshatra', 'birth nakshatra', 'birth star', 'janma nakshatra'],
    },
    {
      id: 'mahadasha',
      questionKey: 'dhruva.personal.mahadashaQuestion',
      fieldKey: 'currentMahadasha',
      answerKey: 'dhruva.personal.mahadashaAnswer',
      unavailableKey: 'dhruva.personal.mahadashaUnavailableAnswer',
      aliases: ['mahadasha', 'maha dasha', 'current mahadasha'],
    },
    {
      id: 'antardasha',
      questionKey: 'dhruva.personal.antardashaQuestion',
      fieldKey: 'currentAntardasha',
      answerKey: 'dhruva.personal.antardashaAnswer',
      unavailableKey: 'dhruva.personal.antardashaUnavailableAnswer',
      aliases: ['antardasha', 'antar dasha', 'current antardasha'],
    },
    {
      id: 'present-yogas',
      questionKey: 'dhruva.personal.presentYogasQuestion',
      fieldKey: 'presentYogas',
      answerKey: 'dhruva.personal.presentYogasAnswer',
      unavailableKey: 'dhruva.personal.presentYogasUnavailableAnswer',
      aliases: ['yogas present', 'yoga present', 'what yogas', 'which yogas', 'my yogas'],
    },
    {
      id: 'present-doshas',
      questionKey: 'dhruva.personal.presentDoshasQuestion',
      fieldKey: 'presentDoshas',
      answerKey: 'dhruva.personal.presentDoshasAnswer',
      unavailableKey: 'dhruva.personal.presentDoshasUnavailableAnswer',
      aliases: ['doshas present', 'dosha present', 'what doshas', 'which doshas', 'my doshas'],
    },
    {
      id: 'strongest-planets',
      questionKey: 'dhruva.personal.strongestPlanetsQuestion',
      fieldKey: 'strongestPlanets',
      answerKey: 'dhruva.personal.strongestPlanetsAnswer',
      unavailableKey: 'dhruva.personal.strongestPlanetsUnavailableAnswer',
      aliases: ['strongest planets', 'which planets are strongest', 'strong planets', 'planet strength'],
    },
    {
      id: 'panchang-tithi',
      questionKey: 'dhruva.personal.panchangTithiQuestion',
      fieldKey: 'panchangTithi',
      answerKey: 'dhruva.personal.panchangFieldAnswer',
      unavailableKey: 'dhruva.personal.panchangUnavailableAnswer',
      aliases: ['tithi', 'my tithi', 'birth tithi'],
    },
    {
      id: 'panchang-yoga',
      questionKey: 'dhruva.personal.panchangYogaQuestion',
      fieldKey: 'panchangYoga',
      answerKey: 'dhruva.personal.panchangFieldAnswer',
      unavailableKey: 'dhruva.personal.panchangUnavailableAnswer',
      aliases: ['panchang yoga', 'birth yoga'],
    },
    {
      id: 'moon-placement',
      questionKey: 'dhruva.personal.moonPlacementQuestion',
      answerKey: 'dhruva.personal.planetPlacementAnswer',
      unavailableKey: 'dhruva.personal.planetFactUnavailableAnswer',
      aliases: ['where is moon placed', 'moon placement'],
      planetExample: { planet: 'Moon', fact: 'sign' },
    },
    {
      id: 'saturn-retrograde',
      questionKey: 'dhruva.personal.saturnRetrogradeQuestion',
      answerKey: 'dhruva.personal.planetFactAnswer',
      unavailableKey: 'dhruva.personal.planetFactUnavailableAnswer',
      aliases: ['is saturn retrograde', 'saturn retrograde'],
      planetExample: { planet: 'Saturn', fact: 'retrogradeStatus' },
    },
  ];
  readonly tourSteps: DhruvaTourStep[] = [
    {
      id: 'dashboard',
      titleKey: 'dhruva.tour.dashboardTitle',
      bodyKey: 'dhruva.tour.dashboardBody',
      selector: '[data-tour-id="portal-dashboard"]',
    },
    {
      id: 'create-kundali',
      titleKey: 'dhruva.tour.createKundaliTitle',
      bodyKey: 'dhruva.tour.createKundaliBody',
      selector: '[data-tour-id="portal-create-kundali"]',
    },
    {
      id: 'saved-kundalis',
      titleKey: 'dhruva.tour.savedKundalisTitle',
      bodyKey: 'dhruva.tour.savedKundalisBody',
      selector: '[data-tour-id="portal-saved-kundalis"]',
    },
    {
      id: 'compatibility',
      titleKey: 'dhruva.tour.compatibilityTitle',
      bodyKey: 'dhruva.tour.compatibilityBody',
      selector: '[data-tour-id="portal-compatibility"]',
    },
    {
      id: 'muhurtha',
      titleKey: 'dhruva.tour.muhurthaTitle',
      bodyKey: 'dhruva.tour.muhurthaBody',
      selector: '[data-tour-id="portal-muhurtha"]',
    },
    {
      id: 'learning',
      titleKey: 'dhruva.tour.learningTitle',
      bodyKey: 'dhruva.tour.learningBody',
      selector: '[data-tour-id="portal-learning"]',
    },
    {
      id: 'dhruva-assistant',
      titleKey: 'dhruva.tour.assistantTitle',
      bodyKey: 'dhruva.tour.assistantBody',
      selector: '[data-tour-id="portal-dhruva-assistant"]',
    },
  ];

  readonly kundaliReportTourSteps: DhruvaTourStep[] = [
    {
      id: 'report-download',
      titleKey: 'dhruva.reportTour.downloadTitle',
      bodyKey: 'dhruva.reportTour.downloadBody',
      selector: '[data-tour-id="kundali-report-actions"]',
    },
    {
      id: 'chart-summary',
      titleKey: 'dhruva.reportTour.summaryTitle',
      bodyKey: 'dhruva.reportTour.summaryBody',
      selector: '[data-tour-id="kundali-report-summary"]',
    },
    {
      id: 'planetary-positions',
      titleKey: 'dhruva.reportTour.planetsTitle',
      bodyKey: 'dhruva.reportTour.planetsBody',
      selector: '[data-tour-id="kundali-report-planets"]',
    },
    {
      id: 'dasha',
      titleKey: 'dhruva.reportTour.dashaTitle',
      bodyKey: 'dhruva.reportTour.dashaBody',
      selector: '[data-tour-id="kundali-report-dasha"]',
    },
    {
      id: 'yoga',
      titleKey: 'dhruva.reportTour.yogaTitle',
      bodyKey: 'dhruva.reportTour.yogaBody',
      selector: '[data-tour-id="kundali-report-yogas"]',
    },
    {
      id: 'dosha',
      titleKey: 'dhruva.reportTour.doshaTitle',
      bodyKey: 'dhruva.reportTour.doshaBody',
      selector: '[data-tour-id="kundali-report-doshas"]',
    },
    {
      id: 'planetary-strength',
      titleKey: 'dhruva.reportTour.strengthTitle',
      bodyKey: 'dhruva.reportTour.strengthBody',
      selector: '[data-tour-id="kundali-report-strength"]',
    },
  ];
  ngAfterViewInit(): void {
    if (!this.hasCompletedTour()) {
      window.setTimeout(() => this.startTour(), 350);
    }
  }
  ngDoCheck(): void {
    const hasContext = this.hasKundaliContext;

    if (hasContext && !this.hadKundaliContext && typeof window !== 'undefined') {
      window.setTimeout(() => this.tryStartKundaliReportTour(), 450);
    }

    this.hadKundaliContext = hasContext;
  }

  ngOnDestroy(): void {
    this.clearResponseTypingTimers();
    this.clearTargetHighlight();
  }

  @HostBinding('class.dhruva-tour-host-open')
  get isTourHostOpen(): boolean {
    return this.isTourOpen;
  }

  get kundaliContext(): DhruvaKundaliContext | null {
    return this.kundaliContextService.context();
  }

  get hasKundaliContext(): boolean {
    return this.kundaliContext !== null;
  }

  get shouldShowGeneralQuestions(): boolean {
    return !this.hasKundaliContext || this.isGeneralQuestionsOpen;
  }

  get selectedPersonalAnswer(): DhruvaAnswer | null {
    const question = this.personalQuestions.find((item) => item.id === this.selectedPersonalQuestionId);
    return question ? this.answerPersonalQuestion(question) : null;
  }

  get displayedPersonalAnswer(): DhruvaAnswer | null {
    return this.displayedAnswer;
  }

  get displayedAnswer(): DhruvaAnswer | null {
    return this.typedAnswer ?? this.latestAnswer ?? { key: this.selectedGeneralQuestion.answerKey };
  }

  get hasAskInputError(): boolean {
    return this.askInputTouched && !this.typedQuestion.trim();
  }

  get selectedGeneralQuestion(): DhruvaGeneralQuestion {
    return this.generalQuestions.find((question) => question.id === this.selectedGeneralQuestionId)
      ?? this.generalQuestions[0];
  }

  get activeTourSteps(): DhruvaTourStep[] {
    return this.activeTourKind === 'kundali-report' ? this.activeReportTourSteps : this.activePortalTourSteps;
  }

  get currentTourStep(): DhruvaTourStep {
    return this.activeTourSteps[this.currentTourIndex] ?? this.activeTourSteps[0] ?? this.tourSteps[0];
  }

  get isFirstTourStep(): boolean {
    return this.currentTourIndex === 0;
  }

  get isLastTourStep(): boolean {
    return this.currentTourIndex === this.activeTourSteps.length - 1;
  }

  open(): void {
    this.isOpen = true;
  }

  close(): void {
    this.isOpen = false;
    this.isExpanded = false;
  }

  toggle(): void {
    this.isOpen = !this.isOpen;

    if (!this.isOpen) {
      this.isExpanded = false;
    }
  }

  expand(): void {
    if (!this.isOpen) {
      return;
    }

    this.isExpanded = true;
  }

  restoreCompact(): void {
    this.isExpanded = false;
  }
  toggleGeneralQuestions(): void {
    this.isGeneralQuestionsOpen = !this.isGeneralQuestionsOpen;
  }

  selectGeneralQuestion(questionId: string): void {
    this.selectedGeneralQuestionId = questionId;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = false;
    this.typedAnswer = null;
    this.latestAnswer = { key: this.selectedGeneralQuestion.answerKey };
    this.beginResponseReveal();
  }

  selectPersonalQuestion(questionId: string): void {
    if (!this.hasKundaliContext) {
      return;
    }

    this.selectedPersonalQuestionId = questionId;
    this.selectedStoryActive = false;
    this.typedAnswer = null;
    this.latestAnswer = this.selectedPersonalAnswer;
    this.beginResponseReveal();
  }


  tellJanmaStory(): void {
    if (!this.hasKundaliContext) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.noKundaliAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    this.isOpen = true;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = true;
    this.typedAnswer = null;
    this.latestAnswer = this.answerJanmaStory();
    this.beginResponseReveal();
  }
  submitTypedQuestion(): void {
    const trimmedQuestion = this.typedQuestion.trim();

    if (!trimmedQuestion) {
      this.typedQuestion = '';
      this.askInputTouched = true;
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.emptyQuestionAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    this.askInputTouched = false;
    this.typedQuestion = trimmedQuestion;

    const wantsStory = this.matchStoryQuestion(this.typedQuestion);

    if (wantsStory) {
      this.tellJanmaStory();
      return;
    }

    const panchangFieldKey = this.matchTypedPanchangField(this.typedQuestion);
    const houseQuery = panchangFieldKey ? null : this.matchTypedHouseQuestion(this.typedQuestion);
    const resultDetail = panchangFieldKey || houseQuery ? null : this.matchTypedResultDetail(this.typedQuestion);
    const match = panchangFieldKey || houseQuery || resultDetail ? null : this.matchPersonalQuestion(this.typedQuestion);
    const planetFact = match || panchangFieldKey || houseQuery || resultDetail ? null : this.matchTypedPlanetFact(this.typedQuestion);

    if (houseQuery?.kind === 'ambiguous') {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.ambiguousHouseAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    if (!match && !panchangFieldKey && !planetFact && !houseQuery && !resultDetail) {
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.unsupportedAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    if (!this.hasKundaliContext) {
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.noKundaliAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    if (panchangFieldKey) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = this.answerPanchangField(panchangFieldKey);
    } else if (houseQuery) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = this.answerHouseQuestion(houseQuery);
    } else if (resultDetail) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = this.answerResultDetail(resultDetail.kind, resultDetail.result);
    } else if (planetFact) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = this.answerPlanetFact(planetFact.planet, planetFact.fact);
    } else if (match) {
      this.selectedPersonalQuestionId = match.id;
      this.selectedStoryActive = false;
      this.typedAnswer = this.answerPersonalQuestion(match);
    }

    this.latestAnswer = this.typedAnswer ?? { key: 'dhruva.personal.unsupportedAnswer' };
    this.beginResponseReveal();
  }
  animatedResponseText(value: string): string {
    if (!value || this.isReducedMotionPreferred()) {
      return value;
    }

    return value.slice(0, Math.min(this.responseVisibleLength, value.length));
  }
  startTour(): void {
    const visibleSteps = this.visibleTourSteps(this.tourSteps);

    if (!visibleSteps.length) {
      return;
    }

    this.activeTourKind = 'portal';
    this.activePortalTourSteps = visibleSteps;
    this.activeReportTourSteps = [];
    this.isOpen = false;
    this.isTourOpen = true;
    this.currentTourIndex = 0;
    this.updateTourPosition(true);
  }

  startKundaliReportTour(): void {
    this.tryStartKundaliReportTour(true);
  }

  nextTourStep(): void {
    if (this.isLastTourStep) {
      this.finishTour();
      return;
    }

    this.currentTourIndex += 1;
    this.updateTourPosition(true);
  }

  @HostListener('window:resize')
  @HostListener('window:scroll')
  onTourViewportChange(): void {
    if (this.isTourOpen) {
      this.updateTourPosition();
    }
  }

  previousTourStep(): void {
    if (this.isFirstTourStep) {
      return;
    }

    this.currentTourIndex -= 1;
    this.updateTourPosition(true);
  }

  skipTour(): void {
    this.completeTour();
  }

  finishTour(): void {
    this.completeTour();
  }


  private beginResponseReveal(): void {
    this.clearResponseTypingTimers();

    if (this.isReducedMotionPreferred()) {
      this.isTypingResponse = false;
      this.responseVisibleLength = Number.MAX_SAFE_INTEGER;
      this.markResponseAnimationForCheck();
      return;
    }

    this.isTypingResponse = true;
    this.responseVisibleLength = 0;
    this.markResponseAnimationForCheck();
    this.scrollLatestResponseIntoView();
    this.responseTypingStartTimer = setTimeout(() => {
      this.responseTypingStartTimer = null;
      this.isTypingResponse = false;
      this.markResponseAnimationForCheck();
      this.responseTypingInterval = setInterval(() => {
        this.responseVisibleLength += 4;
        this.markResponseAnimationForCheck();

        if (this.responseVisibleLength >= 600) {
          this.clearResponseTypingTimers(false);
        }
      }, 18);
    }, 180);
  }

  private scrollLatestResponseIntoView(): void {
    if (typeof window === 'undefined' || typeof document === 'undefined') {
      return;
    }

    window.setTimeout(() => {
      document.querySelector<HTMLElement>('[data-dhruva-response]')?.scrollIntoView({
        block: 'nearest',
        inline: 'nearest',
      });
    });
  }

  private clearResponseTypingTimers(resetState = true): void {
    if (this.responseTypingStartTimer) {
      clearTimeout(this.responseTypingStartTimer);
      this.responseTypingStartTimer = null;
    }

    if (this.responseTypingInterval) {
      clearInterval(this.responseTypingInterval);
      this.responseTypingInterval = null;
    }

    if (resetState) {
      this.isTypingResponse = false;
      this.responseVisibleLength = Number.MAX_SAFE_INTEGER;
      this.markResponseAnimationForCheck();
    }
  }

  private markResponseAnimationForCheck(): void {
    this.changeDetector.markForCheck();
  }

  private isReducedMotionPreferred(): boolean {
    return typeof window !== 'undefined'
      && typeof window.matchMedia === 'function'
      && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }
  private answerJanmaStory(): DhruvaAnswer {
    const context = this.kundaliContext;
    const ascendant = this.availableField(context?.ascendant) ?? 'the rising sign still opening';
    const moonSign = this.availableField(context?.moonSign);
    const nakshatra = this.availableField(context?.birthNakshatra);
    const mahadasha = this.availableField(context?.currentMahadasha);
    const antardasha = this.availableField(context?.currentAntardasha);
    const yogas = this.availableField(context?.presentYogas);
    const doshas = this.availableField(context?.presentDoshas);
    const strongestPlanets = this.availableField(context?.strongestPlanets);
    const strongestHouses = this.availableField(context?.strongestHouses);
    const supportHouses = this.availableField(context?.supportNeededHouses);

    const mindSignature = [moonSign, nakshatra].filter(Boolean).join(' with ');
    const currentChapter = [mahadasha, antardasha].filter(Boolean).join(' / ');

    const lines = [
      `Your Janma story opens with ${this.formatSignValue(ascendant)} shaping the first response to life, temperament, instinct, and the way effort begins.`,
      mindSignature
        ? `The inner rhythm carries ${mindSignature}, so emotion, memory, and timing become important keys in how this chart receives life.`
        : 'The Moon and nakshatra layer will add the emotional rhythm of the chart as more birth markers become available.',
      strongestPlanets
        ? `A visible support comes through ${strongestPlanets}; these planets behave like guiding lamps when their energy is used with discipline.`
        : 'Planetary support is still unfolding, but the chart will show which forces act as guides and which ask for careful handling.',
      strongestHouses
        ? `Strength gathers around ${strongestHouses}, suggesting areas where effort can become visible progress.`
        : supportHouses
          ? `Some care is needed around ${supportHouses}; this is not weakness, but a place where maturity creates stability.`
          : 'The house pattern shows where life asks for effort, where support arrives, and where maturity gradually becomes strength.',
      yogas || doshas
        ? `The karmic subplot includes ${[yogas, doshas].filter(Boolean).join(' along with ')}, adding grace, friction, and turning points to the reading.`
        : 'Yogas and doshas become the subplots of the chart, describing where grace, friction, and turning points may appear.',
      currentChapter
        ? `The current chapter is coloured by ${currentChapter}, so the same birth chart is now expressing itself through that planetary period.`
        : 'The dasha sequence will tell which chapter is active now and which part of the chart is asking to be lived.',
    ];

    return {
      kind: 'story',
      title: 'Your Janma Story',
      lines,
    };
  }

  private availableField(field: DhruvaKundaliField | undefined): string | null {
    return field?.status === 'available' && field.value ? field.value : null;
  }

  private matchStoryQuestion(input: string): boolean {
    const normalized = this.normalizeQuestion(input);
    return ['tell me my story', 'my story', 'janma story', 'birth story', 'chart story', 'kundali story']
      .some((term) => normalized.includes(this.normalizeQuestion(term)));
  }
  private answerPersonalQuestion(question: DhruvaPersonalQuestion): DhruvaAnswer {
    if (question.planetExample) {
      return this.answerPlanetFact(question.planetExample.planet, question.planetExample.fact, question.answerKey);
    }

    if (!question.fieldKey) {
      return { key: question.unavailableKey };
    }

    const field = this.kundaliContext?.[question.fieldKey] as DhruvaKundaliField | undefined;
    const label = this.fieldLabel(question.fieldKey);

    if (!field || field.status === 'missing' || !field.value) {
      return label ? { key: question.unavailableKey, params: { label } } : { key: question.unavailableKey };
    }

    const params: Record<string, string> = { value: this.formatAnswerValue(question, field.value) };

    if (label) {
      params['label'] = label;
    }

    return { key: question.answerKey, params };
  }
  private matchTypedPanchangField(input: string): DhruvaPanchangFieldKey | null {
    const normalized = this.normalizeQuestion(input);
    const matches = (aliases: string[]) => aliases.some((alias) => normalized.includes(this.normalizeQuestion(alias)));

    if (matches(['nakshatra pada', 'panchang nakshatra pada'])) return 'panchangNakshatraPada';
    if (matches(['panchang yoga', 'birth yoga'])) return 'panchangYoga';
    if (matches(['maas', 'masa', 'birth month'])) return 'panchangMaas';
    if (matches(['paksha'])) return 'panchangPaksha';
    if (matches(['tithi'])) return 'panchangTithi';
    if (matches(['karana'])) return 'panchangKarana';
    if (matches(['vaar', 'vara', 'weekday', 'birth day'])) return 'panchangVaar';

    return null;
  }

  private matchTypedPlanetFact(input: string): { planet: string; fact: DhruvaPlanetFact } | null {
    const normalized = this.normalizeQuestion(input);
    const planet = PLANET_NAMES.find((name) => normalized.includes(name));
    const fact = PLANET_FACT_ALIASES.find((item) =>
      item.aliases.some((alias) => normalized.includes(this.normalizeQuestion(alias)))
    )?.fact;

    return planet && fact ? { planet, fact } : null;
  }

  private matchTypedResultDetail(input: string): { kind: DhruvaResultKind; result: DhruvaResultDetailContext } | null {
    const normalized = this.normalizeQuestion(input);
    const kind: DhruvaResultKind | null = normalized.includes('dosha') ? 'dosha' : normalized.includes('yoga') ? 'yoga' : null;

    if (!kind || !this.kundaliContext) {
      return null;
    }

    const results = kind === 'yoga' ? this.kundaliContext.yogas : this.kundaliContext.doshas;
    const detailTerms = ['detail', 'details', 'description', 'reason', 'category', 'strength', 'severity', 'planet', 'house', 'impact', 'activation', 'period', 'tell me about'];
    const result = results.find((item) => normalized.includes(this.normalizeQuestion(item.name)))
      ?? (detailTerms.some((term) => normalized.includes(term)) && results.length === 1 ? results[0] : null);

    return result ? { kind, result } : null;
  }

  private matchTypedHouseQuestion(input: string): DhruvaHouseQuery | null {
    const normalized = this.normalizeQuestion(input);

    if (AMBIGUOUS_HOUSE_TERMS.some((term) => normalized.includes(term))) {
      return { kind: 'ambiguous' };
    }

    if (normalized.includes('strongest house') || normalized.includes('strongest houses')) {
      return { kind: 'strongest' };
    }

    if (normalized.includes('need more support') || normalized.includes('weak house') || normalized.includes('challenging house')) {
      return { kind: 'support' };
    }

    const numericHouse = this.houseNumberFromQuestion(normalized);
    if (numericHouse) {
      return { kind: 'house', houseNumber: numericHouse };
    }

    const aliasMatch = HOUSE_LIFE_AREA_ALIASES.find((item) =>
      item.aliases.some((alias) => normalized.includes(this.normalizeQuestion(alias)))
    );

    return aliasMatch ? { kind: 'house', houseNumber: aliasMatch.houseNumber } : null;
  }

  private answerResultDetail(kind: DhruvaResultKind, result: DhruvaResultDetailContext): DhruvaAnswer {
    const details = [
      this.resultLine('Category', result.category),
      this.resultLine(kind === 'dosha' ? 'Severity' : 'Strength', result.strengthOrSeverity),
      this.resultLine('Reason', result.reason),
      this.resultLine('Description', result.description),
      this.resultLine('Planets', result.involvedPlanets),
      this.resultLine('Houses', result.involvedHouses),
      this.resultLine('Impact areas', result.impactAreas),
      this.resultLine('Activation period', result.activationPeriod),
    ].filter(Boolean).slice(0, 5);

    if (!details.length) {
      return { key: 'dhruva.personal.resultDetailUnavailableAnswer', params: { result: result.name } };
    }

    return {
      key: 'dhruva.personal.resultDetailAnswer',
      params: { result: result.name, kind: kind === 'dosha' ? 'Dosha' : 'Yoga', value: details.join(' ') },
    };
  }

  private answerHouseQuestion(query: DhruvaHouseQuery): DhruvaAnswer {
    if (query.kind === 'strongest') {
      return this.answerHouseSummary(this.kundaliContext?.strongestHouses, 'strongest houses');
    }

    if (query.kind === 'support') {
      return this.answerHouseSummary(this.kundaliContext?.supportNeededHouses, 'houses needing more support');
    }

    if (query.kind === 'ambiguous') {
      return { key: 'dhruva.personal.ambiguousHouseAnswer' };
    }

    const house = this.kundaliContext?.houses.find((item) => item.houseNumber === query.houseNumber) ?? null;

    if (!house) {
      return { key: 'dhruva.personal.houseUnavailableAnswer', params: { house: `${query.houseNumber}${this.ordinalSuffix(query.houseNumber)} house` } };
    }

    const details = [
      this.resultLine('Sign', house.sign),
      this.resultLine('Lord', house.lord),
      this.resultLine('Occupants', house.occupants),
      this.resultLine('Score', house.score),
      this.resultLine('Status', house.status),
      this.resultLine('Themes', house.themes),
      this.resultLine('Life areas', house.lifeAreas),
      this.resultLine('Support', house.supportingFactors),
      this.resultLine('Pressure', house.pressureFactors),
    ].filter(Boolean).slice(0, 5);

    if (!details.length) {
      return { key: 'dhruva.personal.houseUnavailableAnswer', params: { house: `${house.houseNumber}${this.ordinalSuffix(house.houseNumber)} house` } };
    }

    return {
      key: 'dhruva.personal.houseAnswer',
      params: { house: `${house.houseNumber}${this.ordinalSuffix(house.houseNumber)} house (${house.label})`, value: details.join(' ') },
    };
  }

  private answerHouseSummary(field: DhruvaKundaliField | undefined, label: string): DhruvaAnswer {
    if (!field || field.status === 'missing' || !field.value) {
      return { key: 'dhruva.personal.housesUnavailableAnswer', params: { label } };
    }

    return { key: 'dhruva.personal.housesSummaryAnswer', params: { label, value: field.value } };
  }

  private resultLine(label: string, field: DhruvaKundaliField): string {
    if (field.status !== 'available' || !field.value) {
      return '';
    }

    return `${label}: ${field.value.replace(/[.ã€‚]+$/, '')}.`;
  }
  private houseNumberFromQuestion(normalized: string): number | null {
    const ordinalMatch = normalized.match(/\b(1st|2nd|3rd|[4-9]th|10th|11th|12th|first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth|eleventh|twelfth|[1-9]|1[0-2]) house\b/);
    const value = ordinalMatch?.[1];
    const words: Record<string, number> = {
      first: 1,
      second: 2,
      third: 3,
      fourth: 4,
      fifth: 5,
      sixth: 6,
      seventh: 7,
      eighth: 8,
      ninth: 9,
      tenth: 10,
      eleventh: 11,
      twelfth: 12,
    };
    const number = value ? (words[value] ?? Number(value.replace(/\D/g, ''))) : NaN;
    return Number.isFinite(number) && number >= 1 && number <= 12 ? number : null;
  }

  private ordinalSuffix(value: number): string {
    if ([11, 12, 13].includes(value % 100)) return 'th';
    if (value % 10 === 1) return 'st';
    if (value % 10 === 2) return 'nd';
    if (value % 10 === 3) return 'rd';
    return 'th';
  }
  private answerPanchangField(fieldKey: DhruvaPanchangFieldKey): DhruvaAnswer {
    const field = this.kundaliContext?.[fieldKey] as DhruvaKundaliField | undefined;
    const label = this.fieldLabel(fieldKey);

    if (!field || field.status === 'missing' || !field.value) {
      return { key: 'dhruva.personal.panchangUnavailableAnswer', params: { label } };
    }

    return { key: 'dhruva.personal.panchangFieldAnswer', params: { label, value: field.value } };
  }

  private answerPlanetFact(planetName: string, fact: DhruvaPlanetFact, answerKey = 'dhruva.personal.planetFactAnswer'): DhruvaAnswer {
    const planet = this.findPlanet(planetName);
    const field = planet?.[fact] as DhruvaKundaliField | undefined;
    const planetLabel = this.displayPlanetName(planetName);
    const factLabel = PLANET_FACT_LABELS[fact];

    if (!planet || !field || field.status === 'missing' || !field.value) {
      return { key: 'dhruva.personal.planetFactUnavailableAnswer', params: { planet: planetLabel, fact: factLabel } };
    }

    const value = fact === 'sign' ? this.formatSignValue(field.value) : field.value;

    return {
      key: answerKey,
      params: { planet: this.displayPlanetName(planet.name), fact: factLabel, value },
    };
  }

  private findPlanet(planetName: string): DhruvaPlanetContext | null {
    const normalizedPlanet = this.normalizeQuestion(planetName);
    return this.kundaliContext?.planets.find((planet) => this.normalizeQuestion(planet.name) === normalizedPlanet) ?? null;
  }

  private displayPlanetName(planetName: string): string {
    return planetName.charAt(0).toUpperCase() + planetName.slice(1).toLowerCase();
  }

  private fieldLabel(fieldKey: keyof DhruvaKundaliContext): string {
    const labels: Partial<Record<keyof DhruvaKundaliContext, string>> = {
      panchangMaas: 'Maas',
      panchangPaksha: 'Paksha',
      panchangTithi: 'Tithi',
      panchangNakshatraPada: 'Nakshatra Pada',
      panchangYoga: 'Panchang Yoga',
      panchangKarana: 'Karana',
      panchangVaar: 'Vaar',
    };

    return labels[fieldKey] ?? '';
  }

  private formatAnswerValue(question: DhruvaPersonalQuestion, value: string): string {
    return this.isSignQuestion(question) ? this.formatSignValue(value) : value;
  }

  private isSignQuestion(question: DhruvaPersonalQuestion): boolean {
    return question.fieldKey === 'ascendant' || question.fieldKey === 'moonSign' || question.fieldKey === 'sunSign';
  }

  private formatSignValue(value: string): string {
    const trimmedValue = value.trim();
    const numberedMatch = trimmedValue.match(/^(\d{1,2})(?:\s*[-:.)]\s*)?(.*)$/);

    if (numberedMatch?.[1] && SIGN_DISPLAY_BY_NUMBER[numberedMatch[1]]) {
      return SIGN_DISPLAY_BY_NUMBER[numberedMatch[1]];
    }

    const normalizedName = this.normalizeSignName(numberedMatch?.[2] || trimmedValue);
    return SIGN_DISPLAY_BY_NAME[normalizedName] ?? (trimmedValue.replace(/^\d{1,2}\s*[-:.)]\s*/, '').trim() || trimmedValue);
  }

  private normalizeSignName(value: string): string {
    return value.toLowerCase().replace(/[^a-z]+/g, ' ').trim().split(' ')[0] ?? '';
  }

  private matchPersonalQuestion(input: string): DhruvaPersonalQuestion | null {
    const normalized = this.normalizeQuestion(input);

    if (!normalized) {
      return null;
    }

    const priorityIds = ['panchang-yoga', 'antardasha'];
    const orderedQuestions = [
      ...priorityIds.flatMap((id) => this.personalQuestions.filter((question) => question.id === id)),
      ...this.personalQuestions.filter((question) => !priorityIds.includes(question.id)),
    ];

    return orderedQuestions.find((question) =>
      question.aliases.some((alias) => normalized.includes(this.normalizeQuestion(alias)))
    ) ?? null;
  }

  private normalizeQuestion(value: string): string {
    return value.toLowerCase().replace(/[^a-z0-9]+/g, ' ').trim();
  }
  private completeTour(): void {
    if (this.activeTourKind === 'kundali-report') {
      this.storeReportTourCompleted();
    } else {
      this.storeTourCompleted();
    }

    this.isTourOpen = false;
    this.clearTargetHighlight();
  }

  private updateTourPosition(scrollTarget = false): void {
    if (typeof document === 'undefined') {
      this.tourPosition = null;
      return;
    }

    this.clearTargetHighlight();
    const target = this.currentTourStep ? document.querySelector<HTMLElement>(this.currentTourStep.selector) : null;

    if (!target) {
      this.tourPosition = null;
      return;
    }

    if (scrollTarget) {
      target.scrollIntoView({ block: 'center', inline: 'nearest' });
    }
    target.classList.add('dhruva-tour-active-target');
    const rect = target.getBoundingClientRect();
    this.tourPosition = {
      top: Math.max(8, rect.top - 8),
      left: Math.max(8, rect.left - 8),
      width: rect.width + 16,
      height: rect.height + 16,
    };
  }

  private clearTargetHighlight(): void {
    if (typeof document === 'undefined') {
      return;
    }

    document.querySelectorAll('.dhruva-tour-active-target').forEach((target) => {
      target.classList.remove('dhruva-tour-active-target');
    });
  }

  private tryStartKundaliReportTour(force = false, retryCount = 0): void {
    if (!this.hasKundaliContext || (!force && this.hasCompletedReportTour())) {
      return;
    }

    if (this.isTourOpen) {
      if (retryCount < 6 && typeof window !== 'undefined') {
        window.setTimeout(() => this.tryStartKundaliReportTour(force, retryCount + 1), 700);
      }
      return;
    }

    const visibleSteps = this.visibleTourSteps(this.kundaliReportTourSteps);

    if (!visibleSteps.length) {
      if (retryCount < 6 && typeof window !== 'undefined') {
        window.setTimeout(() => this.tryStartKundaliReportTour(force, retryCount + 1), 700);
      }
      return;
    }

    this.activeTourKind = 'kundali-report';
    this.activeReportTourSteps = visibleSteps;
    this.isOpen = false;
    this.isTourOpen = true;
    this.currentTourIndex = 0;
    this.updateTourPosition(true);
  }

  private visibleTourSteps(steps: DhruvaTourStep[]): DhruvaTourStep[] {
    if (typeof document === 'undefined') {
      return [];
    }

    return steps.filter((step) => document.querySelector(step.selector));
  }

  private hasCompletedTour(): boolean {
    if (!this.isBrowserStorageAvailable()) {
      return true;
    }

    return window.localStorage.getItem(this.tourStorageKey()) === 'true';
  }

  private hasCompletedReportTour(): boolean {
    if (!this.isBrowserStorageAvailable()) {
      return true;
    }

    return window.localStorage.getItem(this.reportTourStorageKey()) === 'true';
  }

  private storeTourCompleted(): void {
    if (!this.isBrowserStorageAvailable()) {
      return;
    }

    window.localStorage.setItem(this.tourStorageKey(), 'true');
  }

  private storeReportTourCompleted(): void {
    if (!this.isBrowserStorageAvailable()) {
      return;
    }

    window.localStorage.setItem(this.reportTourStorageKey(), 'true');
  }

  private tourStorageKey(): string {
    const userId = this.currentUserId();
    return userId ? `${TOUR_COMPLETED_KEY}.${userId}` : TOUR_COMPLETED_KEY;
  }
  private reportTourStorageKey(): string {
    const userId = this.currentUserId();
    return userId ? `${REPORT_TOUR_COMPLETED_KEY}.${userId}` : REPORT_TOUR_COMPLETED_KEY;
  }
  private currentUserId(): string | null {
    if (!this.isBrowserStorageAvailable()) {
      return null;
    }

    const rawUser = window.localStorage.getItem('kundali_user');

    if (!rawUser) {
      return null;
    }

    try {
      const user = JSON.parse(rawUser) as { userId?: unknown; email?: unknown };
      return typeof user.userId === 'string'
        ? user.userId
        : typeof user.email === 'string'
          ? user.email
          : null;
    } catch {
      return null;
    }
  }

  private isBrowserStorageAvailable(): boolean {
    return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
  }
}








































