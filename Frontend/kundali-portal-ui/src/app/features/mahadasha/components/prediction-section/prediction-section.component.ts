import { Component } from '@angular/core';

@Component({
  selector: 'app-prediction-section',
  standalone: true,
  template: `
    <section class="placeholder-card">
      <h3>Prediction Section</h3>
      <p>Show detailed predictions for the selected period.</p>
    </section>
  `,
  styles: [
    `
      .placeholder-card {
        border: 1px solid #e5e7eb;
        border-radius: 12px;
        padding: 1rem;
        background: #fff;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05);
      }
    `,
  ],
})
export class PredictionSectionComponent {}
