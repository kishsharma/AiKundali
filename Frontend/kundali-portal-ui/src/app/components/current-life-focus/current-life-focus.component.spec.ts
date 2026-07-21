import { CommonModule } from '@angular/common';
import { Component, Input, Pipe, PipeTransform, SimpleChange } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Subject, of } from 'rxjs';
import { vi } from 'vitest';
import { TranslocoPipe } from '@jsverse/transloco';

import { KundaliService } from '../../modules/kundali/kundali.service';
import { SectionCardComponent } from '../../shared/components/section-card.component';
import { CurrentLifeFocusComponent } from './current-life-focus.component';
import { CurrentLifeFocusResponse, FocusedLifeAreaResponse } from './current-life-focus.models';
import { CurrentLifeFocusService } from './current-life-focus.service';

@Pipe({ name: 'transloco', standalone: true })
class MockTranslocoPipe implements PipeTransform {
  transform(value: string): string {
    return value;
  }
}

@Component({
  selector: 'app-section-card',
  standalone: true,
  imports: [CommonModule],
  template: '<section><h3 *ngIf="title">{{ title }}</h3><ng-content></ng-content></section>'
})
class MockSectionCardComponent {
  @Input() title = '';
  @Input() interactive = false;
  @Input() highlight = false;
  @Input() footer = false;
  @Input() headerAction = false;
}

describe('CurrentLifeFocusComponent', () => {
  let component: CurrentLifeFocusComponent;
  let fixture: ComponentFixture<CurrentLifeFocusComponent>;
  let currentLifeFocusService: any;
  let kundaliService: any;

  const topResponse: CurrentLifeFocusResponse = {
    analysisId: 'analysis-1',
    calculatedAtUtc: '2026-07-15T00:00:00Z',
    currentMahadasha: 'Saturn',
    currentAntardasha: 'Mars',
    prompt: 'Which area best matches what you are currently experiencing?',
    areas: [
      {
        key: 'career_business',
        title: 'Career and business',
        activationLevel: 'High',
        tone: 'Pressured',
        activationScore: 82,
        activatedHouses: [10, 6, 11],
        shortReasons: ['Tenth house is strongly activated', 'Antardasha is connected with profession']
      },
      {
        key: 'finance',
        title: 'Finance',
        activationLevel: 'Medium',
        tone: 'Mixed',
        score: 61,
        activatedHouses: [2, 11],
        primaryFactors: ['Second house is active']
      },
      {
        key: 'mental_peace_sleep',
        title: 'Mental peace and sleep',
        activationLevel: 'Medium',
        experienceTone: 'Supportive',
        score: 54,
        activatedHouses: [4, 12],
        primaryFactors: ['Moon-linked houses are active']
      }
    ]
  };

  const focusedResponse: FocusedLifeAreaResponse = {
    areaKey: 'career_business',
    title: 'Career and business',
    activationLevel: 'High',
    tone: 'Mixed',
    summary: 'Career is active through Dasha and transit links.',
    narrative: {
      verdict: 'Career is showing high activation with a mixed experience tone.',
      currentSituation: 'Work matters can feel more visible right now.',
      astrologicalReason: 'House 10 and active Dasha are shaping the current professional focus.',
      futureChange: 'Results may improve with steady effort.',
      importantPeriods: 'This reading is anchored to July 2026.',
      expectedOutcome: 'The outcome is mixed but workable.',
      evidence: ['Active Dasha context: Saturn-Mars.', 'House 10 is active.']
    },
    possibleExperiences: ['Workload may feel more visible.'],
    supportiveFactors: ['Jupiter supports gains.'],
    pressureFactors: ['Saturn asks for discipline.'],
    activatedHouses: [{ house: 10, role: 'Primary', activationScore: 82 }, { house: 6, role: 'Secondary', activationScore: 64 }],
    followUpQuestion: 'Which career concern is closest?',
    followUpOptions: [{ key: 'job_change', label: 'Considering a job change' }]
  };

  beforeEach(async () => {
    currentLifeFocusService = {
      getCurrentLifeFocus: vi.fn(),
      getFocusedLifeAreaAnalysis: vi.fn()
    };
    kundaliService = { saveKundali: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [CurrentLifeFocusComponent],
      providers: [
        { provide: CurrentLifeFocusService, useValue: currentLifeFocusService },
        { provide: KundaliService, useValue: kundaliService }
      ]
    })
      .overrideComponent(CurrentLifeFocusComponent, {
        remove: { imports: [SectionCardComponent, TranslocoPipe] },
        add: { imports: [MockSectionCardComponent, MockTranslocoPipe] }
      })
      .compileComponents();

    fixture = TestBed.createComponent(CurrentLifeFocusComponent);
    component = fixture.componentInstance;
    component.kundaliId = 'kundali-1';
    component.savePayload = { name: 'Test Native', chartData: {} };
  });

  it('renders the top three cards with activation level and tone separately', () => {
    currentLifeFocusService.getCurrentLifeFocus.mockReturnValue(of(topResponse));

    component.analyse();
    fixture.detectChanges();

    const cards = fixture.debugElement.queryAll(By.css('.area-card'));
    const activationPills = fixture.debugElement.queryAll(By.css('.level-pill'));
    const tonePills = fixture.debugElement.queryAll(By.css('.tone-pill'));

    expect(cards.length).toBe(3);
    expect(cards[0].nativeElement.textContent).toContain('Career and business');
    expect(activationPills[0].nativeElement.textContent).toContain('High');
    expect(tonePills[0].nativeElement.textContent).toContain('Pressured');
  });

  it('calls the focused endpoint with the current analysis id and selected area key', () => {
    currentLifeFocusService.getCurrentLifeFocus.mockReturnValue(of(topResponse));
    currentLifeFocusService.getFocusedLifeAreaAnalysis.mockReturnValue(of(focusedResponse));

    component.analyse();
    fixture.detectChanges();
    fixture.debugElement.query(By.css('.select-area-action')).nativeElement.click();
    fixture.detectChanges();

    expect(currentLifeFocusService.getFocusedLifeAreaAnalysis).toHaveBeenCalledWith('analysis-1', {
      selectedAreaKey: 'career_business'
    });
    expect(fixture.nativeElement.textContent).toContain('Career is showing high activation with a mixed experience tone.');
  });

  it('prevents duplicate analyse requests while loading', () => {
    const pending = new Subject<CurrentLifeFocusResponse>();
    currentLifeFocusService.getCurrentLifeFocus.mockReturnValue(pending.asObservable());

    component.analyse();
    component.analyse();

    expect(currentLifeFocusService.getCurrentLifeFocus).toHaveBeenCalledTimes(1);
    pending.next(topResponse);
    pending.complete();
  });

  it('shows the none-of-these state', () => {
    currentLifeFocusService.getCurrentLifeFocus.mockReturnValue(of(topResponse));

    component.analyse();
    fixture.detectChanges();
    component.selectNone();
    fixture.detectChanges();

    expect(component.state).toBe('noneSelected');
    expect(component.focusedResponse).toBeNull();
  });

  it('clears stale results when the reset key changes', () => {
    currentLifeFocusService.getCurrentLifeFocus.mockReturnValue(of(topResponse));

    component.analyse();
    fixture.detectChanges();
    component.ngOnChanges({ resetKey: new SimpleChange(0, 1, false) });
    fixture.detectChanges();

    expect(component.state).toBe('initial');
    expect(component.topResponse).toBeNull();
  });
});









