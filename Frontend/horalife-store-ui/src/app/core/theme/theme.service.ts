import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

const THEME_STORAGE_KEY = 'kundali-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly mode = signal<ThemeMode>('dark');

  constructor() {
    this.apply(readTheme(true));
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        this.followHoraLife();
      }
    });
    window.addEventListener('focus', () => this.followHoraLife());
  }

  private followHoraLife(): void {
    const next = readTheme(false);
    if (next !== this.mode()) {
      this.apply(next);
    }
  }

  private apply(mode: ThemeMode): void {
    this.mode.set(mode);
    document.documentElement.dataset['theme'] = mode;
    document.body.dataset['theme'] = mode;
    try {
      localStorage.setItem(THEME_STORAGE_KEY, mode);
    } catch {
      // The theme still applies for this visit.
    }
  }
}

function readTheme(preferQuery: boolean): ThemeMode {
  if (preferQuery) {
    const fromQuery = new URLSearchParams(window.location.search).get('theme');
    if (isThemeMode(fromQuery)) {
      return fromQuery;
    }
  }

  const fromCookie = readCookie(THEME_STORAGE_KEY);
  if (isThemeMode(fromCookie)) {
    return fromCookie;
  }

  try {
    const stored = localStorage.getItem(THEME_STORAGE_KEY);
    if (isThemeMode(stored)) {
      return stored;
    }
  } catch {
    // Fall through to the HoraLife default.
  }

  return 'dark';
}

function readCookie(name: string): string | null {
  const prefix = `${name}=`;
  const match = document.cookie.split('; ').find((part) => part.startsWith(prefix));
  return match ? decodeURIComponent(match.slice(prefix.length)) : null;
}

function isThemeMode(value: string | null): value is ThemeMode {
  return value === 'dark' || value === 'light';
}
