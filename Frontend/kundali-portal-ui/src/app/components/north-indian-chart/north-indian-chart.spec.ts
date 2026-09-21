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

  it('formats planet degrees to one decimal', () => {
    expect(component.getPlanetDegreeLabel({ degree: 12, minute: 30 })).toBe('12.5');
    expect(component.getPlanetDegreeLabel({ degreeInSign: 8.16 })).toBe('8.2');
    expect(component.getPlanetDegreeLabel({ longitude: 45.2 })).toBe('15.2');
  });
});
