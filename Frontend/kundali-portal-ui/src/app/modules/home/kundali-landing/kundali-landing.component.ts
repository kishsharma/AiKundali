import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AstrologyIconComponent, AstrologyIconName } from '../../../shared/components/astrology-icon.component';

interface LandingNavLink {
  label: string;
  link: string;
}

interface LandingFeature {
  icon: AstrologyIconName;
  title: string;
  description: string;
  tone: 'purple' | 'orange';
  link: string;
}

interface TrustMetric {
  icon: AstrologyIconName;
  value: string;
  label: string;
}

@Component({
  selector: 'app-kundali-landing',
  standalone: true,
  imports: [CommonModule, RouterLink, AstrologyIconComponent],
  templateUrl: './kundali-landing.component.html',
  styleUrl: './kundali-landing.component.scss',
})
export class KundaliLandingComponent {
  readonly navLinks: LandingNavLink[] = [
    { label: 'Home', link: '/' },
    { label: 'Kundali', link: '/app/kundali' },
    { label: 'Matchmaking', link: '/app/compatibility' },
    { label: 'Panchang', link: '/app/panchang' },
    { label: 'Dasha', link: '/app/dasha' },
  ];

  readonly watermarkWords = ['जन्म निर्माण', 'ग्रह', 'नक्षत्र', 'राशि', 'लग्न', 'योग', 'दशा', 'भाव'];

  readonly heroHighlights = [
    { icon: 'kundali' as AstrologyIconName, label: 'North Indian chart' },
    { icon: 'bhava-score' as AstrologyIconName, label: 'House-wise scoring' },
    { icon: 'reports' as AstrologyIconName, label: 'PDF report export' },
    { icon: 'settings' as AstrologyIconName, label: 'White-label branding' },
  ];

  readonly features: LandingFeature[] = [
    {
      icon: 'kundali',
      title: 'Kundali Analysis',
      description: 'Birth details, chart placement, panchang and readable section summaries in one flow.',
      tone: 'purple',
      link: '/app/kundali',
    },
    {
      icon: 'bhava-score',
      title: 'Bhava Scoring',
      description: 'See house strength as practical scores instead of making clients read raw chart data.',
      tone: 'orange',
      link: '/app/kundali',
    },
    {
      icon: 'compatibility',
      title: 'Matchmaking',
      description: 'Compare two charts with compatibility scores, dosha context and export-ready notes.',
      tone: 'purple',
      link: '/app/compatibility',
    },
    {
      icon: 'panchang',
      title: 'Panchang',
      description: 'Keep tithi, nakshatra and daily panchang details close to the client workflow.',
      tone: 'orange',
      link: '/app/panchang',
    },
  ];

  readonly metrics: TrustMetric[] = [
    { icon: 'compatibility', value: '2', label: 'Chart comparison modes' },
    { icon: 'settings', value: '1', label: 'Branding setup page' },
    { icon: 'reports', value: 'PDF', label: 'Client-ready exports' },
    { icon: 'yogas', value: '12', label: 'Report sections planned' },
  ];
}
