import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SouthIndianChartComponent } from './south-indian-chart.component';

describe('SouthIndianChartComponent', () => {
  let component: SouthIndianChartComponent;
  let fixture: ComponentFixture<SouthIndianChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SouthIndianChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(SouthIndianChartComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should rotate houses from the ascendant sign', () => {
    component.ascendant = '4 - Cancer';
    component.ngOnChanges({
      ascendant: {
        currentValue: component.ascendant,
        previousValue: '',
        firstChange: true,
        isFirstChange: () => true
      }
    });

    expect(component.getHouseForSign(4)).toBe(1);
    expect(component.getHouseForSign(5)).toBe(2);
    expect(component.getHouseForSign(3)).toBe(12);
  });
});
