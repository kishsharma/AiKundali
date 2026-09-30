import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BrandLogoComponent } from '../../../shared/components/brand-logo/brand-logo.component';
import { SiteFooterComponent } from '../../../shared/components/site-footer/site-footer.component';
import { SubscriptionModalService } from '../../../core/services/subscription-modal.service';
import { environment } from '../../../config/environment/environment';

interface StoreShowcaseItem {
  slug: string;
  name: string;
  type: 'Gemstone' | 'Yantra' | 'Rudraksha';
  image: string;
}

@Component({
  selector: 'app-kundali-landing',
  standalone: true,
  imports: [RouterLink, BrandLogoComponent, SiteFooterComponent],
  templateUrl: './kundali-landing.component.html',
  styleUrl: './kundali-landing.component.scss',
})
export class KundaliLandingComponent implements AfterViewInit, OnDestroy {
  readonly storeHomeUrl = environment.storeBaseUrl.replace(/\/$/, '');
  readonly storeItems: StoreShowcaseItem[] = [
    { slug: 'yellow-sapphire-pukhraj', name: 'Yellow Sapphire', type: 'Gemstone', image: 'assets/store-showcase/yellow-sapphire-pukhraj.png' },
    { slug: 'blue-sapphire-neelam', name: 'Blue Sapphire', type: 'Gemstone', image: 'assets/store-showcase/blue-sapphire-neelam.png' },
    { slug: 'ruby-manikya', name: 'Ruby', type: 'Gemstone', image: 'assets/store-showcase/ruby-manikya.png' },
    { slug: 'emerald-panna', name: 'Emerald', type: 'Gemstone', image: 'assets/store-showcase/emerald-panna.png' },
    { slug: 'pearl-moti', name: 'Pearl', type: 'Gemstone', image: 'assets/store-showcase/pearl-moti.png' },
    { slug: 'red-coral-moonga', name: 'Red Coral', type: 'Gemstone', image: 'assets/store-showcase/red-coral-moonga.png' },
    { slug: 'hessonite-gomed', name: 'Hessonite', type: 'Gemstone', image: 'assets/store-showcase/hessonite-gomed.png' },
    { slug: 'white-sapphire-safed-pukhraj', name: 'White Sapphire', type: 'Gemstone', image: 'assets/store-showcase/white-sapphire.webp' },
    { slug: 'cats-eye-lehsunia', name: "Cat's Eye", type: 'Gemstone', image: 'assets/store-showcase/cats-eye-lehsunia.webp' },
    { slug: 'shree-yantra', name: 'Shree Yantra', type: 'Yantra', image: 'assets/store-showcase/shree-yantra.webp' },
    { slug: 'navagraha-yantra', name: 'Navagraha Yantra', type: 'Yantra', image: 'assets/store-showcase/navagraha-yantra.webp' },
    { slug: 'five-mukhi-rudraksha', name: '5 Mukhi Rudraksha', type: 'Rudraksha', image: 'assets/store-showcase/five-mukhi-rudraksha.webp' },
  ];

