import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { FeedbackTopic } from './product-feedback.service';

export interface ActiveFeedbackPrompt {
  id: string;
  surface: string;
  contextKey: string;
  prompt: string;
  topics: FeedbackTopic[];
}

@Injectable({
  providedIn: 'root'
})
export class FeedbackPromptRegistry {
  private readonly items = new Map<string, ActiveFeedbackPrompt>();
  readonly prompts$ = new BehaviorSubject<ActiveFeedbackPrompt[]>([]);

  set(prompt: ActiveFeedbackPrompt): void {
    this.items.set(prompt.id, prompt);
    this.prompts$.next([...this.items.values()]);
  }

  remove(id: string): void {
    if (!this.items.delete(id)) {
      return;
    }

    this.prompts$.next([...this.items.values()]);
  }
}
