import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { Subscription, startWith, switchMap } from 'rxjs';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { HoraverseGeometryService } from './horaverse-geometry.service';
import { HoraverseStoryService } from './horaverse-story.service';
import { HoraverseInsightService } from './horaverse-insight.service';
import {
  HoraverseAspectRay,
  HoraverseDashaInsight,
  HoraverseHouseArc,
  HoraverseHouseInsight,
  HoraversePlanetPoint,
  HoraverseScene,
  HoraverseSession
} from './horaverse-models';

type Star = {
  x: number;
  y: number;
  radius: number;
  speed: number;
  alpha: number;
};

type PlanetVoiceProfile = {
  pitch: number;
  rate: number;
  volume: number;
  color: string;
  temperament: string;
};

type ZodiacArcLabel = {
  house: number;
  sign: string;
  path: string;
  pathId: string;
};

@Component({
  selector: 'app-horaverse-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './horaverse-page.component.html',
  styleUrl: './horaverse-page.component.scss'
})
export class HoraversePageComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('starfield') private starfield?: ElementRef<HTMLCanvasElement>;

  session: HoraverseSession | null = null;
  scenes: HoraverseScene[] = [];
  activeSceneIndex = 0;
  loadError = '';
  houseArcs: HoraverseHouseArc[] = [];
  zodiacArcLabels: ZodiacArcLabel[] = [];
  planetPoints: HoraversePlanetPoint[] = [];
  houseInsights: HoraverseHouseInsight[] = [];
  aspectRays: HoraverseAspectRay[] = [];
  dashaInsight: HoraverseDashaInsight | null = null;
  focusedPlanetName = '';
  focusedHouseNumber = 1;
  musicEnabled = false;
  voiceEnabled = false;
  voiceAvailable = typeof window !== 'undefined' && 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
  isSpeaking = false;
  activeVoiceLabel = '';

  readonly viewBox = '0 0 600 600';
  readonly center = 300;
  readonly planetToneColors: Record<string, string> = {
    moon: '#62e9ff',
    saturn: '#b36bff',
    challenging: '#ff6a7a',
    supportive: '#67f5bf',
    neutral: '#f7bf63'
  };
  readonly planetColors: Record<string, string> = {
    sun: '#f7bf63',
    moon: '#62e9ff',
    mars: '#ff6a7a',
    mercury: '#67f5bf',
    jupiter: '#ffd27a',
    venus: '#ff9bd2',
    saturn: '#b36bff',
    rahu: '#c890ff',
    ketu: '#54f0d7'
  };
  private readonly ascendantWatermarkSymbols: Record<string, string> = {
    aries: '\u{1F40F}',
    mesha: '\u{1F40F}',
    taurus: '\u{1F402}',
    vrishabha: '\u{1F402}',
    gemini: '\u{1F465}',
    mithuna: '\u{1F465}',
    cancer: '\u{1F980}',
    karka: '\u{1F980}',
    karkataka: '\u{1F980}',
    leo: '\u{1F981}',
    simha: '\u{1F981}',
    sinh: '\u{1F981}',
    virgo: '\u{1F33E}',
    kanya: '\u{1F33E}',
    libra: '\u2696',
    tula: '\u2696',
    scorpio: '\u{1F982}',
    vrischika: '\u{1F982}',
    vrishchika: '\u{1F982}',
    sagittarius: '\u{1F3F9}',
    dhanu: '\u{1F3F9}',
    capricorn: '\u{1F410}',
    makara: '\u{1F410}',
    aquarius: '\u{1F3FA}',
    kumbha: '\u{1F3FA}',
    pisces: '\u{1F41F}\u{1F41F}',
    meena: '\u{1F41F}\u{1F41F}',
    meen: '\u{1F41F}\u{1F41F}'
  };

  private stars: Star[] = [];
  private animationFrame = 0;
  private canvasContext: CanvasRenderingContext2D | null = null;
  private planetVoiceIndex = 0;
  private speechTimer = 0;
  private languageSubscription?: Subscription;
  private cinemaTimer = 0;
  private atmosphereUnlocked = false;
  private musicContext?: AudioContext;
  private musicGain?: GainNode;
  private musicOscillators: OscillatorNode[] = [];
  private musicStarted = false;
  private readonly planetVoiceProfiles: Record<string, PlanetVoiceProfile> = {
    Sun: { pitch: 0.92, rate: 0.86, volume: 0.92, color: '#f7bf63', temperament: 'horaverse.temperament.sun' },
    Moon: { pitch: 1.26, rate: 0.78, volume: 0.86, color: '#62e9ff', temperament: 'horaverse.temperament.moon' },
    Mars: { pitch: 0.82, rate: 1.04, volume: 0.95, color: '#ff6a7a', temperament: 'horaverse.temperament.mars' },
    Mercury: { pitch: 1.18, rate: 1.12, volume: 0.86, color: '#67f5bf', temperament: 'horaverse.temperament.mercury' },
    Jupiter: { pitch: 0.76, rate: 0.82, volume: 0.92, color: '#f7bf63', temperament: 'horaverse.temperament.jupiter' },
    Venus: { pitch: 1.34, rate: 0.88, volume: 0.84, color: '#ff9bd2', temperament: 'horaverse.temperament.venus' },
    Saturn: { pitch: 0.62, rate: 0.72, volume: 0.9, color: '#b36bff', temperament: 'horaverse.temperament.saturn' },
    Rahu: { pitch: 1.48, rate: 0.96, volume: 0.82, color: '#c890ff', temperament: 'horaverse.temperament.rahu' },
    Ketu: { pitch: 0.7, rate: 0.76, volume: 0.78, color: '#54f0d7', temperament: 'horaverse.temperament.ketu' }
  };

  constructor(
    private readonly route: ActivatedRoute,
    public readonly geometry: HoraverseGeometryService,
    private readonly story: HoraverseStoryService,
    private readonly translocoService: TranslocoService,
    private readonly termTranslation: ApiTermTranslationService,
    private readonly insight: HoraverseInsightService
  ) {}

  ngOnInit(): void {
    this.houseArcs = this.geometry.buildHouseArcs();
    this.session = this.loadSession();

    if (!this.session) {
      return;
    }
    this.languageSubscription = this.translocoService.langChanges$
      .pipe(
        startWith(this.translocoService.getActiveLang()),
        switchMap(lang => this.translocoService.selectTranslation(lang))
      )
      .subscribe(() => {
        this.rebuildLocalizedState();
        if (!this.voiceEnabled) {
          this.activeVoiceLabel = this.t('horaverse.voice.silent');
        }
      });
  }

  ngAfterViewInit(): void {
    this.startStarfield();
  }

  ngOnDestroy(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }

    this.stopVoiceover();
    this.stopCinemaCycle();
    this.stopDefaultMusic();
    this.languageSubscription?.unsubscribe();
  }

  private rebuildLocalizedState(): void {
    if (!this.session) {
      return;
    }

    const report = this.session.report ?? {};
    const summary = report.summary ?? {};
    this.scenes = this.story.buildScenes(this.session);
    this.activeSceneIndex = Math.max(0, Math.min(this.activeSceneIndex, this.scenes.length - 1));
    this.zodiacArcLabels = this.buildZodiacArcLabels(summary.ascendant);
    this.planetPoints = this.geometry.buildPlanetPoints(report.planets, summary.ascendantLongitude ?? report.ascendantLongitude);
    this.houseInsights = this.insight.readHouses(this.session);
    this.aspectRays = this.insight.buildAspectRays(this.houseInsights, this.planetPoints, this.houseArcs);
    this.dashaInsight = this.insight.readCurrentDasha(this.session);
    this.focusedPlanetName = this.currentScene?.focusPlanet
      || this.planetPoints[0]?.planet
      || '';
    this.focusedHouseNumber = this.currentScene?.focusHouse || this.moonPoint?.house || 1;
    this.restartCinemaCycle();
  }
  private buildZodiacArcLabels(ascendant: string | null | undefined): ZodiacArcLabel[] {
    const zodiacSigns = [
      'Aries', 'Taurus', 'Gemini', 'Cancer', 'Leo', 'Virgo',
      'Libra', 'Scorpio', 'Sagittarius', 'Capricorn', 'Aquarius', 'Pisces'
    ];
    const normalizedAscendant = String(ascendant ?? '').toLowerCase().replace(/[^a-z]/g, '');
    const ascendantIndex = zodiacSigns.findIndex(sign => sign.toLowerCase() === normalizedAscendant);
    const startIndex = ascendantIndex >= 0 ? ascendantIndex : 0;

    return this.houseArcs.map((arc, index) => {
      const sign = zodiacSigns[(startIndex + index) % zodiacSigns.length];
      const start = this.geometry.pointOnCircle(arc.startAngle + 3, 294);
      const end = this.geometry.pointOnCircle(arc.endAngle - 3, 294);

      return {
        house: arc.house,
        sign,
        pathId: `zodiac-house-${arc.house}`,
        path: `M ${start.x} ${start.y} A 294 294 0 0 1 ${end.x} ${end.y}`
      };
    });
  }
  get currentScene(): HoraverseScene | null {
    return this.scenes[this.activeSceneIndex] ?? null;
  }

  get progressLabel(): string {
    return `${this.activeSceneIndex + 1} / ${this.scenes.length}`;
  }

  get moonPoint(): HoraversePlanetPoint | null {
    return this.planetPoints.find(point => point.planet.toLowerCase() === 'moon') ?? null;
  }

  get ascendantWatermarkSymbol(): string {
    const ascendant = this.session?.report?.summary?.ascendant ?? '';
    const key = String(ascendant).toLowerCase().replace(/[^a-z]/g, '');
    return this.ascendantWatermarkSymbols[key] ?? '';
  }

  get saturnPoint(): HoraversePlanetPoint | null {
    return this.planetPoints.find(point => point.planet.toLowerCase() === 'saturn') ?? null;
  }

  get currentDashaLabel(): string {
    const currentDasha = this.dashaInsight ?? this.session?.report?.currentDasha;
    const summary = this.session?.report?.summary;
    const maha = this.translatePlanet(currentDasha?.mahadasha ?? summary?.currentMahadasha) || this.t('horaverse.dasha.current');
    const antar = this.translatePlanet(currentDasha?.antardasha ?? summary?.currentAntardasha) || this.t('horaverse.dasha.inner');

    return `${maha} / ${antar}`;
  }

  get focusedPlanet(): HoraversePlanetPoint | null {
    return this.insight.findPlanetPoint(this.planetPoints, this.focusedPlanetName) ?? this.planetPoints[0] ?? null;
  }

  get focusedHouse(): HoraverseHouseInsight | null {
    return this.insight.findHouse(this.houseInsights, this.focusedHouseNumber);
  }

  get showLivingChart(): boolean {
    const kind = this.currentScene?.kind;
    return kind === 'solar-ring' || kind === 'house-walk' || kind === 'door-ahead';
  }

  get showAspectRays(): boolean {
    const kind = this.currentScene?.kind;
    return kind === 'house-walk' || kind === 'door-ahead';
  }

  get visibleAspectRays(): HoraverseAspectRay[] {
    if (!this.showAspectRays) {
      return [];
    }
    if (this.currentScene?.kind === 'house-walk') {
      return this.aspectRays.filter(ray => ray.toHouse === this.focusedHouseNumber);
    }
    return this.aspectRays;
  }

  get musicButtonLabel(): string {
    return this.musicEnabled ? this.t('horaverse.music.off') : this.t('horaverse.music.on');
  }

  get scoreMetrics(): Array<{ label: string; value: number; color: string }> {
    const scores = this.session?.report?.scores ?? {};

    return [
      { label: this.t('horaverse.metrics.overall'), value: Number(scores.overall ?? 0), color: '#f7bf63' },
      { label: this.t('horaverse.metrics.growth'), value: Number(scores.growth ?? 0), color: '#67f5bf' },
      { label: this.t('horaverse.metrics.challenge'), value: Number(scores.challenge ?? 0), color: '#ff6a7a' }
    ];
  }

  get voiceButtonLabel(): string {
    if (!this.voiceAvailable) {
      return this.t('horaverse.voice.unavailable');
    }

    return this.voiceEnabled ? this.t('horaverse.voice.off') : this.t('horaverse.voice.on');
  }

  get activeVoiceColor(): string {
    const speaksSuffix = ` ${this.t('horaverse.voice.speaks')}`;
    const planet = this.activeVoiceLabel.endsWith(speaksSuffix)
      ? this.activeVoiceLabel.slice(0, -speaksSuffix.length)
      : this.activeVoiceLabel;
    const profile = Object.entries(this.planetVoiceProfiles).find(([name]) => this.translatePlanet(name) === planet || name === planet);
    return profile?.[1].color ?? this.currentScene?.accent ?? '#f7bf63';
  }

  @HostListener('window:keydown', ['$event'])
  handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'ArrowRight' || event.key === ' ' || event.key === 'Enter') {
      event.preventDefault();
      this.nextScene();
    }

    if (event.key === 'ArrowLeft') {
      event.preventDefault();
      this.previousScene();
    }

    if (event.key === 'Escape') {
      this.exitHoraverse();
    }
  }

  @HostListener('window:resize')
  handleResize(): void {
    this.resizeCanvas();
  }

  onStageClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;

    if (target?.closest('button, a')) {
      return;
    }

    this.unlockAtmosphere();
    this.nextScene();
  }

  nextScene(): void {
    if (!this.scenes.length) {
      return;
    }

    const nextIndex = Math.min(this.scenes.length - 1, this.activeSceneIndex + 1);
    this.changeScene(nextIndex, true);
  }

  previousScene(): void {
    const nextIndex = Math.max(0, this.activeSceneIndex - 1);
    this.changeScene(nextIndex, false);
  }

  setScene(index: number): void {
    this.changeScene(Math.max(0, Math.min(this.scenes.length - 1, index)), true);
  }

  exitHoraverse(): void {
    this.stopVoiceover();
    window.close();

    window.setTimeout(() => {
      window.location.href = '/app/kundali';
    }, 180);
  }

  getPlanetColor(point: HoraversePlanetPoint): string {
    return this.planetColors[this.getPlanetKey(point)] ?? this.planetToneColors[point.tone] ?? this.planetToneColors['neutral'];
  }

  getPlanetKey(point: HoraversePlanetPoint): string {
    return point.planet.toLowerCase().replace(/[^a-z]/g, '');
  }

  getPlanetAbbreviation(point: HoraversePlanetPoint): string {
    const labels: Record<string, string> = {
      sun: 'SU',
      moon: 'MO',
      mars: 'MA',
      mercury: 'ME',
      jupiter: 'JU',
      venus: 'VE',
      saturn: 'SA',
      rahu: 'RA',
      ketu: 'KE'
    };

    return labels[this.getPlanetKey(point)] ?? point.planet.slice(0, 2).toUpperCase();
  }

  getMetricWidth(value: number): string {
    const safeValue = Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0));
    return `${safeValue}%`;
  }

  focusPlanet(point: HoraversePlanetPoint, event?: Event): void {
    event?.stopPropagation();
    this.focusedPlanetName = point.planet;
    if (point.house) {
      this.focusedHouseNumber = point.house;
    }
    this.restartCinemaCycle();
  }

  focusHouse(houseNumber: number, event?: Event): void {
    event?.stopPropagation();
    this.focusedHouseNumber = houseNumber;
    const occupant = this.planetPoints.find(point => point.house === houseNumber);
    if (occupant) {
      this.focusedPlanetName = occupant.planet;
    }
    this.restartCinemaCycle();
  }

  toggleMusic(): void {
    this.musicEnabled = !this.musicEnabled;
    if (this.musicEnabled) {
      this.startDefaultMusic();
      return;
    }
    this.stopDefaultMusic();
  }

  supportLabel(tone: string | null | undefined): string {
    return this.t(`horaverse.hud.${tone || 'neutral'}`);
  }

  natureLabel(nature: string | null | undefined): string {
    if (nature === 'benefic') {
      return this.t('horaverse.hud.natureBenefic');
    }
    if (nature === 'shadow') {
      return this.t('horaverse.hud.natureShadow');
    }
    return this.t('horaverse.hud.natureMalefic');
  }

  houseMeaning(house: number | null | undefined): string {
    const number = Number(house);
    if (!Number.isInteger(number) || number < 1 || number > 12) {
      return this.t('horaverse.houseMeaning.unknown');
    }
    return this.t(`horaverse.houseMeaning.${number}`);
  }

  formatJoinedPlanets(values: string[] | undefined): string {
    return (values ?? []).map(value => this.translatePlanet(value)).filter(Boolean).join(', ');
  }

  formatHudDate(value: string | null | undefined): string {
    return value ? this.formatSceneDate(value) : this.t('horaverse.common.unknown');
  }

  toggleVoiceover(): void {
    if (!this.voiceAvailable) {
      return;
    }

    this.voiceEnabled = !this.voiceEnabled;
    this.unlockAtmosphere();

    if (this.voiceEnabled) {
      this.speakCurrentScene(true);
      return;
    }

    this.stopVoiceover();
  }

  private changeScene(index: number, includePlanetInterlude: boolean): void {
    if (index === this.activeSceneIndex) {
      return;
    }

    this.activeSceneIndex = index;
    this.syncFocusToScene();
    this.restartCinemaCycle();

    if (this.voiceEnabled) {
      this.speakCurrentScene(includePlanetInterlude);
    }
  }

  private syncFocusToScene(): void {
    const scene = this.currentScene;
    if (scene?.kind === 'solar-ring') {
      this.focusedPlanetName = scene.focusPlanet || this.planetPoints[0]?.planet || this.focusedPlanetName;
      return;
    }
    if (scene?.kind === 'house-walk') {
      this.focusedHouseNumber = scene.focusHouse || this.moonPoint?.house || this.focusedHouseNumber || 1;
    }
  }

  private restartCinemaCycle(): void {
    this.stopCinemaCycle();
    const kind = this.currentScene?.kind;
    if (kind !== 'solar-ring' && kind !== 'house-walk') {
      return;
    }
    this.cinemaTimer = window.setInterval(() => this.advanceCinemaFocus(), 4200);
  }

  private stopCinemaCycle(): void {
    if (this.cinemaTimer) {
      window.clearInterval(this.cinemaTimer);
      this.cinemaTimer = 0;
    }
  }

  private advanceCinemaFocus(): void {
    if (this.currentScene?.kind === 'solar-ring' && this.planetPoints.length) {
      const index = Math.max(0, this.planetPoints.findIndex(point => point.planet === this.focusedPlanetName));
      const next = this.planetPoints[(index + 1) % this.planetPoints.length];
      this.focusedPlanetName = next.planet;
      if (next.house) {
        this.focusedHouseNumber = next.house;
      }
      return;
    }

    if (this.currentScene?.kind === 'house-walk') {
      this.focusedHouseNumber = this.focusedHouseNumber >= 12 ? 1 : this.focusedHouseNumber + 1;
    }
  }

  private unlockAtmosphere(): void {
    if (!this.atmosphereUnlocked) {
      this.atmosphereUnlocked = true;
      this.musicEnabled = true;
      this.startDefaultMusic();
      return;
    }

    if (this.musicEnabled) {
      this.unlockDefaultMusic();
    }
  }

  private formatSceneDate(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }
    return date.toLocaleDateString(this.voiceLocale() === 'en-IN' ? 'en-GB' : this.voiceLocale(), {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  private speakCurrentScene(includePlanetInterlude: boolean): void {
    const scene = this.currentScene;

    if (!scene || !this.voiceAvailable) {
      return;
    }

    this.cancelSpeech();
    this.activeVoiceLabel = this.t('horaverse.voice.kaalaSpeaks');
    const intro = `${scene.kicker}. ${scene.title}. ${scene.lines.join(' ')}`;
    const kaalaVoice = this.createUtterance(intro, {
      pitch: 0.82,
      rate: 0.78,
      volume: 0.92,
      color: scene.accent,
      temperament: this.t('horaverse.voice.quietWitness')
    });

    kaalaVoice.onend = () => {
      if (!includePlanetInterlude || !this.voiceEnabled) {
        this.isSpeaking = false;
        return;
      }

      this.speechTimer = window.setTimeout(() => this.speakNextPlanetInterlude(), 360);
    };

    this.isSpeaking = true;
    window.speechSynthesis.speak(kaalaVoice);
  }

  private speakNextPlanetInterlude(): void {
    const planets = this.session?.report?.planets ?? [];

    if (!planets.length || !this.voiceEnabled || !this.voiceAvailable) {
      this.isSpeaking = false;
      return;
    }

    const planet = planets[this.planetVoiceIndex % planets.length];
    this.planetVoiceIndex += 1;
    const planetName = String(planet?.planet ?? planet?.planetName ?? this.t('horaverse.common.planet'));
    const profile = this.planetVoiceProfiles[planetName] ?? {
      pitch: 1,
      rate: 0.88,
      volume: 0.82,
      color: '#f7bf63',
      temperament: this.t('horaverse.voice.distantSignal')
    };
    const utterance = this.createUtterance(this.buildPlanetLine(planet, planetName, this.translateTemperament(profile.temperament)), profile);

    this.activeVoiceLabel = `${this.translatePlanet(planetName) || planetName} ${this.t('horaverse.voice.speaks')}`;
    utterance.onend = () => {
      this.isSpeaking = false;
      this.activeVoiceLabel = this.voiceEnabled ? this.t('horaverse.voice.listening') : this.t('horaverse.voice.silent');
    };

    this.isSpeaking = true;
    window.speechSynthesis.speak(utterance);
  }

  private createUtterance(text: string, profile: PlanetVoiceProfile): SpeechSynthesisUtterance {
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.pitch = profile.pitch;
    utterance.rate = profile.rate;
    utterance.volume = profile.volume;
    utterance.lang = this.voiceLocale();

    const voices = window.speechSynthesis.getVoices();
    const preferredVoice = voices.find(voice => voice.lang === utterance.lang)
      ?? voices.find(voice => voice.lang.startsWith(this.translocoService.getActiveLang()))
      ?? voices.find(voice => voice.lang.startsWith('en'))
      ?? null;

    if (preferredVoice) {
      utterance.voice = preferredVoice;
    }

    return utterance;
  }

  private buildPlanetLine(planet: any, planetName: string, temperament: string): string {
    const translatedPlanet = this.translatePlanet(planetName) || planetName;
    const sign = planet?.sign
      ? this.t('horaverse.voice.planetFromSign', { sign: this.translateSign(planet.sign) || planet.sign })
      : this.t('horaverse.voice.planetFromPlace');
    const house = planet?.house
      ? this.t('horaverse.voice.planetInHouse', { house: planet.house })
      : this.t('horaverse.voice.planetInsideChart');
    const strength = this.cleanStrength(planet?.detailedStrength);
    const motion = planet?.isRetrograde ? this.t('horaverse.voice.retrogradeMotion') : this.t('horaverse.voice.directMotion');
    const combustion = planet?.isCombust ? this.t('horaverse.voice.combustion') : '';

    return this.t('horaverse.voice.planetLine', { planet: translatedPlanet, temperament, sign, house, strength, motion, combustion });
  }

  private translateTemperament(value: string): string {
    const translated = this.t(value);
    return translated && translated !== value ? translated : value;
  }
  private cleanStrength(value: any): string {
    const strength = String(value ?? this.t('horaverse.common.quiet'));
    return strength.replace(/^strength\./, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  translateSign(value: unknown): string {
    return this.termTranslation.translateSign(value);
  }

  translatePlanet(value: unknown): string {
    return this.termTranslation.translatePlanet(value);
  }

  kaalaWord(key: string): string {
    return this.t(`horaverse.kaalaWords.${key}`);
  }
  t(key: string, params?: Record<string, unknown>): string {
    const exactValue = this.lookupExactTranslation(key);
    if (exactValue !== undefined) {
      return this.interpolate(exactValue, params);
    }

    if (key.startsWith('horaverse.')) {
      return '';
    }

    return this.translocoService.translate(key, params);
  }

  private lookupExactTranslation(key: string): string | undefined {
    try {
      const lang = this.translocoService.getActiveLang();
      const translations = this.translocoService.getTranslation(lang) as Record<string, unknown>;
      const value = translations?.[key];
      return typeof value === 'string' ? value : undefined;
    } catch {
      return undefined;
    }
  }

  private interpolate(value: string, params?: Record<string, unknown>): string {
    if (!params) {
      return value;
    }

    return value.replace(/{{\s*(\w+)\s*}}/g, (_, paramKey: string) => String(params[paramKey] ?? ''));
  }

  private voiceLocale(): string {
    const lang = this.translocoService.getActiveLang();
    return lang === 'hi' ? 'hi-IN' : lang === 'kn' ? 'kn-IN' : 'en-IN';
  }
  private startDefaultMusic(): void {
    if (this.musicStarted || typeof window === 'undefined') {
      return;
    }

    const AudioContextCtor = window.AudioContext || (window as any).webkitAudioContext;
    if (!AudioContextCtor) {
      return;
    }

    try {
      this.musicContext = new AudioContextCtor();
      this.musicGain = this.musicContext.createGain();
      this.musicGain.gain.setValueAtTime(0.0001, this.musicContext.currentTime);
      this.musicGain.connect(this.musicContext.destination);

      const tones = [110, 164.81, 220, 329.63];
      this.musicOscillators = tones.map((frequency, index) => {
        const oscillator = this.musicContext!.createOscillator();
        const toneGain = this.musicContext!.createGain();
        oscillator.type = index % 2 === 0 ? 'sine' : 'triangle';
        oscillator.frequency.setValueAtTime(frequency, this.musicContext!.currentTime);
        oscillator.detune.setValueAtTime(index * 4 - 6, this.musicContext!.currentTime);
        toneGain.gain.setValueAtTime(index === 0 ? 0.42 : 0.18, this.musicContext!.currentTime);
        oscillator.connect(toneGain);
        toneGain.connect(this.musicGain!);
        oscillator.start();
        return oscillator;
      });

      this.musicStarted = true;
      void this.unlockDefaultMusic();
    } catch {
      this.stopDefaultMusic();
    }
  }

  private unlockDefaultMusic(): void {
    if (!this.musicContext || !this.musicGain) {
      return;
    }

    const fadeIn = () => {
      if (!this.musicContext || !this.musicGain) {
        return;
      }

      const now = this.musicContext.currentTime;
      this.musicGain.gain.cancelScheduledValues(now);
      this.musicGain.gain.setTargetAtTime(0.028, now, 1.8);
    };

    if (this.musicContext.state === 'suspended') {
      this.musicContext.resume().then(fadeIn).catch(() => undefined);
      return;
    }

    fadeIn();
  }

  private stopDefaultMusic(): void {
    this.musicOscillators.forEach(oscillator => {
      try {
        oscillator.stop();
      } catch {
        // Already stopped.
      }
    });
    this.musicOscillators = [];
    this.musicGain?.disconnect();
    void this.musicContext?.close();
    this.musicGain = undefined;
    this.musicContext = undefined;
    this.musicStarted = false;
  }
  private stopVoiceover(): void {
    this.voiceEnabled = false;
    this.activeVoiceLabel = this.t('horaverse.voice.silent');
    this.isSpeaking = false;
    this.cancelSpeech();
  }

  private cancelSpeech(): void {
    if (this.speechTimer) {
      window.clearTimeout(this.speechTimer);
      this.speechTimer = 0;
    }

    if (this.voiceAvailable) {
      window.speechSynthesis.cancel();
    }
  }

  private loadSession(): HoraverseSession | null {
    try {
      const querySessionId = this.route.snapshot.queryParamMap.get('session');
      const latestSessionId = window.localStorage.getItem('horaverse.latestSessionId');
      const sessionId = querySessionId || latestSessionId;

      if (!sessionId) {
        this.loadError = 'horaverse.empty.noSession';
        return null;
      }

      const raw = window.localStorage.getItem(`horaverse.session.${sessionId}`);

      if (!raw) {
        this.loadError = 'horaverse.empty.noData';
        return null;
      }

      const parsed = JSON.parse(raw) as HoraverseSession;

      if (parsed?.version !== 1 || !parsed.report) {
        this.loadError = 'horaverse.empty.invalidData';
        return null;
      }

      return parsed;
    } catch {
      this.loadError = 'horaverse.empty.readError';
      return null;
    }
  }

  private startStarfield(): void {
    const canvas = this.starfield?.nativeElement;

    if (!canvas) {
      return;
    }

    this.canvasContext = canvas.getContext('2d');
    this.resizeCanvas();
    this.stars = this.createStars(140);
    this.drawStarfield();
  }

  private resizeCanvas(): void {
    const canvas = this.starfield?.nativeElement;

    if (!canvas) {
      return;
    }

    const ratio = window.devicePixelRatio || 1;
    canvas.width = Math.floor(window.innerWidth * ratio);
    canvas.height = Math.floor(window.innerHeight * ratio);
    canvas.style.width = `${window.innerWidth}px`;
    canvas.style.height = `${window.innerHeight}px`;
    this.canvasContext = canvas.getContext('2d');
    this.canvasContext?.setTransform(ratio, 0, 0, ratio, 0, 0);
  }

  private createStars(count: number): Star[] {
    return Array.from({ length: count }, () => ({
      x: Math.random() * window.innerWidth,
      y: Math.random() * window.innerHeight,
      radius: Math.random() * 1.4 + 0.2,
      speed: Math.random() * 0.18 + 0.04,
      alpha: Math.random() * 0.65 + 0.2
    }));
  }

  private drawStarfield(): void {
    const context = this.canvasContext;

    if (!context) {
      return;
    }

    context.clearRect(0, 0, window.innerWidth, window.innerHeight);
    context.fillStyle = '#03040b';
    context.fillRect(0, 0, window.innerWidth, window.innerHeight);

    this.stars.forEach(star => {
      star.y += star.speed;

      if (star.y > window.innerHeight + 4) {
        star.y = -4;
        star.x = Math.random() * window.innerWidth;
      }

      context.beginPath();
      context.arc(star.x, star.y, star.radius, 0, Math.PI * 2);
      context.fillStyle = `rgba(220, 247, 255, ${star.alpha})`;
      context.fill();
    });

    this.animationFrame = requestAnimationFrame(() => this.drawStarfield());
  }
}