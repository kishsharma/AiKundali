import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    <div class="store-live" [class.store-live-blocked]="storeComingSoon" [attr.aria-hidden]="storeComingSoon ? true : null">
      <router-outlet />
    </div>
    @if (storeComingSoon) {
      <div class="store-soon" role="dialog" aria-modal="true" aria-labelledby="store-soon-title">
        <div class="store-soon-card">
          <img src="assets/brand/horalife-icon.svg" alt="">
          <p class="store-soon-kicker">HoraLife Store</p>
          <h1 id="store-soon-title">Coming soon</h1>
          <p>Certified gemstones matched to your kundali will be available here.</p>
        </div>
      </div>
    }
  `
})
export class App {
  /** Set true to show the coming-soon gate again. */
  readonly storeComingSoon = false;

  constructor(readonly themes: ThemeService) {}
}
