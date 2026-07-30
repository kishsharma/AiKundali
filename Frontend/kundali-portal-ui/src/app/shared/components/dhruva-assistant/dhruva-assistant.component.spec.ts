import '@angular/compiler';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { DhruvaAssistantComponent } from './dhruva-assistant.component';
import { DhruvaKundaliContextService } from './dhruva-kundali-context.service';

interface FakeTourElement {
  classList: { add: ReturnType<typeof vi.fn>; remove: ReturnType<typeof vi.fn> };
  scrollIntoView: ReturnType<typeof vi.fn>;
  getBoundingClientRect: () => { top: number; left: number; width: number; height: number };
}

describe('DhruvaAssistantComponent', () => {
  let component: DhruvaAssistantComponent;
  let contextService: DhruvaKundaliContextService;
  let reportTargets: Map<string, FakeTourElement>;

  beforeEach(() => {
    vi.useRealTimers();
    reportTargets = new Map<string, FakeTourElement>();
    installBrowserFakes(() => reportTargets);
    window.localStorage.clear();
    contextService = new DhruvaKundaliContextService();
    component = new DhruvaAssistantComponent(contextService);
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('moves between floating, compact, and expanded panel states', () => {
    expect(component.isOpen).toBe(false);
    expect(component.isExpanded).toBe(false);

    component.open();
    expect(component.isOpen).toBe(true);
    expect(component.isExpanded).toBe(false);

    component.expand();
    expect(component.isOpen).toBe(true);
    expect(component.isExpanded).toBe(true);

    component.restoreCompact();
    expect(component.isOpen).toBe(true);
    expect(component.isExpanded).toBe(false);

    component.expand();
    component.close();
    expect(component.isOpen).toBe(false);
    expect(component.isExpanded).toBe(false);

    component.toggle();
    expect(component.isOpen).toBe(true);
    expect(component.isExpanded).toBe(false);
  });

  it('preserves Dhruva question and response state when switching panel modes', () => {
    contextService.setContext(buildContext());
    component.typedQuestion = 'What is my Moon sign?';
    component.submitTypedQuestion();
    const answer = component.displayedAnswer;

    component.expand();
    expect(component.typedQuestion).toBe('What is my Moon sign?');
    expect(component.displayedAnswer).toEqual(answer);

    component.restoreCompact();
    expect(component.typedQuestion).toBe('What is my Moon sign?');
    expect(component.displayedAnswer).toEqual(answer);
  });
  it('validates an empty typed question before matching', () => {
    component.typedQuestion = '   ';

    component.submitTypedQuestion();

    expect(component.typedQuestion).toBe('');
    expect(component.hasAskInputError).toBe(true);
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.personal.emptyQuestionAnswer' });
  });

  it('clears empty typed question validation after a valid question is submitted', () => {
    contextService.setContext(buildContext());
    component.typedQuestion = '   ';
    component.submitTypedQuestion();

    component.typedQuestion = 'What is my Moon sign?';
    component.submitTypedQuestion();

    expect(component.hasAskInputError).toBe(false);
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.moonSignAnswer',
      params: { value: 'Taurus (Vrishabha)' },
    });
  });

  it('shows the seven predefined general Jyotish questions', () => {
    expect(component.generalQuestions.map((question) => question.questionKey)).toEqual([
      'dhruva.general.kundaliQuestion',
      'dhruva.general.ascendantQuestion',
      'dhruva.general.nakshatraQuestion',
      'dhruva.general.dashaQuestion',
      'dhruva.general.mahadashaAntardashaQuestion',
      'dhruva.general.yogaQuestion',
      'dhruva.general.doshaQuestion',
    ]);
  });

  it('maps each predefined question to its approved explanation key', () => {
    expect(component.generalQuestions.map((question) => question.answerKey)).toEqual([
      'dhruva.general.kundaliAnswer',
      'dhruva.general.ascendantAnswer',
      'dhruva.general.nakshatraAnswer',
      'dhruva.general.dashaAnswer',
      'dhruva.general.mahadashaAntardashaAnswer',
      'dhruva.general.yogaAnswer',
      'dhruva.general.doshaAnswer',
    ]);
  });

  it('keeps general questions open without Kundali context and collapsed by default with Kundali context', () => {
    expect(component.shouldShowGeneralQuestions).toBe(true);

    contextService.setContext(buildContext());

    expect(component.shouldShowGeneralQuestions).toBe(false);

    component.toggleGeneralQuestions();
    expect(component.shouldShowGeneralQuestions).toBe(true);
  });

  it('changes the selected general question and shared response', () => {
    expect(component.selectedGeneralQuestion.answerKey).toBe('dhruva.general.kundaliAnswer');
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.general.kundaliAnswer' });

    component.selectGeneralQuestion('dosha');

    expect(component.selectedGeneralQuestion.questionKey).toBe('dhruva.general.doshaQuestion');
    expect(component.selectedGeneralQuestion.answerKey).toBe('dhruva.general.doshaAnswer');
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.general.doshaAnswer' });
  });

  it('shows personal suggestions only when normalized Kundali context is available', () => {
    expect(component.hasKundaliContext).toBe(false);

    contextService.setContext(buildContext());

    expect(component.hasKundaliContext).toBe(true);
    expect(component.personalQuestions.map((question) => question.questionKey)).toEqual([
      'dhruva.personal.ascendantQuestion',
      'dhruva.personal.moonSignQuestion',
      'dhruva.personal.sunSignQuestion',
      'dhruva.personal.nakshatraQuestion',
      'dhruva.personal.mahadashaQuestion',
      'dhruva.personal.antardashaQuestion',
      'dhruva.personal.presentYogasQuestion',
      'dhruva.personal.presentDoshasQuestion',
      'dhruva.personal.strongestPlanetsQuestion',
      'dhruva.personal.panchangTithiQuestion',
      'dhruva.personal.panchangYogaQuestion',
      'dhruva.personal.moonPlacementQuestion',
      'dhruva.personal.saturnRetrogradeQuestion',
    ]);
  });

  it('answers personal suggestions from normalized values in the shared response area', () => {
    contextService.setContext(buildContext());

    component.selectPersonalQuestion('moon-sign');

    expect(component.selectedPersonalAnswer).toEqual({
      key: 'dhruva.personal.moonSignAnswer',
      params: { value: 'Taurus (Vrishabha)' },
    });
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.moonSignAnswer',
      params: { value: 'Taurus (Vrishabha)' },
    });
  });

  it('formats raw sign values without exposing sign numbers', () => {
    contextService.setContext(buildContext());

    component.selectPersonalQuestion('ascendant');

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.ascendantAnswer',
      params: { value: 'Taurus (Vrishabha)' },
    });
  });

  it('answers Yoga, Dosha and strength suggestions from normalized values', () => {
    contextService.setContext(buildContext());

    component.selectPersonalQuestion('present-yogas');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.presentYogasAnswer',
      params: { value: 'Gaja Kesari Yoga, Budhaditya Yoga' },
    });

    component.selectPersonalQuestion('present-doshas');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.presentDoshasAnswer',
      params: { value: 'Manglik Dosha' },
    });

    component.selectPersonalQuestion('strongest-planets');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.strongestPlanetsAnswer',
      params: { value: 'Sun (Exalted), Mars (Own Sign)' },
    });
  });

  it('matches typed personal question variations deterministically', () => {
    contextService.setContext(buildContext());
    component.typedQuestion = 'Which antar dasha is running now?';

    component.submitTypedQuestion();

    expect(component.selectedPersonalQuestionId).toBe('antardasha');
    expect(component.typedAnswer).toEqual({
      key: 'dhruva.personal.antardashaAnswer',
      params: { value: 'Mercury' },
    });
    expect(component.displayedAnswer).toEqual(component.typedAnswer);
  });

  it('matches typed Yoga, Dosha and strongest planet questions deterministically', () => {
    contextService.setContext(buildContext());

    component.typedQuestion = 'What doshas are present in my kundali?';
    component.submitTypedQuestion();

    expect(component.selectedPersonalQuestionId).toBe('present-doshas');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.presentDoshasAnswer',
      params: { value: 'Manglik Dosha' },
    });

    component.typedQuestion = 'Which planets are strongest?';
    component.submitTypedQuestion();

    expect(component.selectedPersonalQuestionId).toBe('strongest-planets');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.strongestPlanetsAnswer',
      params: { value: 'Sun (Exalted), Mars (Own Sign)' },
    });
  });

  it('matches typed Panchang questions deterministically', () => {
    contextService.setContext(buildContext());

    const panchangCases = [
      ['What is my Maas?', 'Maas', 'Chaitra'],
      ['What is my Paksha?', 'Paksha', 'Shukla'],
      ['What is my Tithi?', 'Tithi', 'Dwitiya'],
      ['What is my Nakshatra Pada?', 'Nakshatra Pada', 'Rohini, Pada 2'],
      ['What is my Panchang Yoga?', 'Panchang Yoga', 'Siddhi'],
      ['What is my Karana?', 'Karana', 'Bava'],
      ['What is my Vaar?', 'Vaar', 'Monday'],
    ];

    panchangCases.forEach(([question, label, value]) => {
      component.typedQuestion = question;
      component.submitTypedQuestion();

      expect(component.displayedAnswer).toEqual({
        key: 'dhruva.personal.panchangFieldAnswer',
        params: { label, value },
      });
    });
  });

  it('matches generic typed planet fact questions for any supported planet', () => {
    contextService.setContext(buildContext());

    component.typedQuestion = 'What house is Mars in?';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.planetFactAnswer',
      params: { planet: 'Mars', fact: 'house', value: '3' },
    });

    component.typedQuestion = 'Is Saturn retrograde?';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.planetFactAnswer',
      params: { planet: 'Saturn', fact: 'retrograde status', value: 'retrograde' },
    });
  });

  it('answers typed Yoga and Dosha detail questions from normalized detail fields', () => {
    contextService.setContext(buildContext());

    component.typedQuestion = 'Tell me about Gaja Kesari Yoga details';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.resultDetailAnswer',
      params: {
        result: 'Gaja Kesari Yoga',
        kind: 'Yoga',
        value: 'Category: Benefic. Strength: Strong. Reason: Moon and Jupiter are in a supportive relationship. Description: A supportive Yoga shown in the calculated report. Planets: Moon, Jupiter.',
      },
    });

    component.typedQuestion = 'What is Manglik Dosha severity?';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.resultDetailAnswer',
      params: {
        result: 'Manglik Dosha',
        kind: 'Dosha',
        value: 'Category: Relationship. Severity: Medium. Reason: Mars is placed in a sensitive house. Description: A calculated Dosha condition shown in this report. Planets: Mars.',
      },
    });
  });

  it('answers typed house questions with one generic handler', () => {
    contextService.setContext(buildContext());

    component.typedQuestion = 'Tell me about my marriage house';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.houseAnswer',
      params: {
        house: '7th house (Marriage)',
        value: 'Sign: Scorpio. Lord: Mars. Occupants: Saturn. Score: 48/100. Status: Challenging.',
      },
    });

    component.typedQuestion = 'Tell me about my career house';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.houseAnswer',
      params: {
        house: '10th house (Career)',
        value: 'Sign: Aquarius. Lord: Saturn. Score: 82/100. Status: Excellent. Themes: Career and public work.',
      },
    });
  });

  it('answers house summaries and asks for clarification on ambiguous house terms', () => {
    contextService.setContext(buildContext());

    component.typedQuestion = 'Which are my strongest houses?';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.housesSummaryAnswer',
      params: { label: 'strongest houses', value: '10th house (Career) - 82/100' },
    });

    component.typedQuestion = 'Tell me about growth';
    component.submitTypedQuestion();

    expect(component.displayedAnswer).toEqual({ key: 'dhruva.personal.ambiguousHouseAnswer' });
  });

  it('answers planet suggestion examples from normalized planet fields', () => {
    contextService.setContext(buildContext());

    component.selectPersonalQuestion('moon-placement');
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.planetPlacementAnswer',
      params: { planet: 'Moon', fact: 'sign', value: 'Taurus (Vrishabha)' },
    });
  });

  it('returns unavailable response when a normalized value is missing', () => {
    contextService.setContext({
      ...buildContext(),
      sunSign: { value: null, status: 'missing' },
      presentYogas: { value: null, status: 'missing' },
    });

    component.selectPersonalQuestion('sun-sign');

    expect(component.selectedPersonalAnswer).toEqual({ key: 'dhruva.personal.sunSignUnavailableAnswer' });
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.personal.sunSignUnavailableAnswer' });

    component.selectPersonalQuestion('present-yogas');
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.personal.presentYogasUnavailableAnswer' });
  });

  it('does not answer personal chart values when no Kundali context is loaded', () => {
    component.typedQuestion = 'What is my ascendant?';

    component.submitTypedQuestion();

    expect(component.typedAnswer).toEqual({ key: 'dhruva.personal.noKundaliAnswer' });
    expect(component.displayedAnswer).toEqual({ key: 'dhruva.personal.noKundaliAnswer' });
  });

  it('starts, skips, and restarts the portal tour without changing its steps', () => {
    renderPortalTargets(reportTargets);
    component.open();

    component.startTour();
    expect(component.activeTourKind).toBe('portal');
    expect(component.activeTourSteps.map((step) => step.id)).toEqual(component.tourSteps.map((step) => step.id));
    expect(component.isOpen).toBe(false);
    expect(component.isTourOpen).toBe(true);
    expect(component.currentTourIndex).toBe(0);

    component.skipTour();
    expect(component.isTourOpen).toBe(false);

    component.startTour();
    expect(component.isTourOpen).toBe(true);
    expect(component.currentTourIndex).toBe(0);
  });

  it('moves through tour steps and finishes on the last step', () => {
    renderPortalTargets(reportTargets);
    component.startTour();

    component.nextTourStep();
    expect(component.currentTourIndex).toBe(1);

    component.previousTourStep();
    expect(component.currentTourIndex).toBe(0);

    component.currentTourIndex = component.activeTourSteps.length - 1;
    component.nextTourStep();
    expect(component.isTourOpen).toBe(false);
  });

  it('skips missing portal tour targets safely', () => {
    component.startTour();

    expect(component.isTourOpen).toBe(false);
    expect(component.activeTourSteps).toEqual([]);
  });

  it('does not start the Kundali report tour without loaded Kundali context', () => {
    renderReportTargets(reportTargets);

    component.startKundaliReportTour();

    expect(component.isTourOpen).toBe(false);
    expect(component.activeTourKind).toBe('portal');
  });

  it('starts the Kundali report tour only with rendered report sections and skips missing sections', () => {
    renderReportTargets(reportTargets, ['[data-tour-id="kundali-report-actions"]', '[data-tour-id="kundali-report-summary"]', '[data-tour-id="kundali-report-planets"]', '[data-tour-id="kundali-report-dasha"]']);
    contextService.setContext(buildContext());

    component.startKundaliReportTour();

    expect(component.activeTourKind).toBe('kundali-report');
    expect(component.isTourOpen).toBe(true);
    expect(component.activeTourSteps.map((step) => step.id)).toEqual([
      'report-download',
      'chart-summary',
      'planetary-positions',
      'dasha',
    ]);
  });

  it('stores Kundali report tour completion separately from the portal tour', () => {
    renderReportTargets(reportTargets, ['[data-tour-id="kundali-report-actions"]', '[data-tour-id="kundali-report-summary"]']);
    contextService.setContext(buildContext());

    component.startKundaliReportTour();
    component.skipTour();

    expect(window.localStorage.getItem('dhruva.kundaliReportTour.completed')).toBe('true');
    expect(window.localStorage.getItem('dhruva.portalTour.completed')).toBeNull();
  });

  it('automatically starts the first Kundali report tour when context becomes available', () => {
    vi.useFakeTimers();
    installBrowserFakes(() => reportTargets);
    renderReportTargets(reportTargets, ['[data-tour-id="kundali-report-actions"]', '[data-tour-id="kundali-report-summary"]']);
    contextService.setContext(buildContext());

    component.ngDoCheck();
    vi.advanceTimersByTime(450);

    expect(component.activeTourKind).toBe('kundali-report');
    expect(component.isTourOpen).toBe(true);
  });
  it('briefly shows typing before progressively revealing a selected general Dhruva response', () => {
    vi.useFakeTimers();

    component.selectGeneralQuestion('ascendant');

    expect(component.isTypingResponse).toBe(true);
    expect(component.responseVisibleLength).toBe(0);

    vi.advanceTimersByTime(180);
    expect(component.isTypingResponse).toBe(false);
    expect(component.animatedResponseText('Dhruva response')).toBe('');

    vi.advanceTimersByTime(18);
    expect(component.animatedResponseText('Dhruva response')).toBe('Dhru');

    vi.advanceTimersByTime(18);
    expect(component.animatedResponseText('Dhruva response')).toBe('Dhruva r');
  });

  it('scrolls the latest Dhruva response into view when typing begins', () => {
    vi.useFakeTimers();
    installBrowserFakes(() => reportTargets);
    const responseTarget = createFakeElement();
    reportTargets.set('[data-dhruva-response]', responseTarget);

    component.selectGeneralQuestion('ascendant');
    vi.advanceTimersByTime(0);

    expect(responseTarget.scrollIntoView).toHaveBeenCalledWith({ block: 'nearest', inline: 'nearest' });
  });


  it('progressively reveals typed Kundali Dhruva answers after the typing indicator', () => {
    vi.useFakeTimers();
    contextService.setContext(buildContext());
    component.typedQuestion = 'What is my Moon sign?';

    component.submitTypedQuestion();

    expect(component.isTypingResponse).toBe(true);
    expect(component.displayedAnswer).toEqual({
      key: 'dhruva.personal.moonSignAnswer',
      params: { value: 'Taurus (Vrishabha)' },
    });

    vi.advanceTimersByTime(180);
    expect(component.isTypingResponse).toBe(false);
    expect(component.animatedResponseText('Your Moon sign is Taurus (Vrishabha).')).toBe('');

    vi.advanceTimersByTime(36);
    expect(component.animatedResponseText('Your Moon sign is Taurus (Vrishabha).')).toBe('Your Moo');
  });

  it('cancels the previous typing animation when a new question is selected', () => {
    vi.useFakeTimers();

    component.selectGeneralQuestion('ascendant');
    vi.advanceTimersByTime(100);

    component.selectGeneralQuestion('dosha');
    vi.advanceTimersByTime(179);

    expect(component.isTypingResponse).toBe(true);
    expect(component.responseVisibleLength).toBe(0);

    vi.advanceTimersByTime(1);
    expect(component.isTypingResponse).toBe(false);
  });

  it('shows the full Dhruva response immediately when reduced motion is preferred', () => {
    window.matchMedia = vi.fn().mockReturnValue({ matches: true });

    component.selectGeneralQuestion('dasha');

    expect(component.isTypingResponse).toBe(false);
    expect(component.animatedResponseText('Dhruva response')).toBe('Dhruva response');
  });
});

