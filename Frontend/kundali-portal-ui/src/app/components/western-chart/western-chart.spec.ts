import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WesternChartComponent } from './western-chart.component';

describe('WesternChartComponent', () => {
  let component: WesternChartComponent;
  let fixture: ComponentFixture<WesternChartComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WesternChartComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(WesternChartComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should rotate houses from the ascendant sign', () => {
    component.ascendant = '7 - Libra';
    component.ngOnChanges({
      ascendant: {
        currentValue: component.ascendant,
        previousValue: '',
        firstChange: true,
        isFirstChange: () => true
      }
    });

    expect(component.getHouseForSign(7)).toBe(1);
    expect(component.getHouseForSign(8)).toBe(2);
    expect(component.getHouseForSign(6)).toBe(12);
  });

  it('places gochar planets on ecliptic longitude while keeping natal houses', () => {
    component.ascendant = '7 - Libra';
    component.placeByDegree = true;
    component.planets = [
      { planet: 'Sun', sign: 'Aries', longitude: 15 }
    ];
    component.ngOnChanges({
      ascendant: {
        currentValue: component.ascendant,
        previousValue: '',
        firstChange: true,
        isFirstChange: () => true
      },
      placeByDegree: {
        currentValue: true,
        previousValue: false,
        firstChange: true,
        isFirstChange: () => true
      },
      planets: {
        currentValue: component.planets,
        previousValue: [],
        firstChange: true,
        isFirstChange: () => true
      }
    });

    expect(component.getPlanetAbsoluteLongitude(component.planets[0])).toBe(15);
    expect(component.placedPlanets.length).toBe(1);
    expect(component.placedPlanets[0].angle).toBe(-75);
    expect(component.getHouseForSign(7)).toBe(1);
    expect(component.getHouseForSign(1)).toBe(7);
  });
});
