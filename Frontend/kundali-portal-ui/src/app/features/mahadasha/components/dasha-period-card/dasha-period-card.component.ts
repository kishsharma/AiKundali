import { Component } from '@angular/core';

@Component({
  selector: 'app-dasha-period-card',
  standalone: true,
  template: `
    <section class="placeholder-card">
      <h3>Dasha Period Card</h3>
      <p>Present a focused card for a particular period.</p>
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
export class DashaPeriodCardComponent {}
