import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KundaliScoreCard } from './kundali-score-card';

describe('KundaliScoreCard', () => {
  let component: KundaliScoreCard;
  let fixture: ComponentFixture<KundaliScoreCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KundaliScoreCard],
    }).compileComponents();

    fixture = TestBed.createComponent(KundaliScoreCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
