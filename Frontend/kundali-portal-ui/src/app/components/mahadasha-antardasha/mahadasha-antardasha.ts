import { Component, Input } from '@angular/core';
import { CommonModule} from "@angular/common";
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
@Component({
  selector: 'app-mahadasha-antardasha',
  standalone: true,
  imports: [CommonModule, TranslocoPipe],
  templateUrl: './mahadasha-antardasha.html',
  styleUrls: ['./mahadasha-antardasha.scss'],
})
export class MahadashaAntardasha {

  constructor(private translocoService: TranslocoService) {}

  @Input() mahadashas: any[] = [];

  isCurrentPeriod(start: string, end: string): boolean {

  const today = new Date();

  return today >= new Date(start)
      && today <= new Date(end);
}
}
