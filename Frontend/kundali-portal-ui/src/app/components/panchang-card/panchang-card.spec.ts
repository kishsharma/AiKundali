import { ComponentFixture, TestBed } from '@angular/core/testing';

import { PanchangCardComponent } from './panchang-card.component';

describe('PanchangCard', () => {
  let component: PanchangCardComponent;
  let fixture: ComponentFixture<PanchangCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PanchangCardComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PanchangCardComponent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
