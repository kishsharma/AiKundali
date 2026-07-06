import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/components/page-header.component';
import { NorthIndianChartComponent } from '../../components/north-indian-chart/north-indian-chart.component';
import {CompatibilityApiService,MatchRequestPerson} from '../../core/services/compatibility-api.service';
import { BirthChart, CompatibilityResponse } from '../../shared/models/compatibility.model';

interface PersonSummary {
  role: 'Boy' | 'Girl';
  name: string;
  gender: string;
  moonSign: string;
  nakshatra: string;
  ascendant: string;
  birthDate: string;
  birthTime: string;
  birthPlace: string;
}

@Component({
  selector: 'app-compatibility',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, PageHeaderComponent, NorthIndianChartComponent],
  templateUrl: './compatibility.component.html',
  styleUrls: ['./compatibility.component.scss'],
})
export class CompatibilityComponent {
  private fb = inject(FormBuilder);
  private compatibilityApi = inject(CompatibilityApiService);

  loading = false;
  showReport=false;
  errorMessage = '';
  result: CompatibilityResponse | null = null;
  personAhouses: any = null;
  personBhouses: any = null;
  personAplanetPositions: any = null;
  personBplanetPositions: any = null;
  personAchart: BirthChart | null = null;
  personBchart: BirthChart | null = null;
  //result: any | null = null;
  readonly form = this.fb.group({
    boyName: ['Kishor Sharma', Validators.required],
    boyBirthDate: ['1986-10-06', Validators.required],
    boyBirthTime: ['11:45', Validators.required],
    boyBirthPlace: ['Hingoli, Maharashtra, India', Validators.required],
    boyLatitude: [19.5431163, Validators.required],
    boyLongitude: [77.1739432, Validators.required],
    girlName: ['Pooja Vyas', Validators.required],
    girlBirthDate: ['1991-12-01', Validators.required],
    girlBirthTime: ['02:15', Validators.required],
    girlBirthPlace: ['Aurangabad, Maharashtra, India', Validators.required],
    girlLatitude: [19.8762, Validators.required],
    girlLongitude: [75.3433, Validators.required],
  });

  readonly kutaScores = [
    { name: 'Varna', score: 1, max: 1, grade: 'Excellent', tone: 'orange' },
    { name: 'Vasya', score: 2, max: 2, grade: 'Excellent', tone: 'blue' },
    { name: 'Tara', score: 2.5, max: 3, grade: 'Good', tone: 'purple' },
    { name: 'Yoni', score: 3, max: 4, grade: 'Good', tone: 'pink' },
    { name: 'Graha Maitri', score: 5, max: 5, grade: 'Excellent', tone: 'green' },
    { name: 'Gana', score: 5, max: 6, grade: 'Good', tone: 'orange' },
    { name: 'Bhakoot', score: 6, max: 7, grade: 'Good', tone: 'blue' },
    { name: 'Nadi', score: 8, max: 8, grade: 'Excellent', tone: 'purple' },
  ];

  readonly doshas = [
    { name: 'Manglik Dosha', detail: 'Both are Manglik', status: 'Neutral', tone: 'orange' },
    { name: 'Nadi Dosha', detail: 'Different Nadis', status: 'No Dosha', tone: 'green' },
    { name: 'Bhakoot Dosha', detail: 'Compatible', status: 'No Dosha', tone: 'blue' },
    { name: 'Gana Compatibility', detail: 'Deva - Manushya', status: 'Good', tone: 'pink' },
    { name: 'Yoni Compatibility', detail: 'Compatible', status: 'Good', tone: 'purple' },
  ];

  readonly marriagePeriods = [
    { period: 'May 2025 - Aug 2025', grade: 'Very Auspicious', tone: 'green' },
    { period: 'Jan 2026 - Apr 2026', grade: 'Auspicious', tone: 'blue' },
    { period: 'Sep 2026 - Dec 2026', grade: 'Good', tone: 'purple' },
    { period: 'Mar 2027 - Jun 2027', grade: 'Auspicious', tone: 'pink' },
  ];

  get boy(): PersonSummary {
    return {
      role: 'Boy',
      name: this.form.value.boyName || 'Rahul Sharma',
      gender: 'Male',
      moonSign: 'Leo (सिंह)',
      nakshatra: 'Magha',
      ascendant: 'Aries (मेष)',
      birthDate: this.formatDate(this.form.value.boyBirthDate),
      birthTime: this.form.value.boyBirthTime || '11:45',
      birthPlace: this.form.value.boyBirthPlace || 'Hingoli, Maharashtra, India',
    };
  }

  get girl(): PersonSummary {
    return {
      role: 'Girl',
      name: this.form.value.girlName || 'Priya Verma',
      gender: 'Female',
      moonSign: 'Taurus (वृषभ)',
      nakshatra: 'Rohini',
      ascendant: 'Cancer (कर्क)',
      birthDate: this.formatDate(this.form.value.girlBirthDate),
      birthTime: this.form.value.girlBirthTime || '09:20',
      birthPlace: this.form.value.girlBirthPlace || 'Aurangabad, Maharashtra, India',
    };
  }

