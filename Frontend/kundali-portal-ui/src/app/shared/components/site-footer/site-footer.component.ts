import { Component, Input } from '@angular/core';
import { NgFor } from '@angular/common';
import { RouterLink } from '@angular/router';
import { business } from '../../../config/business.config';

@Component({
  selector: 'app-site-footer',
  standalone: true,
  imports: [NgFor, RouterLink],
  templateUrl: './site-footer.component.html',
  styleUrl: './site-footer.component.scss',
})
export class SiteFooterComponent {
  @Input() tone: 'landing' | 'app' = 'app';
  readonly business = business;
  readonly year = new Date().getFullYear();
  readonly groups = [
    {
      title: 'Company',
      links: [
        { label: 'About HoraLife', path: '/about' },
        { label: 'Contact Us', path: '/contact' },
      ],
    },
    {
      title: 'Services',
      links: [
        { label: 'Astrology Services', path: '/services' },
        { label: 'Pricing', path: '/pricing' },
      ],
    },
    {
      title: 'Legal',
      links: [
        { label: 'Terms & Conditions', path: '/terms' },
        { label: 'Privacy Policy', path: '/privacy' },
        { label: 'Refund & Cancellation Policy', path: '/refund-policy' },
        { label: 'Shipping / Delivery Policy', path: '/shipping-policy' },
      ],
    },
    {
      title: 'Support',
      links: [
        { label: 'Help & Support', path: '/support' },
        { label: 'Grievance Redressal', path: '/grievance' },
      ],
    },
  ];
}
