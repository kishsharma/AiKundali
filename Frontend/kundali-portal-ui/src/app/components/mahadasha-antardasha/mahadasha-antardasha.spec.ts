import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MahadashaAntardasha } from './mahadasha-antardasha';

describe('MahadashaAntardasha', () => {
  let component: MahadashaAntardasha;
  let fixture: ComponentFixture<MahadashaAntardasha>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MahadashaAntardasha],
    }).compileComponents();

    fixture = TestBed.createComponent(MahadashaAntardasha);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
