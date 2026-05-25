import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HouseDetailsCard } from './house-details-card';

describe('HouseDetailsCard', () => {
  let component: HouseDetailsCard;
  let fixture: ComponentFixture<HouseDetailsCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HouseDetailsCard],
    }).compileComponents();

    fixture = TestBed.createComponent(HouseDetailsCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
