import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HouseScoreOverview } from './house-score-overview';

describe('HouseScoreOverview', () => {
  let component: HouseScoreOverview;
  let fixture: ComponentFixture<HouseScoreOverview>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HouseScoreOverview],
    }).compileComponents();

    fixture = TestBed.createComponent(HouseScoreOverview);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
