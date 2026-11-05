import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThemeService } from '../../../core/services/theme.service';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';
import { SiteFooterComponent } from '../../../shared/components/site-footer/site-footer.component';
import { NorthIndianChartComponent } from '../../../components/north-indian-chart/north-indian-chart.component';

@Component({
  selector: 'app-astrologer-landing',
  standalone: true,
  imports: [RouterLink, BrandLogoComponent, SiteFooterComponent, NorthIndianChartComponent],
  templateUrl: './astrologer-landing.component.html',
  styleUrl: './astrologer-landing.component.scss',
})
export class AstrologerLandingComponent {
  readonly themeService = inject(ThemeService);
  readonly sampleAscendant = '1 - Aries';
  readonly samplePlanets = [
    { planet: 'Venus', house: 2 },
    { planet: 'Mercury', house: 3 },
    { planet: 'Sun', house: 5 },
    { planet: 'Mars', house: 6 },
    { planet: 'Saturn', house: 10 },
    { planet: 'Jupiter', house: 11 },
    { planet: 'Moon', house: 12 },
  ];

  get themeToggleLabel(): string {
    return this.themeService.mode() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode';
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
