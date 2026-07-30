import '@angular/compiler';
import { ChangeDetectorRef, createEnvironmentInjector, EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiTermTranslationService } from '../../core/services/api-term-translation.service';
import { AstrologerBrandingSettingsService } from '../../core/services/astrologer-branding-settings.service';
import { DashaApiService } from '../../core/services/dasha-api.service';
import { LatestKundaliStorageService } from '../../core/services/latest-kundali-storage.service';
import { LocationService } from '../../core/services/location.service';
import { DhruvaKundaliContextService } from '../../shared/components/dhruva-assistant/dhruva-kundali-context.service';
import { KundaliComponent } from './kundali.component';
import { KundaliService } from './kundali.service';

describe('KundaliComponent', () => {
  let component: KundaliComponent;

  beforeEach(() => {
    vi.useRealTimers();
    const injector: EnvironmentInjector = createEnvironmentInjector([
      FormBuilder,
      { provide: KundaliService, useValue: {} },
      { provide: DashaApiService, useValue: {} },
      { provide: LocationService, useValue: { searchLocation: () => of([]) } },
      { provide: LatestKundaliStorageService, useValue: { save: vi.fn() } },
      { provide: DhruvaKundaliContextService, useValue: { setContext: vi.fn(), clearContext: vi.fn() } },
      { provide: ApiTermTranslationService, useValue: { translateTerm: (_type: string, value: string) => value } },
      { provide: AstrologerBrandingSettingsService, useValue: { getSettings: () => of(null) } },
      { provide: ActivatedRoute, useValue: { paramMap: of({ get: () => null }) } },
    ], null as any);

    component = runInInjectionContext(injector, () =>
      new KundaliComponent({ translate: (key: string) => key } as any, { detectChanges: () => undefined } as ChangeDetectorRef)
    );
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
        activationPeriods: { currentlyActive: [], upcoming: [], past: [] },
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
        activationPeriods: { currentlyActive: [], upcoming: [], past: [] },
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
          remedies: ['Worship Lord Hanuman on Tuesdays.'],
        activationPeriods: { currentlyActive: [], upcoming: [], past: [] }
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
        remedies: ['Worship Lord Hanuman on Tuesdays.'],
        activationPeriods: { currentlyActive: [], upcoming: [], past: [] }
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
  it('prepares Janma Story collapsed without starting presentation', () => {
    vi.useFakeTimers();
    component.kundaliForm.patchValue({ fullName: 'Arya Sharma' });

    (component as any).prepareJanmaStory(buildStoryResponse());

    expect(component.isJanmaStoryRailCollapsed).toBe(true);
    expect(component.janmaStoryLines.length).toBeGreaterThan(0);
    expect(component.displayedJanmaStoryLines).toEqual(component.janmaStoryLines.map(() => ''));
    expect((component as any).janmaStoryTimers).toEqual([]);
  });

  it('starts Janma Story only when the collapsed rail is opened', () => {
    vi.useFakeTimers();
    component.kundaliResponse = buildStoryResponse();
    (component as any).prepareJanmaStory(component.kundaliResponse);

    component.toggleJanmaStoryRail();

    expect(component.isJanmaStoryRailCollapsed).toBe(false);
    expect((component as any).janmaStoryTimers.length).toBe(1);

    vi.advanceTimersByTime(260);
    expect(component.displayedJanmaStoryLines[0].length).toBeGreaterThan(0);
  });

  it('resets active Janma Story presentation when closed and restarts on reopen', () => {
    vi.useFakeTimers();
    component.kundaliResponse = buildStoryResponse();
    (component as any).prepareJanmaStory(component.kundaliResponse);

    component.toggleJanmaStoryRail();
    vi.advanceTimersByTime(300);
    expect((component as any).janmaStoryTimers.length).toBeGreaterThan(0);

    component.toggleJanmaStoryRail();

    expect(component.isJanmaStoryRailCollapsed).toBe(true);
    expect(component.displayedJanmaStoryLines).toEqual(component.janmaStoryLines.map(() => ''));
    expect((component as any).janmaStoryTimers).toEqual([]);

    component.toggleJanmaStoryRail();

    expect(component.isJanmaStoryRailCollapsed).toBe(false);
    expect((component as any).janmaStoryTimers.length).toBe(1);
  });
});
function buildStoryResponse() {
  return {
    summary: {
      ascendant: 'Taurus',
      rashi: 'Cancer',
      nakshatra: 'Rohini',
      pada: '2',
      akshar: 'Va',
    },
    houses: [{ houseNumber: 1, score: 72, title: 'Self' }],
    scores: [{ houseNumber: 1, score: 72 }],
    planets: [
      { name: 'Venus', functionalNature: 'Benefic' },
      { name: 'Saturn', functionalNature: 'Malefic' },
    ],
  };
}