import { Component, Input, OnChanges, OnDestroy, inject } from '@angular/core';
import { FeedbackTopic, ProductFeedbackService } from '../../../core/services/product-feedback.service';
import { FeedbackPromptRegistry } from '../../../core/services/feedback-prompt.registry';

@Component({
  selector: 'app-result-feedback',
  standalone: true,
  template: '',
  styles: [':host { display: none; }'],
})
export class ResultFeedbackComponent implements OnChanges, OnDestroy {
  private readonly feedback = inject(ProductFeedbackService);
  private readonly registry = inject(FeedbackPromptRegistry);

  @Input() surface = '';
  @Input() contextKey = '';
  @Input() prompt = 'Was this useful?';
  @Input() topics: FeedbackTopic[] = [];

  private activeId = '';

  ngOnChanges(): void {
    this.clear();
    if (!this.surface || !this.contextKey) {
      return;
    }

    const id = `${this.surface}:${this.contextKey}`;
    this.feedback.prompt(this.surface, this.contextKey).subscribe({
      next: (status) => {
        if (!status.show || id !== `${this.surface}:${this.contextKey}`) {
          return;
        }

        this.activeId = id;
        this.registry.set({
          id,
          surface: this.surface,
          contextKey: this.contextKey,
          prompt: this.prompt,
          topics: this.topics ?? [],
        });
      },
      error: () => this.clear(),
    });
  }

  ngOnDestroy(): void {
    this.clear();
  }

  private clear(): void {
    if (!this.activeId) {
      return;
    }

    this.registry.remove(this.activeId);
    this.activeId = '';
  }
}
