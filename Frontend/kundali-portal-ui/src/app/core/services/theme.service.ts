import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light' | 'hora';

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
    const nextTheme: Record<ThemeMode, ThemeMode> = {
      dark: 'light',
      light: 'hora',
      hora: 'dark',
    };

    this.setTheme(nextTheme[this.mode()]);
  }

  setTheme(mode: ThemeMode, persist = true): void {
    this.mode.set(mode);

    if (typeof document !== 'undefined') {
      document.body.dataset['theme'] = mode;
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
    return value === 'dark' || value === 'light' || value === 'hora';
  }
}
