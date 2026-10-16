import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-consultation-predictions',
  standalone: true,
  imports: [CommonModule, RouterLink, TranslocoPipe],
  template: `
    <ng-container *ngIf="isPremium; else teaser">
      <div class="item-list" *ngIf="cards.length; else empty">
        <article class="item-card" *ngFor="let card of cards">
          <header>
            <strong>{{ card.lifeArea }}</strong>
            <span *ngIf="card.tone">{{ card.tone }}</span>
          </header>
          <p *ngIf="card.summary">{{ card.summary }}</p>
          <ul *ngIf="card.claims.length">
            <li *ngFor="let claim of card.claims">{{ claim }}</li>
          </ul>
        </article>
      </div>
    </ng-container>

    <ng-template #teaser>
      <div class="teaser">
        <p>{{ 'kundali.predictions.teaserCopy' | transloco }}</p>
        <a routerLink="/app/billing">{{ 'kundali.predictions.unlockCta' | transloco }}</a>
      </div>
    </ng-template>

    <ng-template #empty>
      <p class="empty">{{ 'consultation.sectionEmpty' | transloco }}</p>
    </ng-template>
  `,
  styles: [`
    .item-list { display: grid; gap: 10px; }
    .item-card {
      padding: 12px 14px;
      border: 1px solid var(--theme-line, #e0e0e0);
      border-radius: 12px;
      background: var(--theme-surface, #fff);
    }
    header { display: flex; gap: 10px; align-items: baseline; justify-content: space-between; }
    header span, .empty { color: var(--theme-text-muted, #5d6689); font-size: 12px; }
    p { margin: 8px 0 0; font-size: 13px; line-height: 1.45; }
    ul { margin: 8px 0 0; padding-left: 18px; font-size: 13px; }
    .teaser {
      display: grid;
      gap: 10px;
      padding: 16px;
      border: 1px solid var(--theme-line, #e0e0e0);
      border-radius: 12px;
      background: var(--theme-surface, #fff);
    }
    .teaser p { margin: 0; }
    .teaser a { color: var(--theme-accent, #c56f50); font-weight: 700; text-decoration: none; }
  `]
})
export class ConsultationPredictionsSection {
  @Input() predictions: any[] = [];
  @Input() isPremium = false;

  get cards(): Array<{ lifeArea: string; tone: string; summary: string; claims: string[] }> {
    return (Array.isArray(this.predictions) ? this.predictions : [])
      .map(item => {
        const lifeArea = String(item?.lifeArea ?? item?.LifeArea ?? '').trim();
        if (!lifeArea) {
          return null;
        }

        const synthesis = item?.synthesis ?? item?.Synthesis ?? {};
        const tone = String(synthesis?.overallTone ?? synthesis?.OverallTone ?? item?.tone ?? '').trim();
        const summary = String(
          synthesis?.summary ??
            synthesis?.Summary ??
            synthesis?.headline ??
            synthesis?.Headline ??
            item?.summary ??
            ''
        ).trim();
        const claims = (Array.isArray(item?.claims) ? item.claims : [])
          .map((claim: any) => String(claim?.text ?? claim?.Text ?? claim?.statement ?? claim ?? '').trim())
          .filter(Boolean)
          .slice(0, 4);

        return { lifeArea, tone, summary, claims };
      })
      .filter((card): card is { lifeArea: string; tone: string; summary: string; claims: string[] } => !!card);
  }
}
