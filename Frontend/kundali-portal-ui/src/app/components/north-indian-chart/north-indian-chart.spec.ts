import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NorthIndianChartComponent } from './north-indian-chart.component';

describe('NorthIndianChartComponent', () => {
  let component: NorthIndianChartComponent;
  let fixture: ComponentFixture<NorthIndianChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NorthIndianChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(NorthIndianChartComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
