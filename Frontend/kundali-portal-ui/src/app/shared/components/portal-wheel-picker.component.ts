import { CommonModule } from '@angular/common';
import { Component, HostListener, Input, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { IonDatetime } from '@ionic/angular/standalone';

@Component({
  selector: 'app-portal-wheel-picker',
  standalone: true,
  imports: [CommonModule, IonDatetime],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PortalWheelPickerComponent),
      multi: true,
    },
  ],
  template: `
    <div class="wheel-picker" [class.disabled]="disabled">
      <button
        type="button"
        class="wheel-trigger"
        [class.empty]="!value"
        [disabled]="disabled"
        (click)="openPicker()"
      >
        <span class="wheel-trigger-value">{{ displayValue || placeholder }}</span>
        <span class="wheel-trigger-icon" aria-hidden="true">{{ triggerBadge }}</span>
      </button>

      <div class="wheel-overlay" *ngIf="isOpen" (click)="closePicker()">
        <section class="wheel-dialog" role="dialog" [attr.aria-label]="label" (click)="$event.stopPropagation()">
          <header class="wheel-header">
            <div>
              <span class="wheel-kicker">{{ presentationLabel }}</span>
              <strong>{{ label }}</strong>
            </div>
            <button type="button" class="wheel-close" (click)="closePicker()" aria-label="Close picker">x</button>
          </header>

          <ion-datetime
            class="picker-datetime"
            [value]="draftValue"
            [presentation]="presentation"
            [preferWheel]="true"
            [showDefaultButtons]="true"
            [disabled]="disabled"
            [min]="minValue"
            [max]="maxValue"
            [locale]="'en-IN'"
            cancelText="Cancel"
            doneText="Set"
            [showDefaultTitle]="false"
            [showDefaultTimeLabel]="mode === 'date-time'"
            (ionChange)="onPickerChanged($event)"
            (ionCancel)="onPickerCancelled()"
          ></ion-datetime>
        </section>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      width: 100%;
    }

    .wheel-picker {
      width: 100%;
    }

    .wheel-trigger {
      display: flex;
      width: 100%;
      min-height: 44px;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      border: 1px solid var(--theme-line, #d7d1c7);
      border-radius: var(--theme-radius-control, 12px);
      padding: 10px 12px;
      background: var(--theme-input-bg, #fff);
      color: var(--theme-text, #211a14);
      font: inherit;
      text-align: left;
      cursor: pointer;
    }

    .wheel-trigger.empty {
      color: var(--theme-text-muted, #81776d);
    }

    .wheel-trigger:disabled {
      cursor: not-allowed;
      opacity: 0.68;
    }

    .wheel-trigger-icon {
      flex: 0 0 auto;
      min-width: 40px;
      color: var(--theme-accent, #7c3aed);
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0;
      text-align: right;
      text-transform: uppercase;
    }

    .wheel-overlay {
      position: fixed;
      inset: 0;
      z-index: 1600;
      display: grid;
      place-items: center;
      padding: 18px;
      background: rgba(15, 12, 25, 0.42);
      backdrop-filter: blur(8px);
    }

    .wheel-dialog {
      width: min(520px, calc(100vw - 24px));
      overflow: hidden;
      border: 1px solid color-mix(in srgb, var(--theme-accent, #7c3aed) 16%, var(--theme-line, #d7d1c7));
      border-radius: 20px;
      background: color-mix(in srgb, var(--theme-surface, #fffdf8) 92%, white);
      color: var(--theme-text, #211a14);
      box-shadow: 0 28px 80px rgba(12, 10, 22, 0.22);
    }

    .wheel-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 16px;
      padding: 16px 16px 10px;
      border-bottom: 1px solid color-mix(in srgb, var(--theme-line, #d7d1c7) 80%, transparent);
    }

    .wheel-header strong {
      display: block;
      font-size: 18px;
      font-weight: 700;
    }

    .wheel-kicker {
      display: inline-block;
      margin-bottom: 4px;
      color: var(--theme-accent, #7c3aed);
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
    }

    .wheel-close {
      width: 34px;
      height: 34px;
      border: 1px solid var(--theme-line, #d7d1c7);
      border-radius: 999px;
      background: var(--theme-input-bg, #fff);
      color: var(--theme-text, #211a14);
      font: inherit;
      font-size: 16px;
      line-height: 1;
      cursor: pointer;
    }

    .picker-datetime {
      width: 100%;
      padding: 6px 8px 10px;
      color: var(--theme-text, #211a14);
      --background: var(--theme-surface, #ffffff);
      --background-rgb: 255, 255, 255;
      --title-color: var(--theme-text, #211a14);
      --wheel-highlight-background: color-mix(in srgb, var(--theme-accent, #7c3aed) 14%, var(--theme-input-bg, #ffffff));
      --wheel-highlight-border-radius: 16px;
      --wheel-fade-background-rgb: 255, 255, 255;
    }

    :host-context(body[data-theme='dark']) .picker-datetime {
      --background: var(--theme-surface, #24366f);
      --background-rgb: 36, 54, 111;
      --title-color: var(--theme-text, #efe6d3);
      --wheel-highlight-background: color-mix(in srgb, var(--theme-accent, #c9a24b) 18%, var(--theme-surface-2, #2b417f));
      --wheel-fade-background-rgb: 36, 54, 111;
    }

    @media (max-width: 640px) {
      .wheel-overlay {
        align-items: end;
        padding: 8px;
      }

      .wheel-dialog {
        width: min(100vw - 8px, 100%);
        border-radius: 18px 18px 0 0;
      }
    }
  `],
})
export class PortalWheelPickerComponent implements ControlValueAccessor {
  @Input() mode: 'date' | 'time' | 'date-time' = 'date';
  @Input() label = 'Select value';
  @Input() placeholder = 'Select';
  @Input() minYear = 1900;
  @Input() maxYear = new Date().getFullYear() + 2;

