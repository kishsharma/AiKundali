import { Component } from '@angular/core';

@Component({
  selector: 'app-remedy-card',
  standalone: true,
  template: `
    <section class="placeholder-card">
      <h3>Remedy Card</h3>
      <p>Provide practical remedies tailored to the current dasha.</p>
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
export class RemedyCardComponent {}
