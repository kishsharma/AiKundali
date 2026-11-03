export type LearningLanguage = 'en' | 'hi' | 'kn';

export interface LearningLevelSummary {
  id: string;
  code: string;
  slug: string;
  title: string;
  subtitle?: string;
  description: string;
  sessionCount: number;
  estimatedMinutes: number;
  estimatedHours: number;
  languages: LearningLanguages;
  status: string;
}

export interface LearningLevelManifest extends LearningLevelSummary {
  audience: string[];
  learningOutcome: string;
  recommendedPace?: LearningRecommendedPace;
  access?: LearningAccess;
  navigation?: LearningNavigation;
  contentRules?: LearningContentRules;
  completionRules?: LearningCompletionRules;
  sessions: LearningSessionSummary[];
}

export interface LearningRecommendedPace {
  sessionsPerWeek: number;
  estimatedWeeks: number;
  note?: string;
}

export interface LearningAccess {
  requiredRoles: string[];
  authenticationRequired: boolean;
  allowPublicPreview: boolean;
  publicPreviewSessionIds: string[];
}

export interface LearningLanguages {
  default: LearningLanguage;
  supported: LearningLanguage[];
}

export interface LearningNavigation {
  route?: string;
  sessionRoutePattern?: string;
  showProgress: boolean;
  showLockedSessions: boolean;
  allowSequentialNavigation: boolean;
}

export interface LearningContentRules {
  sessionSchema?: string;
  referenceLibrary?: string;
  oneJsonPerSessionPerLanguage: boolean;
  maximumShlokasPerSession: number;
  shlokaDisplayMode?: string;
  referencesRequired: boolean;
  commonMistakesRequired: boolean;
  selfCheckRequired: boolean;
}

export interface LearningCompletionRules {
  requireAllSessions: boolean;
  minimumQuizScorePercent: number;
  certificateEnabled: boolean;
  completionMessage?: string;
}

export interface LearningSessionSummary {
  id: string;
  sessionNumber: number;
  slug: string;
  title: string;
  estimatedMinutes: number;
  difficulty: string;
  status: string;
  languages: Record<string, string>;
  contentPathPattern?: string;
  prerequisiteSessionIds: string[];
  isPreviewAvailable: boolean;
}

export interface LearningSessionResponse {
  resolvedLanguage: LearningLanguage;
  usedFallback: boolean;
  manifestSession?: LearningSessionSummary;
  session: LearningSession;
  glossary: LearningGlossaryEntry[];
}

export interface LearningGlossaryEntry {
  key: string;
  term: Partial<Record<LearningLanguage, string>>;
  transliteration?: string;
  definition: Partial<Record<LearningLanguage, string>>;
}
export interface LearningSession {
  schemaVersion: string;
  id: string;
  levelCode: string;
  sessionNumber: number;
  slug: string;
  language: LearningLanguage;
  title: string;
  subtitle?: string;
  summary: string;
  estimatedMinutes: number;
  difficulty: string;
  learningObjectives: string[];
  prerequisites: string[];
  keyTerms: LearningKeyTerm[];
  tableOfContents: LearningTocItem[];
  sections: LearningSection[];
  commonMistakes: LearningCommonMistake[];
  practicalExercise?: LearningPracticalExercise;
  sessionSummary: string[];
  selfCheck?: LearningSelfCheck;
  references: LearningReference[];
  shlokaReferences: LearningShlokaReference[];
  furtherReading: LearningFurtherReading[];
  contentReview?: LearningContentReview;
  status: string;
}

export interface LearningKeyTerm {
  term: string;
  sanskrit?: string;
  transliteration?: string;
  meaning: string;
  note?: string;
}

export interface LearningTocItem {
  sectionId: string;
  title: string;
  estimatedMinutes: number;
}

export interface LearningSection {
  id: string;
  title: string;
  estimatedMinutes: number;
  blocks: LearningContentBlock[];
}

export type LearningContentBlock =
  | HeadingBlock
  | ParagraphBlock
  | ListBlock
  | DefinitionBlock
  | FormulaBlock
  | DiagramBlock
  | TableBlock
  | ExampleBlock
  | NoteBlock
  | ComparisonBlock
  | DividerBlock
  | ShlokaTriggerBlock;

export interface HeadingBlock { type: 'heading'; level: 2 | 3 | 4; text: string; }
export interface ParagraphBlock { type: 'paragraph'; text: string; }
export interface ListBlock { type: 'list'; style: 'bullet' | 'numbered' | 'check'; items: string[]; }
export interface DefinitionBlock { type: 'definition'; term: string; sanskrit?: string; transliteration?: string; meaning: string; context?: string; }
export interface FormulaBlock { type: 'formula'; expression: string; explanation: string; workedExample?: string; }
export interface DiagramBlock { type: 'diagram'; assetId: string; assetPath?: string; altText: string; caption: string; purpose: string; annotations?: string[]; }
export interface TableBlock { type: 'table'; caption: string; columns: string[]; rows: unknown[][]; note?: string; }
export interface ExampleBlock { type: 'example'; title: string; scenario: string; steps: string[]; conclusion: string; }
export interface NoteBlock { type: 'note'; style: 'important' | 'remember' | 'caution' | 'tradition' | 'modern-context'; title: string; text: string; }
export interface ComparisonBlock { type: 'comparison'; title: string; left: ComparisonSide; right: ComparisonSide; }
export interface ComparisonSide { label: string; points: string[]; }
export interface DividerBlock { type: 'divider'; }
export interface ShlokaTriggerBlock { type: 'shloka-trigger'; shlokaId: string; label: string; icon?: string; displayMode: 'collapsed'; }

