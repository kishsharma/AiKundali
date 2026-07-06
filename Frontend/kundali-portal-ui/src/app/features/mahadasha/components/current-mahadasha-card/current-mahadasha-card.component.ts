import { Component } from '@angular/core';

@Component({
  selector: 'app-current-mahadasha-card',
  standalone: true,
  template: `
    <section class="placeholder-card">
      <h3>Current Mahadasha</h3>
      <p>Show the currently active Mahadasha and its key influence.</p>
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
export class CurrentMahadashaCardComponent {}
