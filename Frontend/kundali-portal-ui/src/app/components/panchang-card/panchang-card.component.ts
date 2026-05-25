import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
@Component({
  selector: 'app-panchang-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './panchang-card.component.html',
  styleUrls: ['./panchang-card.component.scss'],
})
export class PanchangCardComponent {
  @Input() panchang: any;
}
