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
        {
          name: 'Dhan Yoga',
          category: 'Dhana Yoga',
          strength: 'Strong',
          involvedPlanets: ['Venus', 'Jupiter'],
          involvedHouses: [2, 11],
          impactAreas: [
            {
              category: 'Financial',
              statements: ['Prosperity may improve through discipline.']
            }
          ]
        },
        { name: 'Raj Yoga', category: 'Raja Yoga', strength: 'Moderate' }
      ]
    });

    expect(yogas).toEqual([
      {
        name: 'Dhan Yoga',
        category: 'Dhana Yoga',
        strength: 'Strong',
        reason: undefined,
        sanskritName: undefined,
        description: undefined,
        impact: undefined,
        involvedPlanets: ['Venus', 'Jupiter'],
        involvedHouses: [2, 11],
        impactAreas: [
          {
            category: 'Financial',
            statements: ['Prosperity may improve through discipline.']
          }
        ]
      },
      {
        name: 'Raj Yoga',
        category: 'Raja Yoga',
        strength: 'Moderate',
        reason: undefined,
        sanskritName: undefined,
        description: undefined,
        impact: undefined,
        involvedPlanets: [],
        involvedHouses: [],
        impactAreas: []
      }
    ]);
  });

  it('extracts yogas from a summary payload string', () => {
    const yogas = component.extractYogas({ summary: { yoga: 'Gajakesari Yoga' } });

    expect(yogas).toEqual([{ name: 'Gajakesari Yoga' }]);
  });

  it('extracts doshas from an array response payload', () => {
    const doshas = component.extractDoshas({
      doshas: [
        {
          name: 'Manglik Dosha',
          category: 'Relationship & Marriage',
          severity: 'Medium',
          reason: 'Mars is placed in house 7.',
          involvedPlanets: ['Mars'],
          involvedHouses: [7],
          impactAreas: [
            {
              category: 'Relationships',
              statements: ['Partnerships may need patience.']
            }
          ],
          remedies: ['Worship Lord Hanuman on Tuesdays.']
        }
      ]
    });

    expect(doshas).toEqual([
      {
        name: 'Manglik Dosha',
        category: 'Relationship & Marriage',
        severity: 'Medium',
        reason: 'Mars is placed in house 7.',
        description: undefined,
        involvedPlanets: ['Mars'],
        involvedHouses: [7],
        impactAreas: [
          {
            category: 'Relationships',
            statements: ['Partnerships may need patience.']
          }
        ],
        remedies: ['Worship Lord Hanuman on Tuesdays.']
      }
    ]);
  });

  it('selects the first available house as the default selection', () => {
    component.kundaliResponse = {
      houses: [{ houseNumber: 1, title: 'House 1' }, { houseNumber: 2, title: 'House 2' }]
    };

    component.setDefaultHouseSelection();

    expect(component.selectedHouseData).toEqual({ houseNumber: 1, title: 'House 1' });
  });
});
