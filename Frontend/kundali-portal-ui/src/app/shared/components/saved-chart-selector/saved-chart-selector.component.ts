import { CommonModule } from '@angular/common';
import { Component, ElementRef, EventEmitter, HostListener, Input, OnDestroy, OnInit, Output, inject } from '@angular/core';
import { Subscription } from 'rxjs';

import { KundaliService } from '../../../modules/kundali/kundali.service';
import { SavedKundaliDetail, SavedKundaliListItem } from '../../services/saved-chart-mapper.service';

@Component({
  selector: 'app-saved-chart-selector',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="saved-chart-selector">
      <label class="selector-label" [class.visually-hidden]="hideLabel" [attr.for]="inputId">{{ label }}</label>
      <div class="selector-control" [class.open]="open">
        <input
          [id]="inputId"
          type="search"
          [placeholder]="placeholder"
          [value]="query"
          autocomplete="off"
          (focus)="openSelector()"
          (input)="onSearch($any($event.target).value)" />
        <button type="button" class="selector-trigger" (click)="toggleSelector()" [attr.aria-expanded]="open" aria-label="Open saved chart list">
          <span></span>
        </button>
      </div>

      <div class="selector-dropdown" *ngIf="open">
        <div class="selector-status" *ngIf="loading">Loading saved charts...</div>
        <div class="selector-status" *ngIf="!loading && !filteredItems.length">No saved charts found.</div>
        <button
          type="button"
          class="selector-option"
          *ngFor="let item of filteredItems; trackBy: trackById"
          [class.selected]="item.id === selectedId"
          [disabled]="loadingDetailId === item.id"
          (mousedown)="$event.preventDefault(); selectChart(item)">
          <strong>{{ item.displayName || item.fullName }}</strong>
          <span>{{ formatDate(item.dateOfBirth) }} · {{ item.birthPlace || 'Birth place not available' }}</span>
          <small *ngIf="loadingDetailId === item.id">Opening...</small>
        </button>
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-width: 0;
    }

    .saved-chart-selector {
      position: relative;
      display: grid;
      gap: 5px;
      min-width: 0;
    }

    .selector-label {
      color: var(--theme-text-muted, #6b7280);
      font-size: 10px;
      font-weight: 800;
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }

    .selector-label.visually-hidden {
      position: absolute;
      width: 1px;
      height: 1px;
      overflow: hidden;
      clip: rect(0 0 0 0);
      white-space: nowrap;
    }

    .selector-control {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 32px;
      min-height: 36px;
      border: 1px solid var(--theme-line, #d9deea);
      border-radius: var(--theme-radius-control, 8px);
      background: var(--theme-input-bg, #fff);
      overflow: hidden;
    }

    .selector-control.open,
    .selector-control:focus-within {
      border-color: var(--theme-accent, #d97706);
      box-shadow: 0 0 0 2px color-mix(in srgb, var(--theme-accent, #d97706) 14%, transparent);
    }

    input {
      min-width: 0;
      border: 0;
      padding: 0 10px;
      color: var(--theme-text, #111827);
      background: transparent;
      font: 700 12px/1.2 Inter, system-ui, sans-serif;
      outline: none;
    }

    .selector-trigger {
      display: grid;
      place-items: center;
      border: 0;
      border-left: 1px solid var(--theme-line, #d9deea);
      background: transparent;
      color: var(--theme-text-muted, #6b7280);
      cursor: pointer;
    }

    .selector-trigger span {
      width: 8px;
      height: 8px;
      border-right: 2px solid currentColor;
      border-bottom: 2px solid currentColor;
      transform: translateY(-2px) rotate(45deg);
    }

    .selector-dropdown {
      position: absolute;
      left: 0;
      right: 0;
      top: calc(100% + 6px);
      z-index: 80;
      display: grid;
      gap: 3px;
      max-height: 240px;
      overflow-y: auto;
      border: 1px solid var(--theme-line, #d9deea);
      border-radius: 8px;
      padding: 6px;
      background: var(--theme-surface, #fff);
      box-shadow: 0 16px 34px var(--theme-shadow, rgba(15, 23, 42, 0.16));
    }

    .selector-option {
      display: grid;
      gap: 3px;
      width: 100%;
      border: 0;
      border-radius: 6px;
      padding: 8px;
      color: var(--theme-text, #111827);
      background: transparent;
      text-align: left;
      cursor: pointer;
    }

    .selector-option:hover,
    .selector-option:focus-visible,
    .selector-option.selected {
      background: color-mix(in srgb, var(--theme-accent, #d97706) 10%, transparent);
      outline: none;
    }

    .selector-option strong {
      overflow: hidden;
      font-size: 12px;
      line-height: 1.2;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .selector-option span,
    .selector-option small,
    .selector-status {
      color: var(--theme-text-muted, #6b7280);
      font-size: 10.5px;
      line-height: 1.25;
    }

    .selector-status {
      padding: 8px;
      font-weight: 700;
    }
  `]
})
export class SavedChartSelectorComponent implements OnInit, OnDestroy {
  private readonly kundaliService = inject(KundaliService);
  private readonly elementRef = inject(ElementRef<HTMLElement>);
  private readonly subscription = new Subscription();

  @Input() label = 'Select chart';
  @Input() hideLabel = false;
  @Input() placeholder = 'Search saved Kundali';
  @Input() selectedId: string | null = null;
  @Output() chartSelected = new EventEmitter<SavedKundaliDetail>();

  readonly inputId = `saved-chart-${Math.random().toString(36).slice(2)}`;
  items: SavedKundaliListItem[] = [];
  query = '';
  open = false;
  loading = false;
  loadingDetailId: string | null = null;

  ngOnInit(): void {
    this.loadCharts();
    this.subscription.add(this.kundaliService.savedKundaliChanged$.subscribe(() => this.loadCharts()));
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  get filteredItems(): SavedKundaliListItem[] {
    const term = this.query.trim().toLowerCase();
    if (!term) {
      return this.items;
    }

    return this.items.filter((item) => [
      item.displayName,
      item.fullName,
      item.birthPlace,
      item.dateOfBirth,
    ].some((value) => String(value ?? '').toLowerCase().includes(term)));
  }

  @HostListener('document:click', ['$event'])
  closeOnOutsideClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.open = false;
    }
  }

  openSelector(): void {
    this.open = true;
  }

  toggleSelector(): void {
    this.open = !this.open;
  }

  onSearch(value: string): void {
    this.query = value;
    this.open = true;
  }

  selectChart(item: SavedKundaliListItem): void {
    this.loadingDetailId = item.id;
    this.kundaliService.getSavedKundali(item.id).subscribe({
      next: (detail) => {
        this.loadingDetailId = null;
        this.selectedId = detail?.id ?? item.id;
        this.query = detail?.fullName || item.displayName || item.fullName;
        this.open = false;
        this.chartSelected.emit(detail);
      },
      error: (error) => {
        console.error('Saved chart selector load error:', error);
        this.loadingDetailId = null;
      }
    });
  }

  trackById(_: number, item: SavedKundaliListItem): string {
    return item.id;
  }

  formatDate(value: string): string {
    return String(value ?? '').slice(0, 10) || 'Date not available';
  }

  private loadCharts(): void {
    this.loading = true;
    this.kundaliService.getSavedKundalis().subscribe({
      next: (items) => {
        this.items = Array.isArray(items) ? items : [];
        this.loading = false;
      },
      error: (error) => {
        console.error('Saved chart selector list error:', error);
        this.items = [];
        this.loading = false;
      }
    });
  }
}
