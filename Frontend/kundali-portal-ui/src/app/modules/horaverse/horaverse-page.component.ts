import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { HoraverseGeometryService } from './horaverse-geometry.service';
import { HoraverseStoryService } from './horaverse-story.service';
import { HoraverseHouseArc, HoraversePlanetPoint, HoraverseScene, HoraverseSession } from './horaverse-models';

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
  planetPoints: HoraversePlanetPoint[] = [];
  voiceEnabled = false;
  voiceAvailable = typeof window !== 'undefined' && 'speechSynthesis' in window && 'SpeechSynthesisUtterance' in window;
  isSpeaking = false;
  activeVoiceLabel = 'Silent';

  readonly viewBox = '0 0 600 600';
  readonly center = 300;
  readonly planetToneColors: Record<string, string> = {
    moon: '#62e9ff',
    saturn: '#b36bff',
    challenging: '#ff6a7a',
    supportive: '#67f5bf',
    neutral: '#f7bf63'
  };

  private stars: Star[] = [];
  private animationFrame = 0;
  private canvasContext: CanvasRenderingContext2D | null = null;
  private planetVoiceIndex = 0;
  private speechTimer = 0;
  private readonly planetVoiceProfiles: Record<string, PlanetVoiceProfile> = {
    Sun: { pitch: 0.92, rate: 0.86, volume: 0.92, color: '#f7bf63', temperament: 'steady fire' },
    Moon: { pitch: 1.26, rate: 0.78, volume: 0.86, color: '#62e9ff', temperament: 'reflective water' },
    Mars: { pitch: 0.82, rate: 1.04, volume: 0.95, color: '#ff6a7a', temperament: 'direct heat' },
    Mercury: { pitch: 1.18, rate: 1.12, volume: 0.86, color: '#67f5bf', temperament: 'quick signal' },
    Jupiter: { pitch: 0.76, rate: 0.82, volume: 0.92, color: '#f7bf63', temperament: 'wide counsel' },
    Venus: { pitch: 1.34, rate: 0.88, volume: 0.84, color: '#ff9bd2', temperament: 'soft magnetism' },
    Saturn: { pitch: 0.62, rate: 0.72, volume: 0.9, color: '#b36bff', temperament: 'slow gravity' },
    Rahu: { pitch: 1.48, rate: 0.96, volume: 0.82, color: '#c890ff', temperament: 'strange hunger' },
    Ketu: { pitch: 0.7, rate: 0.76, volume: 0.78, color: '#54f0d7', temperament: 'distant release' }
  };

  constructor(
    private readonly route: ActivatedRoute,
    public readonly geometry: HoraverseGeometryService,
    private readonly story: HoraverseStoryService
  ) {}

  ngOnInit(): void {
    this.houseArcs = this.geometry.buildHouseArcs();
    this.session = this.loadSession();

    if (!this.session) {
      return;
    }

    this.scenes = this.story.buildScenes(this.session);
    this.planetPoints = this.geometry.buildPlanetPoints(this.session.report?.planets);
  }

  ngAfterViewInit(): void {
    this.startStarfield();
  }

  ngOnDestroy(): void {
    if (this.animationFrame) {
      cancelAnimationFrame(this.animationFrame);
    }

    this.stopVoiceover();
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

  get saturnPoint(): HoraversePlanetPoint | null {
    return this.planetPoints.find(point => point.planet.toLowerCase() === 'saturn') ?? null;
  }

  get currentDashaLabel(): string {
    const currentDasha = this.session?.report?.currentDasha;
    const summary = this.session?.report?.summary;
    const maha = currentDasha?.mahadasha ?? summary?.currentMahadasha ?? 'Current';
    const antar = currentDasha?.antardasha ?? summary?.currentAntardasha ?? 'Inner';

    return `${maha} / ${antar}`;
  }

  get scoreMetrics(): Array<{ label: string; value: number; color: string }> {
    const scores = this.session?.report?.scores ?? {};

    return [
      { label: 'Overall', value: Number(scores.overall ?? 0), color: '#f7bf63' },
      { label: 'Growth', value: Number(scores.growth ?? 0), color: '#67f5bf' },
      { label: 'Challenge', value: Number(scores.challenge ?? 0), color: '#ff6a7a' }
    ];
  }

  get voiceButtonLabel(): string {
    if (!this.voiceAvailable) {
      return 'Voice unavailable';
    }

    return this.voiceEnabled ? 'Voice Off' : 'Voice On';
  }

  get activeVoiceColor(): string {
    const planet = this.activeVoiceLabel.replace(' speaks', '');
    return this.planetVoiceProfiles[planet]?.color ?? this.currentScene?.accent ?? '#f7bf63';
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
    return this.planetToneColors[point.tone] ?? this.planetToneColors['neutral'];
  }

  getPlanetAbbreviation(point: HoraversePlanetPoint): string {
    return point.planet.slice(0, 2).toUpperCase();
  }

  getMetricWidth(value: number): string {
    const safeValue = Math.max(0, Math.min(100, Number.isFinite(value) ? value : 0));
    return `${safeValue}%`;
  }

  toggleVoiceover(): void {
    if (!this.voiceAvailable) {
      return;
    }

    this.voiceEnabled = !this.voiceEnabled;

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

    if (this.voiceEnabled) {
      this.speakCurrentScene(includePlanetInterlude);
    }
  }

  private speakCurrentScene(includePlanetInterlude: boolean): void {
    const scene = this.currentScene;

    if (!scene || !this.voiceAvailable) {
      return;
    }

    this.cancelSpeech();
    this.activeVoiceLabel = 'Kaala speaks';
    const intro = `${scene.kicker}. ${scene.title}. ${scene.lines.join(' ')}`;
    const kaalaVoice = this.createUtterance(intro, {
      pitch: 0.82,
      rate: 0.78,
      volume: 0.92,
      color: scene.accent,
      temperament: 'quiet witness'
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
    const planetName = String(planet?.planet ?? planet?.planetName ?? 'Planet');
    const profile = this.planetVoiceProfiles[planetName] ?? {
      pitch: 1,
      rate: 0.88,
      volume: 0.82,
      color: '#f7bf63',
      temperament: 'distant signal'
    };
    const utterance = this.createUtterance(this.buildPlanetLine(planet, planetName, profile.temperament), profile);

    this.activeVoiceLabel = `${planetName} speaks`;
    utterance.onend = () => {
      this.isSpeaking = false;
      this.activeVoiceLabel = this.voiceEnabled ? 'Listening' : 'Silent';
    };

    this.isSpeaking = true;
    window.speechSynthesis.speak(utterance);
  }

  private createUtterance(text: string, profile: PlanetVoiceProfile): SpeechSynthesisUtterance {
    const utterance = new SpeechSynthesisUtterance(text);
    utterance.pitch = profile.pitch;
    utterance.rate = profile.rate;
    utterance.volume = profile.volume;
    utterance.lang = 'en-IN';

    const voices = window.speechSynthesis.getVoices();
    const preferredVoice = voices.find(voice => voice.lang === 'en-IN')
      ?? voices.find(voice => voice.lang.startsWith('en'))
      ?? null;

    if (preferredVoice) {
      utterance.voice = preferredVoice;
    }

    return utterance;
  }

  private buildPlanetLine(planet: any, planetName: string, temperament: string): string {
    const sign = planet?.sign ? `from ${planet.sign}` : 'from its place';
    const house = planet?.house ? `in house ${planet.house}` : 'inside the chart';
    const strength = this.cleanStrength(planet?.detailedStrength);
    const motion = planet?.isRetrograde ? 'I move inward before I move outward.' : 'I move through visible choices.';
    const combustion = planet?.isCombust ? 'My signal is close to the Sun, so I speak through heat and pressure.' : '';

    return `${planetName} speaks with ${temperament}. I stand ${sign}, ${house}. My strength is ${strength}. ${motion} ${combustion}`;
  }

  private cleanStrength(value: any): string {
    const strength = String(value ?? 'quiet');
    return strength.replace(/^strength\./, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private stopVoiceover(): void {
    this.voiceEnabled = false;
    this.activeVoiceLabel = 'Silent';
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
        this.loadError = 'HoraVerse could not find a saved report session.';
        return null;
      }

      const raw = window.localStorage.getItem(`horaverse.session.${sessionId}`);

      if (!raw) {
        this.loadError = 'HoraVerse could not find the saved report data for this session.';
        return null;
      }

      const parsed = JSON.parse(raw) as HoraverseSession;

      if (parsed?.version !== 1 || !parsed.report) {
        this.loadError = 'HoraVerse found a session, but its report data is not valid.';
        return null;
      }

      return parsed;
    } catch {
      this.loadError = 'HoraVerse could not read the saved report data in this browser.';
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