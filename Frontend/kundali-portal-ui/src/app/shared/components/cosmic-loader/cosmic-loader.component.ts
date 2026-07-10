import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslocoPipe } from '@jsverse/transloco';


@Component({
  selector: 'app-cosmic-loader',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],

  templateUrl: './cosmic-loader.component.html',
  styleUrls: ['./cosmic-loader.component.scss']
})

export class CosmicLoaderComponent {
@Input() showLoader: boolean = false;
@Input() message: string = 'loader.loading';

activeStepIndex = 1;
progress = 35;
steps = [
  {
    titleKey: 'loader.stepPlanetPositionsTitle',
    descriptionKey: 'loader.stepPlanetPositionsText',
    completed: true
  },
  {
    titleKey: 'loader.stepBhavasTitle',
    descriptionKey: 'loader.stepBhavasText',
    completed: false
  },
  {
    titleKey: 'loader.stepYogasDoshasTitle',
    descriptionKey: 'loader.stepYogasDoshasText',
    completed: false
  },
  {
    titleKey: 'loader.stepStrengthsTitle',
    descriptionKey: 'loader.stepStrengthsText',
    completed: false
  },
  {
    titleKey: 'loader.stepDashaTitle',
    descriptionKey: 'loader.stepDashaText',
    completed: false
  },
  {
    titleKey: 'loader.stepReportTitle',
    descriptionKey: 'loader.stepReportText',
    completed: false
  }
];
}
