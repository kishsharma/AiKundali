import { ChangeDetectorRef } from '@angular/core';
import { KundaliComponent } from './kundali.component';

describe('KundaliComponent', () => {
  let component: KundaliComponent;

  beforeEach(() => {
    component = new KundaliComponent({} as any, {} as ChangeDetectorRef);
  });

  it('extracts yogas from an array response payload', () => {
    const yogas = component.extractYogas({
      yogas: [
        { name: 'Dhan Yoga', category: 'Dhana Yoga', strength: 'Strong' },
        { name: 'Raj Yoga', category: 'Raja Yoga', strength: 'Moderate' }
      ]
    });

    expect(yogas).toEqual([
      { name: 'Dhan Yoga', category: 'Dhana Yoga', strength: 'Strong' },
      { name: 'Raj Yoga', category: 'Raja Yoga', strength: 'Moderate' }
    ]);
  });

  it('extracts yogas from a summary payload string', () => {
    const yogas = component.extractYogas({ summary: { yoga: 'Gajakesari Yoga' } });

    expect(yogas).toEqual([{ name: 'Gajakesari Yoga' }]);
  });

  it('selects the first available house as the default selection', () => {
    component.kundaliResponse = {
      houses: [{ houseNumber: 1, title: 'House 1' }, { houseNumber: 2, title: 'House 2' }]
    };

    component.setDefaultHouseSelection();

    expect(component.selectedHouseData).toEqual({ houseNumber: 1, title: 'House 1' });
  });
});
