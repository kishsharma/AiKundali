import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, HostListener, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ActiveFeedbackPrompt, FeedbackPromptRegistry } from '../../../core/services/feedback-prompt.registry';
import { ProductFeedbackService } from '../../../core/services/product-feedback.service';

@Component({
  selector: 'app-feedback-badge',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './feedback-badge.component.html',
  styleUrl: './feedback-badge.component.scss',
})
export class FeedbackBadgeComponent implements OnInit, OnDestroy {
  private readonly registry = inject(FeedbackPromptRegistry);
  private readonly feedback = inject(ProductFeedbackService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private subscription?: Subscription;

  prompts: ActiveFeedbackPrompt[] = [];
  readonly generalPrompt: ActiveFeedbackPrompt = {
    id: 'general:app',
    surface: 'general',
    contextKey: 'app',
    prompt: 'What should we improve?',
    topics: [
      { key: 'chart', label: 'Chart' },
      { key: 'match', label: 'Match' },
      { key: 'dhruva', label: 'Dhruva' },
      { key: 'other', label: 'Something else' },
    ],
  };
  open = false;
  selectedId = '';
  sentiment: 'useful' | 'needs_work' | '' = '';
  topicKey = '';
  note = '';
  saving = false;
  thanks = false;
  errorMessage = '';

  ngOnInit(): void {
    this.subscription = this.registry.prompts$.subscribe((prompts) => {
      this.prompts = prompts;
      if (!this.thanks && !prompts.some((item) => item.id === this.selectedId)) {
        this.selectedId = prompts[0]?.id ?? '';
        this.resetForm();
      }
      if (!prompts.length && !this.thanks) {
        this.open = false;
      }
      this.changeDetector.markForCheck();
    });
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  get offers(): ActiveFeedbackPrompt[] {
    return this.prompts.length ? this.prompts : [this.generalPrompt];
  }

  get selected(): ActiveFeedbackPrompt {
    return this.offers.find((item) => item.id === this.selectedId) ?? this.offers[0];
  }

  surfaceLabel(surface: string): string {
    switch (surface) {
      case 'kundali':
        return 'Chart';
      case 'compatibility':
        return 'Match';
      case 'report':
        return 'Report';
      case 'dhruva':
        return 'Dhruva reply';
      case 'general':
        return 'App';
      default:
        return 'Feedback';
    }
  }

  openPopup(): void {
    this.open = true;
    this.thanks = false;
    this.errorMessage = '';
    if (!this.selectedId) {
      this.selectedId = this.offers[0]?.id ?? '';
    }
  }

  closePopup(): void {
    this.open = false;
    this.resetForm();
  }

  choosePrompt(id: string): void {
    this.selectedId = id;
    this.resetForm();
  }

  choose(value: 'useful' | 'needs_work'): void {
    this.sentiment = value;
    this.errorMessage = '';
    if (value === 'useful') {
      this.topicKey = '';
    }
  }

  send(): void {
    const prompt = this.selected;
    if (!prompt || !this.sentiment || this.saving) {
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    this.feedback.submit({
      surface: prompt.surface,
      contextKey: prompt.contextKey,
      sentiment: this.sentiment,
      topic: this.sentiment === 'needs_work' ? this.topicKey || null : null,
      note: this.note.trim(),
    }).subscribe({
      next: () => {
        this.saving = false;
        this.thanks = true;
        this.registry.remove(prompt.id);
        this.changeDetector.markForCheck();
      },
      error: (error: { error?: { message?: string }; message?: string }) => {
        this.saving = false;
        this.errorMessage = error?.error?.message || error?.message || 'Unable to send feedback.';
        this.changeDetector.markForCheck();
      },
    });
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open) {
      this.closePopup();
    }
  }

  private resetForm(): void {
    this.sentiment = '';
    this.topicKey = '';
    this.note = '';
    this.thanks = false;
    this.errorMessage = '';
  }
}