  private readonly zodiacSymbols = [
    '\u2648', '\u2649', '\u264A', '\u264B', '\u264C', '\u264D',
    '\u264E', '\u264F', '\u2650', '\u2651', '\u2652', '\u2653',
  ];
  private readonly words = [
    '\u0917\u094d\u0930\u0939', '\u0939\u094b\u0930\u093e', '\u0915\u0941\u0923\u094d\u0921\u0932\u0940', '\u091c\u0928\u094d\u092e', '\u0926\u0943\u0937\u094d\u091f\u093f', '\u0930\u093e\u0936\u093f', '\u092d\u093e\u0935', '\u0928\u0915\u094d\u0937\u0924\u094d\u0930',
    '\u0938\u0942\u0930\u094d\u092f', '\u091a\u0928\u094d\u0926\u094d\u0930', '\u092e\u0902\u0917\u0932', '\u092c\u0941\u0927', '\u0917\u0941\u0930\u0941', '\u0936\u0941\u0915\u094d\u0930', '\u0936\u0928\u093f', '\u0930\u093e\u0939\u0941', '\u0915\u0947\u0924\u0941',
    '\u0b17\u0b4d\u0b30\u0b39', '\u0b39\u0b4b\u0b30\u0b3e', '\u0b15\u0b41\u0b23\u0b4d\u0b21\u0b33\u0b40', '\u0b1c\u0b28\u0b4d\u0b2e', '\u0b26\u0b43\u0b37\u0b4d\u0b1f\u0b3f', '\u0b30\u0b3e\u0b36\u0b3f', '\u0b2d\u0b3e\u0b2c', '\u0b28\u0b15\u0b4d\u0b37\u0b24\u0b4d\u0b30',
    '\u0b38\u0b42\u0b30\u0b4d\u0b2f', '\u0b1a\u0b28\u0b4d\u0b26\u0b4d\u0b30', '\u0b2e\u0b19\u0b4d\u0b17\u0b33', '\u0b2c\u0b41\u0b27', '\u0b17\u0b41\u0b30\u0b41', '\u0b36\u0b41\u0b15\u0b4d\u0b30', '\u0b36\u0b28\u0b3f', '\u0b30\u0b3e\u0b39\u0b41', '\u0b15\u0b47\u0b24\u0b41',
    '\u0c97\u0ccd\u0cb0\u0cb9', '\u0cb9\u0ccb\u0cb0\u0cbe', '\u0c95\u0cc1\u0c82\u0ca1\u0cb2\u0cbf', '\u0c9c\u0ca8\u0ccd\u0cae', '\u0ca6\u0cc3\u0cb7\u0ccd\u0c9f\u0cbf', '\u0cb0\u0cbe\u0cb6\u0cbf', '\u0cad\u0cbe\u0cb5', '\u0ca8\u0c95\u0ccd\u0cb7\u0ca4\u0ccd\u0cb0',
    '\u0cb8\u0cc2\u0cb0\u0ccd\u0caf', '\u0c9a\u0c82\u0ca6\u0ccd\u0cb0', '\u0cae\u0c82\u0c97\u0cb3', '\u0cac\u0cc1\u0ca7', '\u0c97\u0cc1\u0cb0\u0cc1', '\u0cb6\u0cc1\u0c95\u0ccd\u0cb0', '\u0cb6\u0ca8\u0cbf', '\u0cb0\u0cbe\u0cb9\u0cc1', '\u0c95\u0cc7\u0ca4\u0cc1',
    '\u0b17\u0b4d\u0b30\u0b39', '\u0b39\u0b4b\u0b30\u0b3e', '\u0b15\u0b41\u0b23\u0b4d\u0b21\u0b33\u0b40', '\u0b1c\u0b28\u0b4d\u0b2e', '\u0b26\u0b43\u0b37\u0b4d\u0b1f\u0b3f', '\u0b30\u0b3e\u0b36\u0b3f', '\u0b2d\u0b3e\u0b2c', '\u0b28\u0b15\u0b4d\u0b37\u0b24\u0b4d\u0b30',
    '\u0b38\u0b42\u0b30\u0b4d\u0b2f', '\u0b1a\u0b28\u0b4d\u0b26\u0b4d\u0b30', '\u0b2e\u0b19\u0b4d\u0b17\u0b33', '\u0b2c\u0b41\u0b27', '\u0b17\u0b41\u0b30\u0b41', '\u0b36\u0b41\u0b15\u0b4d\u0b30', '\u0b36\u0b28\u0b3f', '\u0b30\u0b3e\u0b39\u0b41', '\u0b15\u0b47\u0b24\u0b41',
  ];

  private readonly saturnGlyph = '<svg style="display:block;width:1em;height:1em;overflow:visible" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg"><ellipse cx="32" cy="36" rx="29" ry="8.5" fill="none" stroke="currentColor" stroke-width="2.2" transform="rotate(-12 32 36)"/><circle cx="32" cy="28" r="13" fill="none" stroke="currentColor" stroke-width="2.2"/></svg>';

  isNavScrolled = false;

  private astrologerScrollTimer?: ReturnType<typeof window.setInterval>;
  private readonly subscriptionModal = inject(SubscriptionModalService);

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  openPlans(): void {
    this.subscriptionModal.open();
  }

  storeHref(slug: string): string {
    return `${this.storeHomeUrl}/products/${encodeURIComponent(slug)}`;
  }

