import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnDestroy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AstrologyIconComponent } from '../../shared/components/astrology-icon.component';
import { PortalWheelPickerComponent } from '../../shared/components/portal-wheel-picker.component';
import { NumerologyResponse, NumerologyService } from './numerology.service';

@Component({
  selector: 'app-numerology',
  standalone: true,
  imports: [CommonModule, FormsModule, AstrologyIconComponent, PortalWheelPickerComponent],
  templateUrl: './numerology.component.html',
  styleUrl: './numerology.component.scss',
})
export class NumerologyComponent implements OnDestroy {
  private readonly numerologyService = inject(NumerologyService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly colourMap: Record<string, string> = {
    blue: '#4776b4', brown: '#8b6248', cream: '#f1e5c8', gold: '#c79a35',
    'golden yellow': '#e0ad32', green: '#4f8b62', grey: '#8a9099',
    'light blue': '#91b8d6', 'light green': '#92bd92', orange: '#df8439',
    pink: '#d883a2', red: '#c95b5b', violet: '#8065a8', white: '#f7f5ef',
    yellow: '#dfbc45',
  };

  dateOfBirth = '';
  readonly currentYear = new Date().getFullYear();
  readonly maxDate = new Date().toISOString().slice(0, 10);
  result: NumerologyResponse | null = null;
  revealResult: NumerologyResponse | null = null;
  revealStage: 'idle' | 'dob' | 'digits' | 'shuffle' | 'resolved' = 'idle';
  errorMessage = '';
  isLoading = false;
  private revealTimers: number[] = [];

  get formattedDateOfBirth(): string {
    const [year, month, day] = this.dateOfBirth.split('-');
    return day && month && year ? `${day} · ${month} · ${year}` : this.dateOfBirth;
  }

  get birthDigits(): string[] {
    const [year, month, day] = this.dateOfBirth.split('-');
    return `${day ?? ''}${month ?? ''}${year ?? ''}`.replace(/\D/g, '').split('');
  }

  calculate(): void {
    if (!this.dateOfBirth || this.dateOfBirth > this.maxDate || this.isLoading) {
      return;
    }

    this.clearRevealTimers();
    this.errorMessage = '';
    this.result = null;
    this.revealResult = null;
    this.isLoading = true;
    this.revealStage = 'dob';
    const revealStartedAt = Date.now();

    this.scheduleReveal(() => this.revealStage = 'digits', 220);
    this.scheduleReveal(() => this.revealStage = 'shuffle', 520);

    this.numerologyService.calculate(this.dateOfBirth).subscribe({
      next: result => {
        const resolveDelay = Math.max(0, 800 - (Date.now() - revealStartedAt));
        this.scheduleReveal(() => {
          this.revealResult = result;
          this.revealStage = 'resolved';
          this.scheduleReveal(() => {
            this.result = result;
            this.isLoading = false;
            this.revealStage = 'idle';
          }, 340);
        }, resolveDelay);
      },
      error: error => {
        this.clearRevealTimers();
        this.isLoading = false;
        this.revealStage = 'idle';
        this.errorMessage = error?.error?.message
          || (typeof error?.error === 'string' ? error.error : '')
          || 'Unable to calculate numerology right now. Please try again.';
        this.changeDetector.detectChanges();
      },
    });
  }

  ngOnDestroy(): void {
    this.clearRevealTimers();
  }

  colourSwatch(colour: string): string | null {
    return this.colourMap[colour.trim().toLowerCase()] ?? null;
  }

  private scheduleReveal(action: () => void, delay: number): void {
    const timer = window.setTimeout(() => {
      action();
      this.changeDetector.detectChanges();
    }, delay);
    this.revealTimers.push(timer);
  }

  private clearRevealTimers(): void {
    this.revealTimers.forEach(timer => window.clearTimeout(timer));
    this.revealTimers = [];
  }
}