  value = '';
  disabled = false;
  isOpen = false;
  draftValue: string | null = null;

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  get presentation(): 'date' | 'time' | 'date-time' {
    return this.mode;
  }

  get presentationLabel(): string {
    if (this.mode === 'time') {
      return 'Time';
    }

    if (this.mode === 'date-time') {
      return 'Date & Time';
    }

    return 'Date';
  }

  get triggerBadge(): string {
    if (this.mode === 'time') {
      return 'Time';
    }

    if (this.mode === 'date-time') {
      return 'Date/Time';
    }

    return 'Cal';
  }

  get minValue(): string | undefined {
    return this.mode === 'time' ? undefined : `${this.minYear}-01-01T00:00:00`;
  }

  get maxValue(): string | undefined {
    return this.mode === 'time' ? undefined : `${this.maxYear}-12-31T23:59:00`;
  }

  get displayValue(): string {
    if (!this.value) {
      return '';
    }

    if (this.mode === 'time') {
      const time = this.parseTimeValue(this.value);
      if (!time) {
        return this.value;
      }

      return new Intl.DateTimeFormat('en-IN', { hour: 'numeric', minute: '2-digit' }).format(time);
    }

    if (this.mode === 'date-time') {
      const value = this.parseDateTimeValue(this.value);
      if (!value) {
        return this.value;
      }

      return new Intl.DateTimeFormat('en-IN', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: 'numeric',
        minute: '2-digit',
      }).format(value);
    }

    const date = this.parseDateValue(this.value);
    if (!date) {
      return this.value;
    }

    return new Intl.DateTimeFormat('en-IN', { day: 'numeric', month: 'short', year: 'numeric' }).format(date);
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  openPicker(): void {
    if (this.disabled) {
      return;
    }

    this.draftValue = this.toPickerValue(this.value);
    this.isOpen = true;
  }

  closePicker(): void {
    this.isOpen = false;
    this.draftValue = null;
    this.onTouched();
  }

  onPickerChanged(event: CustomEvent<{ value?: string | string[] | null }>): void {
    const rawValue = Array.isArray(event.detail?.value)
      ? event.detail?.value[0] ?? null
      : event.detail?.value ?? null;

    if (!rawValue) {
      this.closePicker();
      return;
    }

    this.value = this.fromPickerValue(String(rawValue));
    this.onChange(this.value);
    this.closePicker();
  }

  onPickerCancelled(): void {
    this.closePicker();
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen) {
      this.closePicker();
    }
  }

  private toPickerValue(value: string): string {
    const now = new Date();

    if (this.mode === 'time') {
      const time = this.parseTimeValue(value) ?? new Date(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes(), 0, 0);
      return this.toIsoDateTime(time);
    }

    if (this.mode === 'date-time') {
      const dateTime = this.parseDateTimeValue(value) ?? now;
      return this.toIsoDateTime(dateTime);
    }

    const date = this.parseDateValue(value) ?? now;
    return `${date.getFullYear()}-${this.pad(date.getMonth() + 1)}-${this.pad(date.getDate())}T12:00:00`;
  }

  private fromPickerValue(value: string): string {
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})(?:T(\d{2}):(\d{2}))?/);
    if (!match) {
      return this.value;
    }

    const [, year, month, day, hour = '00', minute = '00'] = match;

    if (this.mode === 'time') {
      return `${hour}:${minute}`;
    }

    if (this.mode === 'date-time') {
      return `${year}-${month}-${day}T${hour}:${minute}`;
    }

    return `${year}-${month}-${day}`;
  }

  private parseDateValue(value: string): Date | null {
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
    if (!match) {
      return null;
    }

    const parsed = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]));
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private parseTimeValue(value: string): Date | null {
    const match = value.match(/^(\d{2}):(\d{2})$/);
    if (!match) {
      return null;
    }

    const parsed = new Date();
    parsed.setSeconds(0, 0);
    parsed.setHours(Number(match[1]), Number(match[2]), 0, 0);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private parseDateTimeValue(value: string): Date | null {
    const match = value.match(/^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/);
    if (!match) {
      return null;
    }

    const parsed = new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]), Number(match[4]), Number(match[5]), 0, 0);
    return Number.isNaN(parsed.getTime()) ? null : parsed;
  }

  private toIsoDateTime(value: Date): string {
    return `${value.getFullYear()}-${this.pad(value.getMonth() + 1)}-${this.pad(value.getDate())}T${this.pad(value.getHours())}:${this.pad(value.getMinutes())}:00`;
  }

  private pad(value: number): string {
    return String(value).padStart(2, '0');
  }
}