  ngAfterViewInit(): void {
    this.updateNavScrolledState();
    this.scatter('deva-hero', 10);
    this.scatter('deva-tools', 6);
    this.scatter('deva-process', 6);
    this.scatter('deva-astrologers', 7);
    this.scatter('deva-eval', 8);
    this.scatter('deva-preview', 7);
    this.scatter('deva-milan', 7);
    this.scatter('deva-philosophy', 5);
    this.scatter('deva-store', 6);
    this.scatter('deva-cta', 7);
    this.scatter('deva-footer', 4);
    this.scatterStars();
    this.startAstrologerAutoScroll();
  }

  ngOnDestroy(): void {
    if (this.astrologerScrollTimer) {
      window.clearInterval(this.astrologerScrollTimer);
    }
  }

  @HostListener('window:scroll')
  onWindowScroll(): void {
    this.updateNavScrolledState();
  }

  scrollAstrologers(direction: number): void {
    const scroller = this.elementRef.nativeElement.querySelector<HTMLElement>('.astrologer-scroll');
    const card = scroller?.querySelector<HTMLElement>('.astrologer-review');

    if (!scroller || !card) {
      return;
    }

    const gap = 18;
    const step = card.offsetWidth + gap;
    const nextLeft = scroller.scrollLeft + direction * step;
    const maxLeft = scroller.scrollWidth - scroller.clientWidth;

    if (direction > 0 && nextLeft >= maxLeft - 4) {
      scroller.scrollTo({ left: 0, behavior: 'smooth' });
      return;
    }

    if (direction < 0 && nextLeft <= 0) {
      scroller.scrollTo({ left: maxLeft, behavior: 'smooth' });
      return;
    }

    scroller.scrollBy({
      left: direction * step,
      behavior: 'smooth',
    });
  }

  private updateNavScrolledState(): void {
    this.isNavScrolled = window.scrollY > 16;
  }

  private startAstrologerAutoScroll(): void {
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
      return;
    }

    this.astrologerScrollTimer = window.setInterval(() => {
      this.scrollAstrologers(1);
    }, 4200);
  }

  private scatter(id: string, count: number): void {
    const el = this.elementRef.nativeElement.querySelector<HTMLElement>(`#${id}`);
    if (!el) {
      return;
    }

    el.innerHTML = '';

    for (let i = 0; i < count; i++) {
      const span = document.createElement('span');
      const size = 22 + Math.random() * 46;

      const markerType = Math.random();

      if (markerType < 0.18) {
        span.innerHTML = this.saturnGlyph;
        span.classList.add('glyph-saturn');
        span.style.fontSize = `${size}px`;
      } else if (markerType < 0.24) {
        span.textContent = this.zodiacSymbols[Math.floor(Math.random() * this.zodiacSymbols.length)];
        span.classList.add('zodiac-symbol');
        span.style.fontSize = `${size * 1.65}px`;
      } else {
        span.textContent = this.words[Math.floor(Math.random() * this.words.length)];
        span.style.fontSize = `${size}px`;
      }

      span.style.position = 'absolute';
      span.style.fontFamily = "'Nirmala UI', 'Mangal', 'Kokila', 'Noto Sans Devanagari', 'Noto Sans Kannada', 'Noto Sans Oriya', 'Kalinga', 'Cormorant Garamond', serif";
      span.style.color = 'var(--brass)';
      span.style.opacity = '0.05';
      span.style.whiteSpace = 'nowrap';
      span.style.userSelect = 'none';
      span.style.left = `${Math.random() * 90}%`;
      span.style.top = `${Math.random() * 88}%`;
      span.style.transform = `rotate(${Math.random() * 30 - 15}deg)`;
      el.appendChild(span);
    }
  }

  private scatterStars(): void {
    const starField = this.elementRef.nativeElement.querySelector<HTMLElement>('#stars');
    if (!starField) {
      return;
    }

    starField.innerHTML = '';

    for (let i = 0; i < 38; i++) {
      const span = document.createElement('span');
      span.style.position = 'absolute';
      span.style.width = '2px';
      span.style.height = '2px';
      span.style.background = 'var(--brass)';
      span.style.borderRadius = '50%';
      span.style.animation = 'twinkle 4.5s ease-in-out infinite';
      span.style.left = `${Math.random() * 100}%`;
      span.style.top = `${Math.random() * 100}%`;
      span.style.animationDelay = `${Math.random() * 4}s`;
      starField.appendChild(span);
    }
  }
}

