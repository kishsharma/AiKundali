import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

const THEME_STORAGE_KEY = 'kundali-theme';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  readonly mode = signal<ThemeMode>('dark');

  constructor() {
    this.setTheme(this.readStoredTheme(), false);
  }

  toggleTheme(): void {
    this.setTheme(this.mode() === 'dark' ? 'light' : 'dark');
  }

  setTheme(mode: ThemeMode, persist = true): void {
    this.mode.set(mode);

    if (typeof document !== 'undefined') {
      document.body.dataset['theme'] = mode;
      writeSharedThemeCookie(mode);
    }

    if (persist) {
      try {
        localStorage.setItem(THEME_STORAGE_KEY, mode);
      } catch {
        // Theme still applies for the current session.
      }
    }
  }

  private readStoredTheme(): ThemeMode {
    try {
      const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);
      return this.isThemeMode(storedTheme) ? storedTheme : 'dark';
    } catch {
      return 'dark';
    }
  }

  private isThemeMode(value: string | null): value is ThemeMode {
    return value === 'dark' || value === 'light';
  }
}

function writeSharedThemeCookie(mode: ThemeMode): void {
  const secure = window.location.protocol === 'https:' ? '; Secure' : '';
  const domain = sharedThemeCookieDomain();
  const domainPart = domain ? `; Domain=${domain}` : '';
  document.cookie = `${THEME_STORAGE_KEY}=${mode}; Path=/; Max-Age=31536000; SameSite=Lax${domainPart}${secure}`;
}

function sharedThemeCookieDomain(): string | null {
  const host = window.location.hostname.toLowerCase();
  if (host === 'horalife.in' || host.endsWith('.horalife.in')) {
    return '.horalife.in';
  }

  return null;
}
