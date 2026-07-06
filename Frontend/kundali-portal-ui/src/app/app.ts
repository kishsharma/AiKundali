import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TranslocoPipe],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})

export class App {
  activeLanguage: string;

  constructor(private translocoService: TranslocoService) {
    this.activeLanguage = this.translocoService.getActiveLang();
  }

  changeLanguage(lang: string) {
    this.translocoService.setActiveLang(lang);
    this.activeLanguage = lang;
  }
}
