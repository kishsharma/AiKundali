import { Injectable } from '@angular/core';
import { HoraverseScene, HoraverseSession } from './horaverse-models';

@Injectable({ providedIn: 'root' })
export class HoraverseStoryService {
  buildScenes(session: HoraverseSession): HoraverseScene[] {
    const report = session.report ?? {};
    const summary = report.summary ?? {};
    const planets = report.planets ?? [];
    const houses = report.houses ?? [];
    const scores = report.scores ?? {};
    const currentDasha = report.currentDasha ?? {};
    const birth = session.birthProfile;
    const moon = this.findPlanet(planets, 'Moon');
    const saturn = this.findPlanet(planets, 'Saturn');
    const focusHouse = this.findHouse(houses, moon?.house);
    const saturnLine = saturn
      ? `Saturn sits in house ${saturn.house ?? 'unknown'}, carrying ${this.cleanStrength(saturn.detailedStrength)} pressure.`
      : 'Saturn is quiet in this report, so Kaala keeps the shadow line soft.';

    return [
      {
        kind: 'awakening',
        kicker: 'The awakening',
        title: `You were born at ${birth.birthTime || 'a precise moment'}.`,
        accent: '#f7bf63',
        lines: [
          `${birth.name || 'This chart'} entered through ${birth.birthPlace || 'one place on earth'}.`,
          `Kaala does not predict your life. It listens to the pattern that began moving then.`,
          `The first signal is small, but it is not empty.`
        ]
      },
      {
        kind: 'solar-ring',
        kicker: 'The birth of your Hora',
        title: `${summary.ascendant || 'The ascendant'} rises. The ring forms.`,
        accent: '#54f0d7',
        lines: [
          `Each house opens in a 30 degree chamber, and every planet keeps its exact longitude.`,
          `Your Moon is in ${moon?.sign ?? summary.moonSign ?? 'its sign'}, house ${moon?.house ?? 'unknown'}.`,
          `Your chart shows ${scores.overall ?? 'a'} overall pattern with growth at ${scores.growth ?? 'unknown'} and challenge at ${scores.challenge ?? 'unknown'}.`
        ]
      },
      {
        kind: 'kaala',
        kicker: 'Meet Kaala',
        title: 'I am Kaala.',
        accent: '#c890ff',
        lines: [
          'I do not read your mind. I read the pressure marks left by timing, repetition, and choice.',
          `Your current Mahadasha is ${currentDasha.mahadasha ?? summary.currentMahadasha ?? 'still unfolding'}.`,
          `Your Antardasha is ${currentDasha.antardasha ?? summary.currentAntardasha ?? 'the smaller current inside it'}.`
        ]
      },
      {
        kind: 'inner-focus',
        kicker: 'The inner room',
        title: `Moon in ${moon?.sign ?? summary.moonSign ?? 'focus'}`,
        accent: '#62e9ff',
        focusPlanet: 'Moon',
        lines: [
          `This pattern often appears when the mind keeps arranging conversations after they are over.`,
          `House ${moon?.house ?? 'unknown'} is active here${focusHouse?.houseName ? ` - ${focusHouse.houseName}` : ''}.`,
          `${moon?.nakshatra ? `The Moon sits in ${moon.nakshatra}, pada ${moon.pada ?? '?'}.` : 'The Moon becomes the first doorway into the story.'}`,
          saturnLine
        ]
      },
      {
        kind: 'door-ahead',
        kicker: 'The door ahead',
        title: 'What is unfolding now',
        accent: '#67f5bf',
        lines: this.buildDoorAheadLines(session, scores, currentDasha)
      }
    ];
  }

  private buildDoorAheadLines(session: HoraverseSession, scores: any, currentDasha: any): string[] {
    const strengths = this.toList(scores.dominantStrengths);
    const weaknesses = this.toList(scores.dominantWeaknesses);
    const factors = this.toList(scores.factors ?? scores.growthFactors ?? scores.challengeFactors);
    const dashaWindow = this.formatWindow(currentDasha?.startDate, currentDasha?.endDate);

    return [
      strengths[0]
        ? `Your strongest support says: ${strengths[0]}`
        : 'A supportive window is present, but it asks for quieter attention than force.',
      weaknesses[0]
        ? `The friction point says: ${weaknesses[0]}`
        : 'The difficult part is not a wall. It is a place where energy needs better timing.',
      factors[0]
        ? `Kaala notices this repeating clue: ${factors[0]}`
        : `This ${currentDasha?.mahadasha ?? session.report?.summary?.currentMahadasha ?? 'current'} period asks you to spend energy more selectively.`,
      dashaWindow ? `This cycle is active from ${dashaWindow}.` : 'The next door opens through attention, not hurry.'
    ];
  }

  private findPlanet(planets: any[], planetName: string): any | null {
    return planets.find(item => String(item?.planet ?? item?.planetName ?? '').toLowerCase() === planetName.toLowerCase()) ?? null;
  }

  private findHouse(houses: any[], houseNumber: number | string | null | undefined): any | null {
    const numericHouse = Number(houseNumber);
    return houses.find(item => Number(item?.houseNumber) === numericHouse) ?? null;
  }

  private toList(value: any): string[] {
    return Array.isArray(value)
      ? value.map(item => String(item).trim()).filter(Boolean)
      : [];
  }

  private cleanStrength(value: any): string {
    const strength = String(value ?? 'quiet');
    return strength.replace(/^strength\./, '').replace(/([a-z])([A-Z])/g, '$1 $2');
  }

  private formatWindow(start: string | Date | null | undefined, end: string | Date | null | undefined): string {
    if (!start || !end) {
      return '';
    }

    return `${this.formatDate(start)} to ${this.formatDate(end)}`;
  }

  private formatDate(value: string | Date): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return String(value);
    }

    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }
}
