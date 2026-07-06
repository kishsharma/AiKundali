import { Component } from '@angular/core';

@Component({
  selector: 'app-dasha-table',
  standalone: true,
  template: `
    <section class="placeholder-card">
      <h3>Dasha Table</h3>
      <p>Show the dasha progression in a structured table.</p>
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
export class DashaTableComponent {}