function buildContext() {
  return {
    kundaliId: { value: 'kundali-1', status: 'available' as const },
    schemaVersion: { value: 'test-schema', status: 'available' as const },
    calculationVersion: { value: 'test-calculation', status: 'available' as const },
    ascendant: { value: '2 - Taurus', status: 'available' as const },
    moonSign: { value: 'Taurus', status: 'available' as const },
    sunSign: { value: 'Gemini', status: 'available' as const },
    birthNakshatra: { value: 'Rohini', status: 'available' as const },
    currentMahadasha: { value: 'Jupiter', status: 'available' as const },
    currentAntardasha: { value: 'Mercury', status: 'available' as const },
    presentYogas: { value: 'Gaja Kesari Yoga, Budhaditya Yoga', status: 'available' as const },
    presentDoshas: { value: 'Manglik Dosha', status: 'available' as const },
    strongestPlanets: { value: 'Sun (Exalted), Mars (Own Sign)', status: 'available' as const },
    panchangMaas: { value: 'Chaitra', status: 'available' as const },
    panchangPaksha: { value: 'Shukla', status: 'available' as const },
    panchangTithi: { value: 'Dwitiya', status: 'available' as const },
    panchangNakshatraPada: { value: 'Rohini, Pada 2', status: 'available' as const },
    panchangYoga: { value: 'Siddhi', status: 'available' as const },
    panchangKarana: { value: 'Bava', status: 'available' as const },
    panchangVaar: { value: 'Monday', status: 'available' as const },
    yogas: [
      {
        name: 'Gaja Kesari Yoga',
        description: { value: 'A supportive Yoga shown in the calculated report.', status: 'available' as const },
        reason: { value: 'Moon and Jupiter are in a supportive relationship.', status: 'available' as const },
        category: { value: 'Benefic', status: 'available' as const },
        strengthOrSeverity: { value: 'Strong', status: 'available' as const },
        involvedPlanets: { value: 'Moon, Jupiter', status: 'available' as const },
        involvedHouses: { value: '1, 5', status: 'available' as const },
        impactAreas: { value: 'Wisdom, support', status: 'available' as const },
        activationPeriod: { value: 'Jupiter Mahadasha', status: 'available' as const },
      },
    ],
    doshas: [
      {
        name: 'Manglik Dosha',
        description: { value: 'A calculated Dosha condition shown in this report.', status: 'available' as const },
        reason: { value: 'Mars is placed in a sensitive house.', status: 'available' as const },
        category: { value: 'Relationship', status: 'available' as const },
        strengthOrSeverity: { value: 'Medium', status: 'available' as const },
        involvedPlanets: { value: 'Mars', status: 'available' as const },
        involvedHouses: { value: '7', status: 'available' as const },
        impactAreas: { value: 'Marriage', status: 'available' as const },
        activationPeriod: { value: null, status: 'missing' as const },
      },
    ],
    houses: [
      {
        houseNumber: 7,
        label: 'Marriage',
        sign: { value: 'Scorpio', status: 'available' as const },
        lord: { value: 'Mars', status: 'available' as const },
        occupants: { value: 'Saturn', status: 'available' as const },
        score: { value: '48/100', status: 'available' as const },
        status: { value: 'Challenging', status: 'available' as const },
        themes: { value: 'Partnerships and agreements', status: 'available' as const },
        lifeAreas: { value: 'Marriage, spouse', status: 'available' as const },
        supportingFactors: { value: 'Jupiter aspect', status: 'available' as const },
        pressureFactors: { value: 'Saturn occupancy', status: 'available' as const },
      },
      {
        houseNumber: 10,
        label: 'Career',
        sign: { value: 'Aquarius', status: 'available' as const },
        lord: { value: 'Saturn', status: 'available' as const },
        occupants: { value: null, status: 'missing' as const },
        score: { value: '82/100', status: 'available' as const },
        status: { value: 'Excellent', status: 'available' as const },
        themes: { value: 'Career and public work', status: 'available' as const },
        lifeAreas: { value: 'Career, profession', status: 'available' as const },
        supportingFactors: { value: 'Strong lord', status: 'available' as const },
        pressureFactors: { value: null, status: 'missing' as const },
      },
    ],
    strongestHouses: { value: '10th house (Career) - 82/100', status: 'available' as const },
    supportNeededHouses: { value: '7th house (Marriage) - 48/100', status: 'available' as const },    planets: [
      {
        name: 'Moon',
        sign: { value: 'Taurus', status: 'available' as const },
        house: { value: '1', status: 'available' as const },
        degree: { value: "3Â° 10'", status: 'available' as const },
        nakshatraPada: { value: 'Rohini, Pada 2', status: 'available' as const },
        retrogradeStatus: { value: 'not retrograde', status: 'available' as const },
        combustionStatus: { value: 'not combust', status: 'available' as const },
        strength: { value: 'Neutral', status: 'available' as const },
      },
      {
        name: 'Mars',
        sign: { value: 'Cancer', status: 'available' as const },
        house: { value: '3', status: 'available' as const },
        degree: { value: "18Â° 20'", status: 'available' as const },
        nakshatraPada: { value: 'Pushya, Pada 1', status: 'available' as const },
        retrogradeStatus: { value: 'not retrograde', status: 'available' as const },
        combustionStatus: { value: 'not combust', status: 'available' as const },
        strength: { value: 'Own Sign', status: 'available' as const },
      },
      {
        name: 'Saturn',
        sign: { value: 'Aquarius', status: 'available' as const },
        house: { value: '10', status: 'available' as const },
        degree: { value: "22Â° 00'", status: 'available' as const },
        nakshatraPada: { value: 'Shatabhisha, Pada 4', status: 'available' as const },
        retrogradeStatus: { value: 'retrograde', status: 'available' as const },
        combustionStatus: { value: 'not combust', status: 'available' as const },
        strength: { value: 'Own Sign', status: 'available' as const },
      },
    ],
  };
}