export interface LearningCommonMistake {
  mistake: string;
  whyItIsWrong: string;
  correctUnderstanding: string;
}

export interface LearningPracticalExercise {
  title: string;
  instructions: string[];
  expectedOutcome?: string;
  estimatedMinutes: number;
}

export interface LearningSelfCheck {
  questions: LearningQuestion[];
  passingScorePercent: number;
}

export interface LearningQuestion {
  id: string;
  type: string;
  question: string;
  options: string[];
  answer: unknown;
  explanation: string;
}

export interface LearningReference {
  sourceId: string;
  editionId?: string;
  title: string;
  author?: string;
  supports: string;
  chapterTitle?: string;
  chapterNumberAsPrinted?: string;
  verseNumberAsPrinted?: string;
  pageNumber?: string;
  verificationStatus?: string;
  url?: string;
  notes?: string;
}

export interface LearningKnowledgeMapNode {
  id: string;
  cluster: string;
  title: string;
  order: number;
  x: number;
  y: number;
  sessionIds: string[];
}

export interface LearningKnowledgeMapConnection {
  from: string;
  to: string;
  label: string;
}

export interface LearningKnowledgeMap {
  language: LearningLanguage;
  nodes: LearningKnowledgeMapNode[];
  connections: LearningKnowledgeMapConnection[];
}

export interface LearningKnowledgeReferenceDetails {
  yogas: LearningYogaReference[];
  doshas: LearningDoshaReference[];
  divisionalCharts: LearningDivisionalChartReference[];
  predictionAreas: Record<string, LearningPredictionAreaReference>;
  rashis: LearningRashiReference[];
  nakshatras: LearningNakshatraReference[];
  grahas: LearningGrahaReference[];
  vimshottari: LearningVimshottariReference[];
  vimshottariTotalYears: number;
}

export interface LearningPadaReference {
  number: number;
  rashiId: number;
  startDegree: string;
  endDegree: string;
  navamsa: string;
  syllable: string;
  akshara: string;
  nakshatra?: string;
}

export interface LearningRashiReference {
  id: number; name: string; englishName: string; symbol: string; element: string; modality: string;
  gender: string; nature: string; ruler: string; direction: string; bodyPart: string; quality: string;
  personality: string; strengths: string[]; weaknesses: string[]; career: string[]; color: string;
  relationships: string; health: string; gemstone: string; metal: string; luckyNumbers: string; favorableDirection: string; favorableDay: string;
  charans: LearningPadaReference[];
}

export interface LearningNakshatraReference {
  id: number; name: string; sanskritName: string; lord: string; rashi: string; deity: string; symbol: string;
  gana: string; yoni: string; nadi: string; varna: string; vashya: string; guna: string; tatva: string;
  animalSymbol: string; gender: string; direction: string; fastingDay: string; bodyParts: string[];
  positiveTraits: string[]; negativeTraits: string[]; strengths: string[]; weaknesses: string[];
  motivation: string; nature: string; dosha?: { vata: string; pitta: string; kapha: string };
  padas: LearningPadaReference[];
}

export interface LearningGrahaReference {
  name: string; sanskritName: string; nature: string; classification: string; gender: string; tattva: string;
  guna: string; karakatva: string[]; ownSigns: string[]; exaltation: string; debilitation: string;
  moolatrikona: string; friends: string[]; enemies: string[]; neutral: string[];
}

export interface LearningVimshottariReference { order: number; planet: string; years: number; }

export interface LearningYogaReference {
  key: string;
  name: string;
  category: string;
  nature: string;
  description: string;
  impact: string;
  references: string[];
}

export interface LearningDoshaReference {
  key: string;
  name: string;
  category: string;
  nature: string;
  description: string;
  remedies: string[];
  references: string[];
}

export interface LearningDivisionalChartReference {
  code: string;
  name: string;
  sanskritName?: string;
  division: number;
  group?: string;
  priority?: string;
  primaryDomains: string[];
  predictionRole?: string;
  crossValidation: string[];
  birthTimeRequirement?: string;
}

export interface LearningPredictionAreaReference {
  primaryCharts: string[];
  supportingCharts: string[];
  timing: string[];
  requiredHouses: number[];
  requiredPlanets: string[];
  note?: string;
}

export interface LearningShlokaReference {
  id: string;
  sourceId?: string;
  text?: string;
  transliteration?: string;
  translation?: string;
  context?: string;
}

export interface LearningFurtherReading {
  title: string;
  type: string;
  author?: string;
  note?: string;
  url?: string;
}

export interface LearningContentReview {
  authenticityChecked: boolean;
  referencesChecked: boolean;
  languageReviewed: boolean;
  reviewedBy?: string;
  reviewedOn?: string;
  reviewNotes?: string;
}




