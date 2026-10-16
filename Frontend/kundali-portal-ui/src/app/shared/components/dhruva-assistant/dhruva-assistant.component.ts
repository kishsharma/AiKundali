import { CommonModule } from '@angular/common';
import { AfterViewInit, ChangeDetectorRef, Component, DoCheck, HostBinding, HostListener, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

import { DhruvaHouseContext, DhruvaKundaliContext, DhruvaKundaliField, DhruvaPlanetContext, DhruvaResultDetailContext, mapDhruvaKundaliContext } from './dhruva-kundali-context.mapper';
import { DhruvaKundaliContextService } from './dhruva-kundali-context.service';
import { isPaidUser } from '../../../core/auth/plan.util';
import { AuthResponse } from '../../../core/auth/auth.models';
import { DhruvaNarrationMode, DhruvaNarrationService } from '../../../core/services/dhruva-narration.service';
import { SubscriptionModalService } from '../../../core/services/subscription-modal.service';
import { ResultFeedbackComponent } from '../result-feedback/result-feedback.component';
import { PredictionApiService } from '../../../core/services/prediction-api.service';
import { PredictionFocusKey, PredictionLifeArea, PredictionQuestionRequest, PredictionResponse, PredictionTimeScope } from '../../models/api.models';
import { KundaliService } from '../../../modules/kundali/kundali.service';
import { Subscription } from 'rxjs';

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

interface DhruvaCustomQuestion {
  id: string;
  label: string;
  displayTitle: string;
  summaryTopic: string;
  lifeArea: PredictionLifeArea;
  focusKey: PredictionFocusKey;
  timeScope: PredictionTimeScope;
  kind: 'possibility' | 'timing';
  aliases: string[];
}

type DhruvaReasonTone = 'supportive' | 'challenging';

interface DhruvaReasonView {
  key: string;
  label: string;
  tone: DhruvaReasonTone;
}

interface DhruvaTimingWindowView {
  uniqueKey: string;
  title: string;
  startLabel: string;
  endLabel: string;
  startTime: number;
  strength: string;
  rawStrength: string;
  triggerPlanets: string[];
  triggerLabel: string;
  evidenceKeys: string[];
  rank: number;
}

interface DhruvaCustomQuestionView {
  id: string;
  question: string;
  title: string;
  kind: 'possibility' | 'timing';
  state: 'loading' | 'ready' | 'error';
  outcome?: string;
  summary?: string;
  supportiveReasons: DhruvaReasonView[];
  challengingReasons: DhruvaReasonView[];
  technicalReasonLabels: string[];
  technicalEvidenceLabels: string[];
  technicalWindowEvidence: string[];
  timingWindows: DhruvaTimingWindowView[];
  earliestIndication?: DhruvaTimingWindowView | null;
  strongestNearTermWindow?: DhruvaTimingWindowView | null;
  nextStrongWindow?: DhruvaTimingWindowView | null;
  laterWindows: DhruvaTimingWindowView[];
  errorMessage?: string;
}

interface DhruvaNarrationView {
  state: 'loading' | 'ready' | 'error';
  text: string;
  errorMessage?: string;
}

interface DhruvaAnswer {
  key?: string;
  params?: Record<string, string>;
  kind?: 'story' | 'custom-question' | 'narration';
  title?: string;
  lines?: string[];
  customQuestion?: DhruvaCustomQuestionView;
  narration?: DhruvaNarrationView;
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

interface DhruvaSavedChartOption {
  id: string;
  name: string;
  dateLabel: string;
  place: string;
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
  imports: [CommonModule, FormsModule, TranslocoPipe, ResultFeedbackComponent],
  templateUrl: './dhruva-assistant.component.html',
  styleUrl: './dhruva-assistant.component.scss',
})
export class DhruvaAssistantComponent implements AfterViewInit, DoCheck, OnDestroy {
  constructor(
    private readonly kundaliContextService: DhruvaKundaliContextService,
    private readonly changeDetector: ChangeDetectorRef = { markForCheck: () => undefined } as ChangeDetectorRef,
    private readonly kundaliCharts: KundaliService | null = null,
  ) {}

  private readonly predictionApi = inject(PredictionApiService);
  private readonly dhruvaNarration = inject(DhruvaNarrationService);
  private readonly subscriptionModal = inject(SubscriptionModalService);
  private readonly translocoService = inject(TranslocoService);
  isOpen = false;
  isClosing = false;
  isExpanded = false;
  isFullScreen = false;
  isTourOpen = false;
  isGeneralQuestionsOpen = false;
  chartPickerOpen = false;
  savedCharts: DhruvaSavedChartOption[] = [];
  savedChartsLoading = false;
  savedChartsError = '';
  selectingChartId: string | null = null;
  private savedChartsSubscription: Subscription | null = null;
  private chartDetailSubscription: Subscription | null = null;
  private narrationSubscription: Subscription | null = null;
  private lastSavedChatSignature = '';
  private addressedPerson: string | null = null;
  currentTourIndex = 0;
  activeTourKind: DhruvaTourKind = 'portal';
  activePortalTourSteps: DhruvaTourStep[] = [];
  activeReportTourSteps: DhruvaTourStep[] = [];
  tourPosition: DhruvaTourPosition | null = null;
  selectedGeneralQuestionId = 'kundali';
  selectedPersonalQuestionId: string | null = null;
  selectedStoryActive = false;
  typedQuestion = '';
  latestQuestionText = '';
  askInputTouched = false;
  typedAnswer: DhruvaAnswer | null = null;
  latestAnswer: DhruvaAnswer | null = null;
  isTypingResponse = false;
  isCustomQuestionLoading = false;
  isNarrationLoading = false;
  ollamaNarration = false;
  replyTone: 'friendly' | 'astrologer' | 'corporate' = 'friendly';
  lifeQuestionsRemaining: number | null = null;
  private lifeQuestionDailyLimit: number | null = null;
  private aiChatFromPlan: boolean | null = null;
  upgradeBlock: { title: string; message: string } | null = null;
  private readonly aiUpgradeNote = 'AI mode is included with Premium and Premium Plus.';
  responseVisibleLength = Number.MAX_SAFE_INTEGER;
  private responseTypingStartTimer: ReturnType<typeof setTimeout> | null = null;
  private responseTypingInterval: ReturnType<typeof setInterval> | null = null;
  private customQuestionRevealTimer: ReturnType<typeof setTimeout> | null = null;
  private hadKundaliContext = false;
  private panelStateBeforeTour: { isOpen: boolean; isExpanded: boolean; isFullScreen: boolean } | null = null;
  private closeTransitionTimer: ReturnType<typeof setTimeout> | null = null;

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


  readonly customQuestions: DhruvaCustomQuestion[] = [
    this.lifeQuestion('marriage-possibility', 'Is marriage indicated?', 'Marriage Potential', 'Marriage', 'Relationships', 'marriage_possibility', 'Current', 'possibility', ['marriage indicated', 'will i marry', 'marriage possible']),
    this.lifeQuestion('marriage-timing', 'When is marriage likely?', 'Marriage Timing', 'Marriage', 'Relationships', 'marriage_timing', 'Future', 'timing', ['when marriage', 'when will i marry', 'marriage timing', 'when marry']),
    this.lifeQuestion('career-possibility', 'Is career growth indicated?', 'Career Potential', 'Career', 'Career', 'career_possibility', 'Current', 'possibility', ['career indicated', 'career growth', 'profession indicated', 'career potential']),
    this.lifeQuestion('career-timing', 'When is career progress likely?', 'Career Timing', 'Career', 'Career', 'career_timing', 'Future', 'timing', ['when career', 'career timing', 'when job', 'when promotion']),
    this.lifeQuestion('income-possibility', 'Is income indicated?', 'Income Potential', 'Income', 'Finance', 'income_possibility', 'Current', 'possibility', ['income indicated', 'gains indicated']),
    this.lifeQuestion('income-timing', 'When are gains likely?', 'Income Timing', 'Income', 'Finance', 'income_timing', 'Future', 'timing', ['when gains', 'when income', 'income timing']),
    this.lifeQuestion('savings-possibility', 'Is savings indicated?', 'Savings Potential', 'Savings', 'Finance', 'savings_possibility', 'Current', 'possibility', ['savings indicated', 'wealth indicated']),
    this.lifeQuestion('savings-timing', 'When does wealth strengthen?', 'Savings Timing', 'Savings', 'Finance', 'savings_timing', 'Future', 'timing', ['when wealth', 'when savings', 'wealth timing']),
    this.lifeQuestion('property-possibility', 'Is property indicated?', 'Property Potential', 'Property', 'Property', 'property_possibility', 'Current', 'possibility', ['property indicated', 'home indicated']),
    this.lifeQuestion('property-timing', 'When is property likely?', 'Property Timing', 'Property', 'Property', 'property_timing', 'Future', 'timing', ['when property', 'when will i buy', 'property timing', 'when home']),
    this.lifeQuestion('education-possibility', 'Is study supported?', 'Study Potential', 'Study', 'Education', 'education_possibility', 'Current', 'possibility', ['study indicated', 'education indicated', 'study supported']),
    this.lifeQuestion('education-timing', 'When is a study period stronger?', 'Study Timing', 'Study', 'Education', 'education_timing', 'Future', 'timing', ['when study', 'when education', 'exam timing', 'study timing']),
    this.lifeQuestion('children-possibility', 'Are children indicated?', 'Children Potential', 'Children', 'Education', 'children_possibility', 'Current', 'possibility', ['children indicated', 'child indicated']),
    this.lifeQuestion('children-timing', 'When is a child period likely?', 'Children Timing', 'Children', 'Education', 'children_timing', 'Future', 'timing', ['when child', 'when children', 'child timing', 'when baby']),
    this.lifeQuestion('health-possibility', 'Is vitality supported?', 'Vitality', 'Vitality', 'Health', 'health_possibility', 'Current', 'possibility', ['vitality', 'health supported', 'health indicated']),
    this.lifeQuestion('health-timing', 'When is health more sensitive?', 'Health Timing', 'Health', 'Health', 'health_timing', 'Future', 'timing', ['when health', 'health timing']),
    this.lifeQuestion('family-possibility', 'Is family support indicated?', 'Family Support', 'Family', 'Family', 'family_possibility', 'Current', 'possibility', ['family support', 'family indicated']),
    this.lifeQuestion('family-timing', 'When is family support stronger?', 'Family Timing', 'Family', 'Family', 'family_timing', 'Future', 'timing', ['when family', 'family timing']),
  ];

  private lifeQuestion(
    id: string,
    label: string,
    displayTitle: string,
    summaryTopic: string,
    lifeArea: PredictionLifeArea,
    focusKey: PredictionFocusKey,
    timeScope: PredictionTimeScope,
    kind: 'possibility' | 'timing',
    aliases: string[],
  ): DhruvaCustomQuestion {
    return { id, label, displayTitle, summaryTopic, lifeArea, focusKey, timeScope, kind, aliases };
  }

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
      id: 'dashboard-overview',
      titleKey: 'dhruva.tour.dashboardOverviewTitle',
      bodyKey: 'dhruva.tour.dashboardOverviewBody',
      selector: '[data-tour-id="dashboard-overview"]',
    },
    {
      id: 'dashboard-quick-actions',
      titleKey: 'dhruva.tour.dashboardQuickActionsTitle',
      bodyKey: 'dhruva.tour.dashboardQuickActionsBody',
      selector: '[data-tour-id="dashboard-quick-actions"]',
    },
    {
      id: 'dashboard-journey',
      titleKey: 'dhruva.tour.dashboardJourneyTitle',
      bodyKey: 'dhruva.tour.dashboardJourneyBody',
      selector: '[data-tour-id="dashboard-journey"]',
    },
    {
      id: 'dashboard-collection',
      titleKey: 'dhruva.tour.dashboardCollectionTitle',
      bodyKey: 'dhruva.tour.dashboardCollectionBody',
      selector: '[data-tour-id="dashboard-collection"]',
    },    {
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
      id: 'panchang',
      titleKey: 'dhruva.tour.panchangTitle',
      bodyKey: 'dhruva.tour.panchangBody',
      selector: '[data-tour-id="portal-panchang"]',
    },    {
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
    this.loadDhruvaAllowance();
    if (!this.hasCompletedTour()) {
      window.setTimeout(() => this.startTour(), 350);
    }
  }

  get dhruvaAiEnabled(): boolean {
    return this.aiChatFromPlan ?? isPaidUser(this.readStoredUser() as AuthResponse | null);
  }
  ngDoCheck(): void {
    const hasContext = this.hasKundaliContext;

    if (hasContext && !this.hadKundaliContext && typeof window !== 'undefined') {
      window.setTimeout(() => this.tryStartKundaliReportTour(), 450);
    }

    this.hadKundaliContext = hasContext;

    const person = this.activePersonName;
    if (this.addressedPerson && person !== this.addressedPerson) {
      this.latestAnswer = null;
      this.typedAnswer = null;
      this.latestQuestionText = '';
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.savedCharts = [];
      this.chartPickerOpen = false;
    }
    this.addressedPerson = person;
  }

  ngOnDestroy(): void {
    this.setFullScreenScrollLock(false);
    this.savedChartsSubscription?.unsubscribe();
    this.chartDetailSubscription?.unsubscribe();
    this.narrationSubscription?.unsubscribe();
    this.clearResponseTypingTimers();
    this.clearCustomQuestionRevealTimer();
    this.clearCloseTransitionTimer();
    this.clearTargetHighlight();
  }

  @HostBinding('class.dhruva-tour-host-open')
  get isTourHostOpen(): boolean {
    return this.isTourOpen;
  }

  @HostBinding('class.dhruva-expanded')
  get isExpandedHost(): boolean {
    return this.isExpanded;
  }

  @HostBinding('class.dhruva-full')
  get isFullScreenHost(): boolean {
    return this.isFullScreen;
  }

  get kundaliContext(): DhruvaKundaliContext | null {
    return this.kundaliContextService.context();
  }

  get hasKundaliContext(): boolean {
    return this.kundaliContext !== null;
  }

  get kundaliSource(): unknown | null {
    return this.kundaliContextService.source();
  }

  get hasCustomQuestionSource(): boolean {
    return this.hasKundaliContext && this.kundaliSource !== null;
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

  get viewerName(): string | null {
    return this.currentUserDisplayName();
  }

  get activePersonName(): string | null {
    const field = this.kundaliContext?.personName;
    return field?.status === 'available' && field.value ? field.value : null;
  }

  get activeChartId(): string | null {
    const field = this.kundaliContext?.kundaliId;
    return field?.status === 'available' && field.value ? field.value : null;
  }

  get activeChartMeta(): string {
    const date = this.kundaliContext?.birthDate;
    const place = this.kundaliContext?.birthPlace;
    return [date?.status === 'available' ? this.formatChartDate(date.value) : '', place?.status === 'available' ? place.value : '']
      .filter(Boolean)
      .join(' · ');
  }

  get chartReplyLead(): string | null {
    const person = this.activePersonName;
    if (!person) {
      return null;
    }

    const user = this.viewerName;
    return user
      ? this.translateOr('dhruva.chart.replyLead', { user, person }, `${user}, the kundali of ${person} shows:`)
      : this.translateOr('dhruva.chart.replyLeadNoUser', { person }, `The kundali of ${person} shows:`);
  }

  get showChartReplyLead(): boolean {
    if (!this.chartReplyLead) {
      return false;
    }

    const answer = this.displayedAnswer;
    if (!answer) {
      return false;
    }

    if (answer.kind === 'story' || answer.kind === 'custom-question') {
      return true;
    }

    return !!answer.key?.startsWith('dhruva.personal') && answer.key !== 'dhruva.personal.noKundaliAnswer';
  }

  get welcomeMessage(): string {
    const user = this.viewerName;
    const person = this.activePersonName;

    if (user && person) {
      return this.translateOr('dhruva.chart.welcomeWithPerson', { user, person }, `Hi ${user}. I am reading ${person}'s kundali. How can I guide you?`);
    }

    if (user) {
      return this.translateOr('dhruva.chart.welcomeChoose', { user }, `Hi ${user}. Choose a saved kundali and I will read that chart.`);
    }

    if (person) {
      return this.translateOr('dhruva.chart.welcomePersonOnly', { person }, `I am reading ${person}'s kundali. How can I guide you?`);
    }

    return this.translateOr('dhruva.chart.welcome', {}, 'Hi, how can I guide you?');
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

  get dhruvaStatus(): 'idle' | 'thinking' | 'speaking' {
    if (this.isTypingResponse) {
      return 'thinking';
    }

    return this.responseVisibleLength !== Number.MAX_SAFE_INTEGER && this.responseVisibleLength < 600
      ? 'speaking'
      : 'idle';
  }

  get dhruvaStatusLabel(): string {
    const labels: Record<'idle' | 'thinking' | 'speaking', string> = {
      idle: 'Idle',
      thinking: 'Thinking',
      speaking: 'Speaking',
    };

    return labels[this.dhruvaStatus];
  }

  open(): void {
    this.clearCloseTransitionTimer();
    this.isClosing = false;
    this.isOpen = true;
  }

  close(): void {
    if (!this.isOpen) {
      this.isClosing = false;
      this.isExpanded = false;
      this.isFullScreen = false;
      this.setFullScreenScrollLock(false);
      return;
    }

    this.isOpen = false;
    this.isExpanded = false;
    this.isFullScreen = false;
    this.setFullScreenScrollLock(false);
    this.isClosing = true;
    this.clearCloseTransitionTimer();
    this.closeTransitionTimer = setTimeout(() => {
      this.isClosing = false;
      this.closeTransitionTimer = null;
      this.markResponseAnimationForCheck();
    }, 220);
  }

  toggleChartPicker(): void {
    this.chartPickerOpen = !this.chartPickerOpen;
    if (this.chartPickerOpen) {
      this.loadSavedCharts();
    }
  }

  selectSavedChart(chart: DhruvaSavedChartOption): void {
    if (!this.kundaliCharts || this.selectingChartId) {
      return;
    }

    this.selectingChartId = chart.id;
    this.savedChartsError = '';
    this.chartDetailSubscription?.unsubscribe();
    this.chartDetailSubscription = this.kundaliCharts.getSavedKundali(chart.id).subscribe({
      next: (saved) => {
        const context = mapDhruvaKundaliContext({
          ...saved,
          kundaliId: saved?.id ?? chart.id,
          fullName: saved?.fullName ?? saved?.FullName ?? chart.name,
          dateOfBirth: saved?.dateOfBirth ?? saved?.DateOfBirth,
          birthPlace: saved?.birthPlace ?? saved?.BirthPlace ?? chart.place,
        });
        this.kundaliContextService.setContext(context, saved?.chartData ?? saved);
        this.chartPickerOpen = false;
        this.selectingChartId = null;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.savedChartsError = 'Saved kundalis could not be loaded.';
        this.selectingChartId = null;
        this.changeDetector.markForCheck();
      },
    });
  }

  private loadSavedCharts(): void {
    if (!this.kundaliCharts) {
      this.savedCharts = [];
      this.savedChartsError = 'Saved kundalis could not be loaded.';
      return;
    }

    this.savedChartsLoading = true;
    this.savedChartsError = '';
    this.savedChartsSubscription?.unsubscribe();
    this.savedChartsSubscription = this.kundaliCharts.getSavedKundalis().subscribe({
      next: (items) => {
        const list = Array.isArray(items) ? items : [];
        this.savedCharts = list
          .map((item) => this.toSavedChartOption(item))
          .filter((item): item is DhruvaSavedChartOption => item !== null);
        this.savedChartsLoading = false;
        this.changeDetector.markForCheck();
      },
      error: () => {
        this.savedCharts = [];
        this.savedChartsLoading = false;
        this.savedChartsError = 'Saved kundalis could not be loaded.';
        this.changeDetector.markForCheck();
      },
    });
  }

  private toSavedChartOption(item: any): DhruvaSavedChartOption | null {
    const id = String(item?.id ?? item?.Id ?? '').trim();
    const name = String(item?.fullName ?? item?.FullName ?? '').trim();
    if (!id || !name) {
      return null;
    }

    const place = String(item?.birthPlace ?? item?.BirthPlace ?? '').trim();
    return {
      id,
      name,
      dateLabel: this.formatChartDate(item?.dateOfBirth ?? item?.DateOfBirth),
      place,
    };
  }

  private formatChartDate(value: unknown): string {
    const text = String(value ?? '').trim();
    const match = text.match(/^(\d{4})-(\d{2})-(\d{2})/);
    if (!match) {
      return text;
    }

    const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    const month = months[Number(match[2]) - 1];
    return month ? `${Number(match[3])} ${month} ${match[1]}` : text.slice(0, 10);
  }

  private persistLocalChat(): void {
    const answer = this.latestAnswer;
    if (!answer || answer.kind === 'narration') {
      return;
    }

    if (answer.kind === 'custom-question' && answer.customQuestion?.state !== 'ready') {
      return;
    }

    const question = this.chatQuestionText();
    const response = this.chatResponseText(answer);
    const kundaliId = this.activeChartId;
    if (!question || !response || !kundaliId || !/^[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(kundaliId)) {
      return;
    }

    const signature = `${kundaliId}|${question}|${response}`;
    if (signature === this.lastSavedChatSignature) {
      return;
    }

    this.lastSavedChatSignature = signature;
    try {
      this.dhruvaNarration.saveChat({ kundaliId, question, response }).subscribe({
        error: () => undefined,
      });
    } catch {
      this.lastSavedChatSignature = '';
    }
  }

  private chatQuestionText(): string {
    const typed = this.latestQuestionText?.trim();
    if (typed) {
      return typed;
    }

    if (this.selectedStoryActive) {
      return 'Janma Story';
    }

    const personal = this.personalQuestions.find((item) => item.id === this.selectedPersonalQuestionId);
    if (personal) {
      return this.translateOr(personal.questionKey, {}, personal.id);
    }

    const general = this.generalQuestions.find((item) => item.id === this.selectedGeneralQuestionId);
    if (general) {
      return this.translateOr(general.questionKey, {}, general.id);
    }

    return '';
  }

  private chatResponseText(answer: DhruvaAnswer): string {
    if (this.skippedChatAnswer(answer)) {
      return '';
    }

    if (answer.kind === 'custom-question') {
      return answer.customQuestion?.summary?.trim() || '';
    }

    if (answer.kind === 'story') {
      return [answer.title, ...(answer.lines ?? [])].filter((line) => !!line?.trim()).join('\n\n');
    }

    if (answer.key) {
      return this.translateOr(answer.key, answer.params ?? {}, '').trim();
    }

    return '';
  }

  private skippedChatAnswer(answer: DhruvaAnswer): boolean {
    const key = answer.key ?? '';
    return key === 'dhruva.personal.emptyQuestionAnswer'
      || key === 'dhruva.personal.noKundaliAnswer'
      || key === 'dhruva.personal.unsupportedAnswer'
      || key === 'dhruva.personal.ambiguousHouseAnswer';
  }

  private translateOr(key: string, params: Record<string, string>, fallback: string): string {
    try {
      const translated = this.translocoService.translate(key, params);
      return translated && translated !== key ? translated : fallback;
    } catch {
      return fallback;
    }
  }

  toggle(): void {
    if (this.isOpen) {
      this.close();
      return;
    }

    this.open();
  }

  expand(): void {
    if (!this.isOpen || this.isFullScreen) {
      return;
    }

    if (!this.isExpanded) {
      this.isExpanded = true;
      return;
    }

    this.isFullScreen = true;
    this.setFullScreenScrollLock(true);
  }

  restoreCompact(): void {
    if (this.isFullScreen) {
      this.isFullScreen = false;
      this.setFullScreenScrollLock(false);
      return;
    }

    this.isExpanded = false;
  }

  private setFullScreenScrollLock(locked: boolean): void {
    if (typeof document === 'undefined' || !document.body?.classList) {
      return;
    }

    document.body.classList.toggle('dhruva-full-open', locked);
  }
  toggleGeneralQuestions(): void {
    this.isGeneralQuestionsOpen = !this.isGeneralQuestionsOpen;
  }

  selectGeneralQuestion(questionId: string): void {
    if (this.ollamaNarration && this.dhruvaAiEnabled) {
      const question = this.generalQuestions.find((item) => item.id === questionId);
      this.narrateQuestion(question ? this.translateOr(question.questionKey, {}, question.id) : questionId);
      return;
    }

    this.selectedGeneralQuestionId = questionId;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = false;
    this.typedAnswer = null;
    this.latestAnswer = { key: this.selectedGeneralQuestion.answerKey };
    this.beginResponseReveal();
  }

  selectPersonalQuestion(questionId: string): void {
    if (this.ollamaNarration && this.dhruvaAiEnabled) {
      const question = this.personalQuestions.find((item) => item.id === questionId);
      this.narrateQuestion(question ? this.translateOr(question.questionKey, {}, question.id) : questionId);
      return;
    }

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
    if (this.ollamaNarration && this.dhruvaAiEnabled) {
      this.narrateQuestion('Janma Story');
      return;
    }

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
    this.latestQuestionText = trimmedQuestion;
    this.typedQuestion = '';

    if (this.ollamaNarration && this.dhruvaAiEnabled) {
      this.narrateQuestion(trimmedQuestion);
      return;
    }

    const wantsStory = this.matchStoryQuestion(trimmedQuestion);

    if (wantsStory) {
      this.tellJanmaStory();
      return;
    }

    const lifeQuestion = this.matchCustomQuestion(trimmedQuestion);
    if (lifeQuestion) {
      this.askCustomQuestion(lifeQuestion.id);
      return;
    }

    const panchangFieldKey = this.matchTypedPanchangField(trimmedQuestion);
    const houseQuery = panchangFieldKey ? null : this.matchTypedHouseQuestion(trimmedQuestion);
    const resultDetail = panchangFieldKey || houseQuery ? null : this.matchTypedResultDetail(trimmedQuestion);
    const match = panchangFieldKey || houseQuery || resultDetail ? null : this.matchPersonalQuestion(trimmedQuestion);
    const planetFact = match || panchangFieldKey || houseQuery || resultDetail ? null : this.matchTypedPlanetFact(trimmedQuestion);

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

  private narrationMode(question: string): DhruvaNarrationMode {
    const text = question.toLowerCase();
    if (text.includes('janma') || text.includes('birth story') || text.includes('life story')) {
      return 'janma_story';
    }

    if (text.includes('dasha') || text.includes('mahadasha') || text.includes('antardasha')) {
      return 'dasha_focus';
    }

    return 'qa';
  }

  private narrateQuestion(question: string): void {
    if (!this.dhruvaAiEnabled) {
      this.ollamaNarration = false;
      this.showUpgradeBlock('AI mode needs Premium', this.aiUpgradeNote);
      return;
    }

    if (this.lifeQuestionsRemaining === 0) {
      this.showLifeQuestionLimit();
      return;
    }

    this.isOpen = true;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = false;
    this.latestQuestionText = question;

    if (!this.kundaliSource || typeof this.kundaliSource !== 'object') {
      this.isNarrationLoading = false;
      this.typedAnswer = { key: 'dhruva.personal.noKundaliAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    const context = this.kundaliContext;
    const chart = {
      ...(this.kundaliSource as Record<string, unknown>),
      native: {
        name: context?.personName?.value ?? null,
        birthDate: context?.birthDate?.value ?? null,
        birthPlace: context?.birthPlace?.value ?? null,
      },
    };

    this.isNarrationLoading = true;
    this.typedAnswer = null;
    this.latestAnswer = { kind: 'narration', narration: { state: 'loading', text: '' } };
    this.changeDetector.markForCheck();
    this.narrationSubscription?.unsubscribe();
    this.narrationSubscription = this.dhruvaNarration.narrate({
      mode: this.narrationMode(question),
      question,
      locale: this.translocoService.getActiveLang(),
      tone: this.replyTone,
      kundaliId: this.activeChartId,
      chart,
    }).subscribe({
      next: (response) => {
        const text = response?.narration || '';
        this.isNarrationLoading = false;
        if (this.lifeQuestionsRemaining !== null) {
          this.lifeQuestionsRemaining = Math.max(0, this.lifeQuestionsRemaining - 1);
        }
        this.latestAnswer = {
          kind: 'narration',
          narration: { state: 'ready', text },
        };
        this.beginResponseReveal(Math.max(text.length, 1), 6, 12);
      },
      error: (error: { status?: number; error?: { message?: string } }) => {
        this.isNarrationLoading = false;
        if (error?.status === 403) {
          const message = error.error?.message || '';
          if (/life question/i.test(message)) {
            this.lifeQuestionsRemaining = 0;
            this.showLifeQuestionLimit();
            return;
          }
          this.showUpgradeBlock('AI mode needs Premium', this.aiUpgradeNote);
          return;
        }
        this.latestAnswer = {
          kind: 'narration',
          narration: {
            state: 'error',
            text: '',
            errorMessage: 'Chart narration is unavailable right now.',
          },
        };
        this.changeDetector.markForCheck();
      },
    });
  }

  askCustomQuestion(questionId: string): void {
    const question = this.customQuestions.find((item) => item.id === questionId);

    if (!question) {
      return;
    }

    if (this.ollamaNarration && this.dhruvaAiEnabled) {
      this.narrateQuestion(question.label);
      return;
    }

    if (this.lifeQuestionsRemaining === 0) {
      this.showLifeQuestionLimit();
      return;
    }

    if (this.isCustomQuestionLoading) {
      return;
    }

    if (!this.kundaliSource) {
      this.selectedPersonalQuestionId = null;
      this.selectedStoryActive = false;
      this.typedAnswer = { key: 'dhruva.personal.noKundaliAnswer' };
      this.latestAnswer = this.typedAnswer;
      this.beginResponseReveal();
      return;
    }

    this.isOpen = true;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = false;
    this.askInputTouched = false;
    this.typedQuestion = '';
    this.latestQuestionText = question.label;
    this.typedAnswer = null;
    this.isCustomQuestionLoading = true;
    this.latestAnswer = {
      kind: 'custom-question',
      customQuestion: {
        id: question.id,
        question: question.label,
        title: question.displayTitle,
        kind: question.kind,
        state: 'loading',
        supportiveReasons: [],
        challengingReasons: [],
        technicalReasonLabels: [],
        technicalEvidenceLabels: [],
        technicalWindowEvidence: [],
        timingWindows: [],
        laterWindows: [],
      },
    };

    this.clearResponseTypingTimers(false);
    this.isTypingResponse = true;
    this.responseVisibleLength = 0;
    this.markResponseAnimationForCheck();
    this.scrollLatestResponseIntoView();

    const request: PredictionQuestionRequest = {
      lifeArea: question.lifeArea,
      focusKey: question.focusKey,
      timeScope: question.timeScope,
      referenceDate: new Date().toISOString(),
      birthDate: this.birthDateIso(),
      kundaliData: this.kundaliSource as any,
    };

    this.predictionApi.askQuestion(request).subscribe({
      next: (response) => {
        if (this.lifeQuestionsRemaining !== null) {
          this.lifeQuestionsRemaining = Math.max(0, this.lifeQuestionsRemaining - 1);
        }
        const prediction = (response?.data ?? response) as PredictionResponse;
        const mappedAnswer: DhruvaAnswer = {
          kind: 'custom-question',
          customQuestion: this.mapCustomQuestionView(question, prediction),
        };
        this.finishCustomQuestionReveal(mappedAnswer);
      },
      error: (error: Error) => {
        const denied = /forbidden/i.test(error?.message || '');
        if (denied) {
          this.isCustomQuestionLoading = false;
          this.lifeQuestionsRemaining = 0;
          this.showLifeQuestionLimit();
          return;
        }
        const erroredAnswer: DhruvaAnswer = {
          kind: 'custom-question',
          customQuestion: {
            id: question.id,
            question: question.label,
            title: question.displayTitle,
            kind: question.kind,
            state: 'error',
            supportiveReasons: [],
            challengingReasons: [],
            technicalReasonLabels: [],
            technicalEvidenceLabels: [],
            technicalWindowEvidence: [],
            timingWindows: [],
            laterWindows: [],
            errorMessage: error.message || 'Unable to load the answer right now.',
          },
        };
        this.finishCustomQuestionReveal(erroredAnswer);
      },
    });
  }

  private showLifeQuestionLimit(): void {
    const limit = this.lifeQuestionDailyLimit && this.lifeQuestionDailyLimit > 0
      ? this.lifeQuestionDailyLimit
      : 2;
    const noun = limit === 1 ? 'life question' : 'life questions';
    this.showUpgradeBlock(
      `Today's ${limit} ${noun} are used`,
      `Free includes ${limit} ${noun} a day. Upgrade to Premium for every life question and AI mode.`,
    );
  }

  openUpgrade(): void {
    this.subscriptionModal.open();
  }

  private showUpgradeBlock(title: string, message: string): void {
    this.isOpen = true;
    this.selectedPersonalQuestionId = null;
    this.selectedStoryActive = false;
    this.isNarrationLoading = false;
    this.isCustomQuestionLoading = false;
    this.isTypingResponse = false;
    this.clearResponseTypingTimers();
    this.latestAnswer = null;
    this.typedAnswer = null;
    this.upgradeBlock = { title, message };
    this.changeDetector.markForCheck();
  }

  private loadDhruvaAllowance(): void {
    this.dhruvaNarration.getAllowance().subscribe({
      next: (allowance) => {
        this.aiChatFromPlan = !!allowance?.dhruvaAiEnabled;
        this.lifeQuestionsRemaining = allowance?.lifeQuestionsRemaining ?? null;
        this.lifeQuestionDailyLimit = allowance?.lifeQuestionDailyLimit ?? null;
        if (!this.aiChatFromPlan) {
          this.ollamaNarration = false;
        }
        this.changeDetector.markForCheck();
      },
      error: () => undefined,
    });
  }

  isCustomQuestionActive(questionId: string): boolean {
    return this.latestAnswer?.kind === 'custom-question'
      && this.latestAnswer.customQuestion?.id === questionId;
  }

  getOutcomeToneClass(outcome: string | undefined): string {
    switch ((outcome ?? '').trim().toLowerCase()) {
      case 'supported':
      case 'supportive':
      case 'highly supportive':
        return 'supported';
      case 'challenging':
        return 'challenging';
      case 'mixed':
        return 'mixed';
      default:
        return 'neutral';
    }
  }

  narrationOutcomeLabel(): string | null {
    const text = this.toPlainNarration(this.latestAnswer?.narration?.text ?? '').toLowerCase();
    const match = text.match(/\b(highly supportive|supportive|mixed|challenging)\b/);
    switch (match?.[1]) {
      case 'highly supportive':
      case 'supportive':
        return 'Supportive';
      case 'mixed':
        return 'Mixed';
      case 'challenging':
        return 'Challenging';
      default:
        return null;
    }
  }

  private mapCustomQuestionView(
    question: DhruvaCustomQuestion,
    response: PredictionResponse | null | undefined,
  ): DhruvaCustomQuestionView {
    const answer = response?.answer ?? null;
    const technicalReasonKeys = this.normalizeKeys(answer?.reasonKeys);
    const reasonViews = technicalReasonKeys.map((key) => this.mapReasonView(key));
    const technicalEvidenceKeys = this.normalizeKeys(answer?.evidenceKeys);
    const timingWindows = Array.isArray(answer?.timingWindows)
      ? answer.timingWindows.map((window, index) => this.mapTimingWindowView(window, index))
      : [];
    const timingPresentation = this.buildTimingWindowPresentation(timingWindows);
    const outcome = answer?.outcome || response?.synthesis?.overallTone || 'Mixed';
    const supportiveReasons = reasonViews.filter((reason) => reason.tone === 'supportive');
    const challengingReasons = reasonViews.filter((reason) => reason.tone === 'challenging');

    return {
      id: question.id,
      question: question.label,
      title: question.displayTitle,
      kind: question.kind,
      state: 'ready',
      outcome,
      summary: this.buildCustomQuestionSummary(question, outcome, supportiveReasons, challengingReasons, timingPresentation),
      supportiveReasons,
      challengingReasons,
      technicalReasonLabels: this.uniqueNonEmpty(reasonViews.map((reason) => reason.label)),
      technicalEvidenceLabels: this.uniqueNonEmpty(technicalEvidenceKeys.map((key) => this.mapEvidenceLabel(key))),
      technicalWindowEvidence: this.mapTimingWindowEvidenceLabels(timingWindows),
      timingWindows,
      earliestIndication: timingPresentation.earliestIndication,
      strongestNearTermWindow: timingPresentation.strongestNearTermWindow,
      nextStrongWindow: timingPresentation.nextStrongWindow,
      laterWindows: timingPresentation.laterWindows,
    };
  }

  private mapReasonView(key: string): DhruvaReasonView {
    const labels: Record<string, string> = {
      'primary_house.strong': 'Strong primary-house foundation',
      'primary_house.weak': 'Weak primary-house foundation',
      'primary_house.benefic_aspect': 'Helpful aspect on the primary house',
      'primary_house.malefic_aspect': 'Pressure on the primary house',
      'primary_house_lord.strong_dignity': 'Primary-house lord is well placed',
      'primary_house_lord.weak_dignity': 'Primary-house lord is under pressure',
      'primary_house_lord.benefic_aspect': 'Helpful aspect on the primary-house lord',
      'primary_house_lord.malefic_aspect': 'Pressure on the primary-house lord',
      'natural_significator.supportive': 'Natural significators are supportive',
      'natural_significator.challenging': 'Natural significators show pressure',
      'confirmation.supportive': 'Divisional confirmation is supportive',
      'confirmation.challenging': 'Divisional confirmation shows pressure',
      'occupants.supportive': 'Divisional occupants add supportive energy',
      'occupants.challenging': 'Divisional occupants add pressure',
      'age.before_ready': 'Timing starts at the 18th birthday',
      'age.later_years': 'Later-year periods fit this age',
    };
    const suffix = Object.keys(labels).find((entry) => key.endsWith(entry));

    return {
      key,
      label: suffix ? labels[suffix] : this.humanizeKey(key),
      tone: this.getReasonTone(key),
    };
  }

  private getReasonTone(key: string): DhruvaReasonTone {
    const normalized = key.toLowerCase();
    return normalized.includes('weak') || normalized.includes('malefic') || normalized.includes('challenging') || normalized.includes('pressure')
      ? 'challenging'
      : 'supportive';
  }

  private mapEvidenceLabel(key: string): string {
    const trimmed = key.trim();
    const normalized = trimmed.toLowerCase();
    const divisionalLordMatch = trimmed.match(/^divisional:(\w+):house:(\d+):lord:([A-Za-z]+)$/i);
    if (divisionalLordMatch) {
      return `${this.formatChartType(divisionalLordMatch[1])} ${this.formatHouseOrdinal(Number(divisionalLordMatch[2]))} house lord is ${this.displayPlanetName(divisionalLordMatch[3])}`;
    }

    const divisionalOccupantMatch = trimmed.match(/^divisional:(\w+):house:(\d+):occupant:([A-Za-z]+)$/i);
    if (divisionalOccupantMatch) {
      return `${this.displayPlanetName(divisionalOccupantMatch[3])} occupies the ${this.formatChartType(divisionalOccupantMatch[1])} ${this.formatHouseOrdinal(Number(divisionalOccupantMatch[2]))} house`;
    }

    const divisionalHouseMatch = trimmed.match(/^divisional:(\w+):house:(\d+)$/i);
    if (divisionalHouseMatch) {
      return `${this.formatChartType(divisionalHouseMatch[1])} ${this.formatHouseOrdinal(Number(divisionalHouseMatch[2]))} house is part of the reading`;
    }

    const planetMatch = trimmed.match(/(?:^|[:._-])(Sun|Moon|Mars|Mercury|Jupiter|Venus|Saturn|Rahu|Ketu)(?:$|[:._-])/i);
    if (normalized.includes('primary_house_lord')) {
      return planetMatch
        ? `${this.displayPlanetName(planetMatch[1])} is acting as the main house lord`
        : 'The main house lord is active in this reading';
    }

    if (normalized.includes('primary_house')) {
      return 'The main house is actively involved';
    }

    if (normalized.includes('natural_significator') || normalized.includes('significator')) {
      return planetMatch
        ? `${this.displayPlanetName(planetMatch[1])} is acting as a significator`
        : 'A key significator is active';
    }

    const houseMatch = trimmed.match(/(?:^|[:._-])house[:._-]?(\d+)(?:$|[:._-])/i);
    if (houseMatch) {
      return `${this.formatHouseOrdinal(Number(houseMatch[1]))} house is part of this reading`;
    }

    if (planetMatch) {
      return `${this.displayPlanetName(planetMatch[1])} is one of the active chart triggers`;
    }

    const dashaMatch = trimmed.match(/(?:^|[:._-])(MD|AD|PD)[:._-]?([A-Za-z]+)(?:$|[:._-])/i);
    if (dashaMatch) {
      const periodLabels: Record<string, string> = {
        MD: 'Mahadasha',
        AD: 'Antardasha',
        PD: 'Pratyantardasha',
      };
      return `${this.displayPlanetName(dashaMatch[2])} ${periodLabels[dashaMatch[1].toUpperCase()] ?? dashaMatch[1].toUpperCase()} is active`;
    }

    return this.humanizeKey(trimmed);
  }

  private mapTimingWindowEvidenceLabels(timingWindows: DhruvaTimingWindowView[]): string[] {
    return timingWindows.map((window) => {
      const evidence = this.uniqueNonEmpty(window.evidenceKeys.map((key) => this.mapEvidenceLabel(key)));
      const summary = evidence.length ? evidence.join(', ') : 'Planet-period support is active in this window';
      return `${window.title}: ${summary}`;
    });
  }

  private uniqueNonEmpty(values: string[]): string[] {
    return Array.from(new Set(values.map((value) => value.trim()).filter(Boolean)));
  }

  private formatChartType(value: string): string {
    return value.trim().toUpperCase();
  }

  private formatHouseOrdinal(value: number): string {
    return Number.isFinite(value) && value > 0
      ? `${value}${this.ordinalSuffix(value)}`
      : 'relevant';
  }

  private mapTimingWindowView(window: { startDate: string; endDate: string; strength: string; triggerPlanets: string[]; evidenceKeys: string[] }, rank: number): DhruvaTimingWindowView {
    const triggerPlanets = Array.isArray(window.triggerPlanets) ? window.triggerPlanets.filter(Boolean) : [];
    const startTime = Date.parse(window.startDate || '');
    const startLabel = this.formatDisplayDate(window.startDate);
    const endLabel = this.formatDisplayDate(window.endDate);
    const strength = this.formatStrengthLabel(window.strength || 'Candidate');

    return {
      uniqueKey: `${window.startDate}|${window.endDate}|${window.strength}|${triggerPlanets.join('+')}`,
      title: `${startLabel} - ${endLabel}`,
      startLabel,
      endLabel,
      startTime: Number.isFinite(startTime) ? startTime : Number.MAX_SAFE_INTEGER,
      strength,
      rawStrength: window.strength || 'Candidate',
      triggerPlanets,
      triggerLabel: triggerPlanets.join(' + '),
      evidenceKeys: this.normalizeKeys(window.evidenceKeys),
      rank,
    };
  }

  private buildTimingWindowPresentation(timingWindows: DhruvaTimingWindowView[]): {
    earliestIndication: DhruvaTimingWindowView | null;
    strongestNearTermWindow: DhruvaTimingWindowView | null;
    nextStrongWindow: DhruvaTimingWindowView | null;
    laterWindows: DhruvaTimingWindowView[];
  } {
    if (!timingWindows.length) {
      return {
        earliestIndication: null,
        strongestNearTermWindow: null,
        nextStrongWindow: null,
        laterWindows: [],
      };
    }

    const earliestIndication = [...timingWindows]
      .sort((left, right) => left.startTime - right.startTime || left.rank - right.rank)[0] ?? null;

    const strongestNearTermWindow = timingWindows[0] ?? null;
    const selected = new Set<string>();

    if (earliestIndication) {
      selected.add(earliestIndication.uniqueKey);
    }

    const strongestUnique = strongestNearTermWindow && !selected.has(strongestNearTermWindow.uniqueKey)
      ? strongestNearTermWindow
      : null;

    if (strongestUnique) {
      selected.add(strongestUnique.uniqueKey);
    }

    const nextStrongWindow = timingWindows.find((window) => !selected.has(window.uniqueKey)) ?? null;

    if (nextStrongWindow) {
      selected.add(nextStrongWindow.uniqueKey);
    }

    return {
      earliestIndication,
      strongestNearTermWindow: strongestUnique,
      nextStrongWindow,
      laterWindows: timingWindows.filter((window) => !selected.has(window.uniqueKey)),
    };
  }

  private buildCustomQuestionSummary(
    question: DhruvaCustomQuestion,
    outcome: string,
    supportiveReasons: DhruvaReasonView[],
    challengingReasons: DhruvaReasonView[],
    timingPresentation: {
      earliestIndication: DhruvaTimingWindowView | null;
      strongestNearTermWindow: DhruvaTimingWindowView | null;
      nextStrongWindow: DhruvaTimingWindowView | null;
    },
  ): string {
    const topic = question.summaryTopic;
    const ageNote = this.ageSummaryNote([...supportiveReasons, ...challengingReasons], topic);
    const caveat = topic === 'Children'
      ? ' This is a chart indication, not a medical result.'
      : topic === 'Health' || topic === 'Vitality'
        ? ' This describes chart pressure, not a diagnosis.'
        : '';

    if (question.kind === 'timing') {
      if (!timingPresentation.earliestIndication) {
        return ageNote
          ? `${ageNote}No later window falls inside the next 10 years.${caveat}`
          : `No clear near-term ${topic.toLowerCase()} window surfaced from the current dasha pattern.${caveat}`;
      }

      const windowNote = timingPresentation.strongestNearTermWindow
        ? `The earliest indication opens around ${timingPresentation.earliestIndication.title}, while the strongest near-term support gathers around ${timingPresentation.strongestNearTermWindow.title}.`
        : `The earliest meaningful indication appears around ${timingPresentation.earliestIndication.title}.`;
      return `${ageNote}${windowNote}${caveat}`;
    }

    switch (outcome.trim().toLowerCase()) {
      case 'supported':
        return (challengingReasons.length
          ? `${topic} indicators look supportive overall, with a stable base and some manageable pressure.`
          : `${topic} indicators look supportive overall, with the core foundation holding well.`) + caveat;
      case 'challenging':
        return `${topic} indicators show more pressure than support in the core foundation right now.${caveat}`;
      default:
        return (supportiveReasons.length && challengingReasons.length
          ? `${topic} is indicated through a mixed pattern of support and pressure in the chart.`
          : `${topic} indicators are mixed at the moment.`) + caveat;
    }
  }

  private ageSummaryNote(reasons: readonly DhruvaReasonView[], topic: string): string {
    if (reasons.some((reason) => reason.key.endsWith('.age.before_ready'))) {
      return `${topic} timing starts at the 18th birthday. `;
    }

    if (reasons.some((reason) => reason.key.endsWith('.age.later_years'))) {
      return `${topic} timing includes later-year periods for this age. `;
    }

    return '';
  }

  private matchCustomQuestion(question: string): DhruvaCustomQuestion | null {
    const compact = question
      .toLowerCase()
      .replace(/[^a-z0-9\s]/g, ' ')
      .replace(/\b(is|are|does|my|the|a|an|me|for|of|in|on|to|be|get|likely|about)\b/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();

    const matches = this.customQuestions.filter((item) => item.aliases.some((alias) => compact.includes(alias)));
    if (matches.length === 0) {
      return null;
    }

    const wantsTiming = /\bwhen\b/.test(compact);
    return matches.find((item) => item.kind === (wantsTiming ? 'timing' : 'possibility')) ?? matches[0];
  }

  private birthDateIso(): string | undefined {
    const field = this.kundaliContext?.birthDate;
    if (field?.status !== 'available' || !field.value) {
      return undefined;
    }

    const parsed = Date.parse(field.value);
    if (Number.isNaN(parsed)) {
      return undefined;
    }

    return new Date(parsed).toISOString();
  }

  private normalizeKeys(values: readonly string[] | null | undefined): string[] {
    if (!Array.isArray(values)) {
      return [];
    }

    return Array.from(new Set(values.map((value) => String(value ?? '').trim()).filter(Boolean)));
  }

  private humanizeKey(value: string): string {
    const cleaned = value
      .replace(/[:._-]+/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();

    if (!cleaned) {
      return '';
    }

    return cleaned
      .split(' ')
      .map((part) => {
        const upper = part.toUpperCase();
        if (upper === 'D9' || upper === 'MD' || upper === 'AD' || upper === 'PD') {
          return upper;
        }

        return part.charAt(0).toUpperCase() + part.slice(1);
      })
      .join(' ');
  }

  private formatStrengthLabel(value: string | null | undefined): string {
    switch ((value ?? '').trim()) {
      case 'VeryStrong':
        return 'Very Strong';
      default:
        return this.humanizeKey(value ?? 'Candidate');
    }
  }

  private formatDisplayDate(value: string | null | undefined): string {
    if (!value) {
      return 'TBD';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'TBD';
    }

    return new Intl.DateTimeFormat('en-IN', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }
  animatedResponseText(value: string): string {
    const plain = this.toPlainNarration(value);
    if (!plain || this.isReducedMotionPreferred()) {
      return plain;
    }

    return plain.slice(0, Math.min(this.responseVisibleLength, plain.length));
  }

  private toPlainNarration(value: string): string {
    if (!value) {
      return value;
    }

    const plain = value
      .replace(/```/g, '')
      .replace(/`([^`]+)`/g, '$1')
      .replace(/<\/?[a-z][^>]*>/gi, '')
      .replace(/^#{1,6}\s+/gm, '')
      .replace(/\*\*|__/g, '')
      .replace(/\*([^*\n]+)\*/g, '$1')
      .replace(/^\s*[-*+]\s+/gm, '')
      .replace(/\[([^\]]+)\]\([^)]+\)/g, '$1')
      .replace(/[ \t]+\n/g, '\n')
      .replace(/\n{3,}/g, '\n\n')
      .trim();
    return this.omitPastDates(plain);
  }

  private omitPastDates(value: string): string {
    const months = 'January|February|March|April|May|June|July|August|September|October|November|December|Jan|Feb|Mar|Apr|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec';
    const pattern = new RegExp(`\\b\\d{4}-\\d{2}-\\d{2}\\b|\\b\\d{1,2}\\s+(?:${months})\\s+\\d{4}\\b|\\b(?:${months})\\s+\\d{1,2},?\\s+\\d{4}\\b|\\b(?:${months})\\s+\\d{4}\\b`, 'gi');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const normalized = value.replace(/[\u00A0\u202F\u2007\u2009\u200A]/g, ' ');
    let cleaned = normalized.replace(pattern, (match) => this.isPastProseDate(match, today) ? '' : match);
    cleaned = cleaned.replace(/\b(?:19|20)\d{2}\b/g, (match) => Number(match) < today.getFullYear() ? '' : match);
    cleaned = cleaned
      .replace(/[ \t]{2,}/g, ' ')
      .replace(/\bbetween\s+and\b/gi, 'by')
      .replace(/\bfrom\s+to\b/gi, 'until')
      .replace(/\b(?:since|from|on)\s+(?=[,.]|$)/gim, '')
      .replace(/[ \t]{2,}/g, ' ')
      .replace(/\s+([,.;:])/g, '$1')
      .replace(/\(\s*\)/g, '')
      .trim();
    return cleaned;
  }

  private isPastProseDate(text: string, today: Date): boolean {
    const value = text.trim().replace(/\bSept\b/gi, 'Sep');
    const monthNames = ['jan', 'feb', 'mar', 'apr', 'may', 'jun', 'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
    const monthIndex = (name: string) => monthNames.indexOf(name.toLowerCase().slice(0, 3));
    const iso = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
    if (iso) {
      return new Date(Number(iso[1]), Number(iso[2]) - 1, Number(iso[3])) < today;
    }

    const dayFirst = /^(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})$/.exec(value);
    if (dayFirst) {
      const month = monthIndex(dayFirst[2]);
      return month >= 0 && new Date(Number(dayFirst[3]), month, Number(dayFirst[1])) < today;
    }

    const monthFirst = /^([A-Za-z]+)\s+(\d{1,2}),?\s+(\d{4})$/.exec(value);
    if (monthFirst) {
      const month = monthIndex(monthFirst[1]);
      return month >= 0 && new Date(Number(monthFirst[3]), month, Number(monthFirst[2])) < today;
    }

    const monthYear = /^([A-Za-z]+)\s+(\d{4})$/.exec(value);
    if (monthYear) {
      const month = monthIndex(monthYear[1]);
      const year = Number(monthYear[2]);
      return month >= 0 && (year < today.getFullYear() || (year === today.getFullYear() && month < today.getMonth()));
    }

    return false;
  }
  startTour(): void {
    const visibleSteps = this.visibleTourSteps(this.tourSteps);

    if (!visibleSteps.length) {
      return;
    }

    this.activeTourKind = 'portal';
    this.activePortalTourSteps = visibleSteps;
    this.activeReportTourSteps = [];
    this.rememberPanelForTour();
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


  private clearCloseTransitionTimer(): void {
    if (!this.closeTransitionTimer) {
      return;
    }

    clearTimeout(this.closeTransitionTimer);
    this.closeTransitionTimer = null;
  }
  visibleNarrationGlyphs(): string[] {
    return [...this.animatedResponseText(this.latestAnswer?.narration?.text || '')];
  }

  trackNarrationGlyph(index: number): number {
    return index;
  }

  narrationStillTyping(): boolean {
    const text = this.latestAnswer?.narration?.text || '';
    return text.length > 0 && this.responseVisibleLength < text.length && !this.isReducedMotionPreferred();
  }

  private beginResponseReveal(revealLength = 600, step = 4, intervalMs = 18): void {
    this.persistLocalChat();
    this.upgradeBlock = null;
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
        this.responseVisibleLength += step;
        this.markResponseAnimationForCheck();
        if (step > 1 && this.responseVisibleLength % 48 === 0) {
          this.scrollLatestResponseIntoView();
        }

        if (this.responseVisibleLength >= revealLength) {
          this.clearResponseTypingTimers(false);
        }
      }, intervalMs);
    }, 180);
  }

  shouldShowCustomQuestionDetails(customQuestion: DhruvaCustomQuestionView): boolean {
    if (customQuestion.state !== 'ready') {
      return false;
    }

    if (this.isTypingResponse) {
      return false;
    }

    const summaryLength = (customQuestion.summary ?? '').trim().length;
    const revealThreshold = Math.max(summaryLength, 88);
    return this.responseVisibleLength === Number.MAX_SAFE_INTEGER || this.responseVisibleLength >= revealThreshold;
  }

  shouldShowCustomQuestionTyping(customQuestion: DhruvaCustomQuestionView): boolean {
    return customQuestion.state === 'ready' && this.isTypingResponse;
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

  private finishCustomQuestionReveal(answer: DhruvaAnswer): void {
    this.clearCustomQuestionRevealTimer();

    if (this.isReducedMotionPreferred()) {
      this.latestAnswer = answer;
      this.persistLocalChat();
      this.isCustomQuestionLoading = false;
      this.isTypingResponse = false;
      this.responseVisibleLength = Number.MAX_SAFE_INTEGER;
      this.markResponseAnimationForCheck();
      this.scrollLatestResponseIntoView();
      return;
    }

    this.customQuestionRevealTimer = setTimeout(() => {
      this.customQuestionRevealTimer = null;
      this.latestAnswer = answer;
      this.isCustomQuestionLoading = false;
      this.beginResponseReveal();
    }, 420);
  }

  private clearCustomQuestionRevealTimer(): void {
    if (!this.customQuestionRevealTimer) {
      return;
    }

    clearTimeout(this.customQuestionRevealTimer);
    this.customQuestionRevealTimer = null;
  }
  private clearResponseTypingTimers(resetState = true): void {
    this.clearCustomQuestionRevealTimer();

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
      title: this.activePersonName ? `${this.activePersonName}'s Janma Story` : 'Your Janma Story',
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

    return `${label}: ${field.value.replace(/[.ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â£ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â€šÂ¬Ã…Â¡Ãƒâ€šÃ‚Â¬ÃƒÆ’Ã‚Â¢ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬Ãƒâ€¦Ã‚Â¡]+$/, '')}.`;
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
    this.restorePanelAfterTour();
  }

  private restorePanelAfterTour(): void {
    const panelState = this.panelStateBeforeTour;
    this.panelStateBeforeTour = null;

    if (!panelState?.isOpen) {
      return;
    }

    this.isOpen = true;
    this.isExpanded = panelState.isExpanded;
    this.isFullScreen = panelState.isFullScreen;
    this.setFullScreenScrollLock(panelState.isFullScreen);
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
    this.rememberPanelForTour();
    this.isOpen = false;
    this.isTourOpen = true;
    this.currentTourIndex = 0;
    this.updateTourPosition(true);
  }

  private rememberPanelForTour(): void {
    this.panelStateBeforeTour = { isOpen: this.isOpen, isExpanded: this.isExpanded, isFullScreen: this.isFullScreen };
    this.setFullScreenScrollLock(false);
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
    const user = this.readStoredUser();

    if (!user) {
      return null;
    }

    return typeof user.userId === 'string'
      ? user.userId
      : typeof user.email === 'string'
        ? user.email
        : null;
  }

  private currentUserDisplayName(): string | null {
    const user = this.readStoredUser();

    if (!user) {
      return null;
    }

    const directName = [user.firstName, user.fullName, user.name]
      .find((value): value is string => typeof value === 'string' && value.trim().length > 0);

    if (directName) {
      return directName.trim().split(/\s+/)[0];
    }

    if (typeof user.email === 'string' && user.email.includes('@')) {
      return user.email.split('@')[0].replace(/[._-]+/g, ' ').trim().split(/\s+/)[0] || null;
    }

    return null;
  }

  private readStoredUser(): { userId?: unknown; email?: unknown; firstName?: unknown; fullName?: unknown; name?: unknown; isPaid?: unknown; isPremium?: unknown; isPlus?: unknown; planCode?: unknown; planExpiresAt?: unknown } | null {
    if (!this.isBrowserStorageAvailable()) {
      return null;
    }

    const rawUser = window.localStorage.getItem('kundali_user');

    if (!rawUser) {
      return null;
    }

    try {
      return JSON.parse(rawUser) as { userId?: unknown; email?: unknown; firstName?: unknown; fullName?: unknown; name?: unknown };
    } catch {
      return null;
    }
  }

  private isBrowserStorageAvailable(): boolean {
    return typeof window !== 'undefined' && typeof window.localStorage !== 'undefined';
  }
}














