function installBrowserFakes(targets: () => Map<string, FakeTourElement>): void {
  const storage = new Map<string, string>();

  (globalThis as any).window = {
    setTimeout: globalThis.setTimeout.bind(globalThis),
    localStorage: {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => storage.set(key, value),
      clear: () => storage.clear(),
    },
    matchMedia: vi.fn().mockReturnValue({ matches: false }),
  };

  (globalThis as any).document = {
    querySelector: (selector: string) => targets().get(selector) ?? null,
    querySelectorAll: () => Array.from(targets().values()),
  };
}

function renderPortalTargets(targets: Map<string, FakeTourElement>): void {
  renderReportTargets(targets, [
    '[data-tour-id="portal-dashboard"]',
    '[data-tour-id="portal-create-kundali"]',
    '[data-tour-id="portal-saved-kundalis"]',
    '[data-tour-id="portal-compatibility"]',
    '[data-tour-id="portal-muhurtha"]',
    '[data-tour-id="portal-learning"]',
    '[data-tour-id="portal-dhruva-assistant"]',
  ]);
}

function renderReportTargets(targets: Map<string, FakeTourElement>, selectors = [
  '[data-tour-id="kundali-report-actions"]',
  '[data-tour-id="kundali-report-summary"]',
  '[data-tour-id="kundali-report-planets"]',
  '[data-tour-id="kundali-report-dasha"]',
  '[data-tour-id="kundali-report-yogas"]',
  '[data-tour-id="kundali-report-doshas"]',
  '[data-tour-id="kundali-report-strength"]',
]): void {
  targets.clear();
  selectors.forEach((selector) => targets.set(selector, createFakeElement()));
}

function createFakeElement(): FakeTourElement {
  return {
    classList: { add: vi.fn(), remove: vi.fn() },
    scrollIntoView: vi.fn(),
    getBoundingClientRect: () => ({ top: 24, left: 32, width: 240, height: 80 }),
  };
}








