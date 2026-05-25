import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KundaliService } from './kundali.service';
import { PageHeaderComponent } from "../../shared/components/page-header.component";
import { SectionCardComponent } from "../../shared/components/section-card.component";
import { of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, tap } from 'rxjs/operators';
import { LocationService } from '../../core/services/location.service';
import { DashaApiService } from '../../core/services/dasha-api.service';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';
import { SouthIndianChartComponent } from '../../components/south-indian-chart/south-indian-chart.component';
import { WesternChartComponent } from '../../components/western-chart/western-chart.component';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { form } from '@angular/forms/signals';
import { HouseDetailsCard } from "../../components/house-details-card/house-details-card";
import { KundaliSummary } from "../../components/kundali-summary/kundali-summary";
import { PanchangCardComponent } from "../../components/panchang-card/panchang-card.component";
import { KundaliScoreCard } from "../../components/kundali-score-card/kundali-score-card";
import { MahadashaAntardasha } from "../../components/mahadasha-antardasha/mahadasha-antardasha";

@Component({
  selector: 'app-kundali',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    PageHeaderComponent,
    SectionCardComponent,
    NorthIndianChartComponent,
    SouthIndianChartComponent,
    WesternChartComponent,
    HouseDetailsCard,
    KundaliSummary,
    PanchangCardComponent,
    KundaliScoreCard,
    MahadashaAntardasha
],
  templateUrl: './kundali.component.html',
  styleUrls: ['./kundali.component.scss']
})
export class KundaliComponent {

  private fb = inject(FormBuilder);
  private kundaliService = inject(KundaliService);
  private dashaApiService = inject(DashaApiService);
  private locationService = inject(LocationService);
  locationResults: any[] = [];
  kundaliResponse: any = null;
  loading = false;
  showChart = false;
  mahadashaPeriods: any[] = [];
  planets: any[] = [];
  summary: any;
  scores: any[] = [];
  selectedHouseData: any;
  activeChartType: 'north' | 'south' | 'western' = 'north';

  ngOnInit(): void {
    this.kundaliForm.controls.birthPlace.valueChanges
      .pipe(
        debounceTime(500),

        tap(value => {
          console.log('Typing:', value);
        }),

        distinctUntilChanged(),

        switchMap((value: string | null) => {

          if (!value || value.length < 3) {
            return of([]);
          }

          return this.locationService.searchLocation(value);
        })
      )
      .subscribe(results => {

        console.log(results);

        this.locationResults = results;
      });
  }
  selectedCoordinates: any = null;

  selectLocation(place: any): void {

    this.kundaliForm.patchValue({
      birthPlace: place.display_name,
      latitude: place.lat,
      longitude: place.lon
    });

    this.selectedCoordinates = {
      latitude: place.lat,
      longitude: place.lon
    };

    this.locationResults = [];

    console.log(this.selectedCoordinates);
  }
  kundaliForm = this.fb.group({
    fullName: ['', Validators.required],
    gender: ['', Validators.required],
    birthDate: ['', Validators.required],
    birthTime: ['', Validators.required],
    birthPlace: ['', Validators.required],
    latitude: [''],
    longitude: ['']
  });

  generateKundali(): void {
    this.showChart = false;
    if (this.kundaliForm.invalid) {
      this.kundaliForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    const payload = {
      name: this.kundaliForm.value.fullName,
      gender: this.kundaliForm.value.gender,
      birthDateTime: `${this.kundaliForm.value.birthDate}T${this.kundaliForm.value.birthTime}:00`,
      birthplace: this.kundaliForm.value.birthPlace,
      latitude: this.kundaliForm.value.latitude,
      longitude: this.kundaliForm.value.longitude,
      timeZoneOffset: 5.30
    };
    this.kundaliService.generateKundali(payload)
      .subscribe({
        next: (response) => {
          console.log('Generated:', response);
          this.kundaliResponse = response;
          console.log(response);
console.log('scoreslog'  , this.kundaliResponse?.scores);
          this.planets = response.planets;
          this.summary = response.summary;
          this.scores = response.scores;
          console.log(this.planets);
          this.showChart = true;

          this.dashaApiService.getMahadashaPeriods(payload).subscribe({
            next: (mahaResponse) => {
              console.log('Mahadasha periods:', mahaResponse);
              this.mahadashaPeriods = mahaResponse;
              console.log('Mahadasha periods:', this.mahadashaPeriods);
            },
            error: (error) => {
              console.error('Mahadasha periods error:', error);
            }
          });

         setTimeout(() => {
  this.loading = false;
});
        },
        error: (error) => {
          console.error(error);
          setTimeout(() => {
  this.loading = false;
});
        }
      });
  }


}