  get overallScore(): number {
    return this.result?.overallCompatibilityScore ?? 78;
  }

  get scoreLabel(): string {
    if (this.overallScore >= 75) return 'Excellent Match';
    if (this.overallScore >= 60) return 'Good Match';
    if (this.overallScore >= 45) return 'Moderate Match';
    return 'Needs Careful Review';
  }

  get totalKutaScore(): number {
    return this.kutaScores.reduce((total, item) => total + item.score, 0);
  }

  get compatibilityAspects() {
    const fallback = [
      { name: 'Emotional Compatibility', score: 82, grade: 'Very Strong', tone: 'green' },
      { name: 'Communication', score: 75, grade: 'Strong', tone: 'blue' },
      { name: 'Financial Harmony', score: 70, grade: 'Good', tone: 'purple' },
      { name: 'Physical Compatibility', score: 80, grade: 'Very Strong', tone: 'pink' },
      { name: 'Spiritual Alignment', score: 85, grade: 'Excellent', tone: 'orange' },
      { name: 'Family Harmony', score: 78, grade: 'Strong', tone: 'blue' },
    ];

    if (!this.result?.dimensions?.length) {
      return fallback;
    }

    return this.result.dimensions.map((dimension, index) => ({
      name: dimension.name,
      score: dimension.score,
      grade: this.getGrade(dimension.score),
      tone: fallback[index % fallback.length].tone,
    }));
  }

  get insights() {
    const strengths = this.result?.dominantStrengths?.length
      ? this.result.dominantStrengths
      : ['Strong emotional bond, mutual understanding and support in difficult times.'];
    const challenges = this.result?.dominantChallenges?.length
      ? this.result.dominantChallenges
      : ['Ego clashes can occur at times. Patience and communication are key.'];

    return [
      { title: 'Relationship Strengths', text: strengths[0], tone: 'green' },
      { title: 'Potential Challenges', text: challenges[0], tone: 'orange' },
      { title: 'Marriage Stability', text: 'High stability indicated. Long-lasting and harmonious married life.', tone: 'blue' },
      { title: 'Career Support', text: 'Both will support each other career growth and financial success.', tone: 'purple' },
      { title: 'Emotional Bonding', text: this.result?.summary ?? 'Deep emotional connection and strong attraction present.', tone: 'pink' },
      { title: 'Family Life', text: 'Good chances of a peaceful and happy family life.', tone: 'orange' },
    ];
  }

  analyzeCompatibility(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.compatibilityApi.match({
      personA: this.toPersonRequest('boy'),
      personB: this.toPersonRequest('girl'),
    }).subscribe({
      next: (result) => {
        this.result = result;
        this.showReport = true;
        this.personAhouses = result?.personAchart?.houses || null;
        // this.personAhouses = result?.personAchart?.houses?.map(h => ({
        //   ...h, )) || [];
        this.personBhouses = result?.personBchart?.houses || null;
        this.personAplanetPositions = result?.personAchart?.planetPositions || null;
        this.personBplanetPositions = result?.personBchart?.planetPositions || null;
        this.personAchart = result?.personAchart || null;  
        this.personBchart = result?.personBchart || null;
        this.loading = false;
        console.log('Compatibility Result:', result);
        console.log('Person A Houses:', this.personAhouses);
        console.log('Person B Houses:', this.personBhouses);
        console.log('Person A Planet Positions:', this.personAplanetPositions);
        console.log('Person B Planet Positions:', this.personBplanetPositions);
      },
      error: (error) => {
        this.errorMessage = error.message || 'Unable to calculate compatibility right now.';
        this.loading = false;
      },
    });
  }

  private toPersonRequest(prefix: 'boy' | 'girl'): MatchRequestPerson {
    const value = this.form.value;
    const birthDate = value[`${prefix}BirthDate`];
    const birthTime = value[`${prefix}BirthTime`];

    return {
      name: String(value[`${prefix}Name`] ?? ''),
      gender: prefix === 'boy' ? 'Male' : 'Female',
      birthDateTime: `${birthDate}T${birthTime}:00`,
      birthPlace: String(value[`${prefix}BirthPlace`] ?? ''),
      latitude: Number(value[`${prefix}Latitude`] ?? 0),
      longitude: Number(value[`${prefix}Longitude`] ?? 0),
      timeZoneOffset: 5.5,
    };
  }

  private formatDate(value: string | null | undefined): string {
    if (!value) return '—';
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day).toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  private getGrade(score: number): string {
    if (score >= 82) return 'Excellent';
    if (score >= 75) return 'Very Strong';
    if (score >= 65) return 'Strong';
    if (score >= 50) return 'Good';
    return 'Needs Work';
  }
}
