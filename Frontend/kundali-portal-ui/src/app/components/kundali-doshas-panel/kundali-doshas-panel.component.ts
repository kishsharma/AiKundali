import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { AstrologyIconComponent } from '../../shared/components/astrology-icon.component';
import { KundaliYogaDoshaPanelBase } from '../kundali-yoga-dosha-shared/kundali-yoga-dosha-panel.base';

@Component({
  selector: 'app-kundali-doshas-panel',
  standalone: true,
  imports: [CommonModule, TranslocoPipe, AstrologyIconComponent],
  templateUrl: './kundali-doshas-panel.component.html',
  styleUrls: ['../kundali-yoga-dosha-shared/kundali-yoga-dosha-panel.scss']
})
export class KundaliDoshasPanelComponent extends KundaliYogaDoshaPanelBase {
  protected readonly kind = 'dosha' as const;
  protected readonly currentTitle = 'Current Dosha Activation';
  protected readonly upcomingTitle = 'Upcoming Dosha Activation';
}
