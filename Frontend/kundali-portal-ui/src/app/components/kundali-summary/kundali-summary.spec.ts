import { ComponentFixture, TestBed } from '@angular/core/testing';

import { KundaliSummary } from './kundali-summary';

describe('KundaliSummary', () => {
  let component: KundaliSummary;
  let fixture: ComponentFixture<KundaliSummary>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KundaliSummary],
    }).compileComponents();

    fixture = TestBed.createComponent(KundaliSummary);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
