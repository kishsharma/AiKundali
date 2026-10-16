import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { KundaliYogaDoshaPanelBase } from '../kundali-yoga-dosha-shared/kundali-yoga-dosha-panel.base';

@Component({
  selector: 'app-kundali-yogas-panel',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './kundali-yogas-panel.component.html',
  styleUrls: ['../kundali-yoga-dosha-shared/kundali-yoga-dosha-panel.scss']
})
export class KundaliYogasPanelComponent extends KundaliYogaDoshaPanelBase {
  protected readonly kind = 'yoga' as const;
  protected readonly currentTitle = 'Current Yoga Activation';
  protected readonly upcomingTitle = 'Upcoming Yoga Activation';
}
