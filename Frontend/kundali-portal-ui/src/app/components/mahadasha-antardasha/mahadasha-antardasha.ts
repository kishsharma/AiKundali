import { Component, Input } from '@angular/core';
import { CommonModule} from "@angular/common";
@Component({
  selector: 'app-mahadasha-antardasha',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mahadasha-antardasha.html',
  styleUrls: ['./mahadasha-antardasha.scss'],
})
export class MahadashaAntardasha {

  @Input() mahadashas: any[] = [];
}
