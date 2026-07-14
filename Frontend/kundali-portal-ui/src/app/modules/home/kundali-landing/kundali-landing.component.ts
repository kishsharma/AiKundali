import { AfterViewInit, Component, ElementRef } from '@angular/core';

@Component({
  selector: 'app-kundali-landing',
  standalone: true,
  templateUrl: './kundali-landing.component.html',
  styleUrl: './kundali-landing.component.scss',
})
export class KundaliLandingComponent implements AfterViewInit {
  private readonly zodiacSymbols = [
    '♈', '♉', '♊', '♋', '♌', '♍',
    '♎', '♏', '♐', '♑', '♒', '♓',
  ];

  private readonly words = [
    'ग्रह', 'होरा', 'कुण्डली', 'जन्म', 'दृष्टि', 'राशि', 'भाव', 'नक्षत्र',
    'सूर्य', 'चन्द्र', 'मंगल', 'बुध', 'गुरु', 'शुक्र', 'शनि', 'राहु', 'केतु',
    'मेष', 'वृषभ', 'मिथुन', 'कर्क', 'सिंह', 'कन्या', 'तुला', 'वृश्चिक', 'धनु', 'मकर', 'कुम्भ', 'मीन',
    'ಗ್ರಹ', 'ಹೋರಾ', 'ಕುಂಡಲಿ', 'ಜನ್ಮ', 'ದೃಷ್ಟಿ', 'ರಾಶಿ', 'ಭವ', 'ನಕ್ಷತ್ರ',
    'ಸೂರ್ಯ', 'ಚಂದ್ರ', 'ಮಂಗಳ', 'ಬುಧ', 'ಗುರು', 'ಶುಕ್ರ', 'ಶನಿ', 'ರಾಹು', 'ಕೇತು',
    'ಮೇಷ', 'ವೃಷಭ', 'ಮಿಥುನ', 'ಕರ್ಕ', 'ಸಿಂಹ', 'ಕನ್ಯಾ', 'ತುಲಾ', 'ವೃಶ್ಚಿಕ', 'ಧನು', 'ಮಕರ', 'ಕುಂಭ', 'ಮೀನ',
    'ଗ୍ରହ', 'ହୋରା', 'କୁଣ୍ଡଳୀ', 'ଜନ୍ମ', 'ଦୃଷ୍ଟି', 'ରାଶି', 'ଭାବ', 'ନକ୍ଷତ୍ର',
    'ସୂର୍ଯ୍ୟ', 'ଚନ୍ଦ୍ର', 'ମଙ୍ଗଳ', 'ବୁଧ', 'ଗୁରୁ', 'ଶୁକ୍ର', 'ଶନି', 'ରାହୁ', 'କେତୁ',
    'ମେଷ', 'ବୃଷଭ', 'ମିଥୁନ', 'କର୍କଟ', 'ସିଂହ', 'କନ୍ୟା', 'ତୁଳା', 'ବୃଶ୍ଚିକ', 'ଧନୁ', 'ମକର', 'କୁମ୍ଭ', 'ମୀନ',
  ];

  private readonly saturnGlyph = '<svg style="display:block;width:1em;height:1em;overflow:visible" viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg"><ellipse cx="32" cy="36" rx="29" ry="8.5" fill="none" stroke="currentColor" stroke-width="2.2" transform="rotate(-12 32 36)"/><circle cx="32" cy="28" r="13" fill="none" stroke="currentColor" stroke-width="2.2"/></svg>';

  constructor(private readonly elementRef: ElementRef<HTMLElement>) {}

  ngAfterViewInit(): void {
    this.scatter('deva-hero', 10);
    this.scatter('deva-process', 6);
    this.scatter('deva-eval', 8);
    this.scatter('deva-preview', 7);
    this.scatter('deva-milan', 7);
    this.scatter('deva-philosophy', 5);
    this.scatter('deva-cta', 7);
    this.scatter('deva-footer', 4);
    this.scatterStars();
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
      span.style.fontFamily = "'Noto Sans Devanagari', 'Noto Sans Kannada', 'Noto Sans Oriya', 'Cormorant Garamond', serif";
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
