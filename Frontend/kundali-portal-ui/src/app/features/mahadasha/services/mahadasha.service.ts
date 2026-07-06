import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import analysisMock from '../mockdata/mahadasha-analysis.mock.json';
import { MahadashaAnalysis } from '../models/mahadasha.model';

@Injectable({
  providedIn: 'root',
})
export class MahadashaService {
  getAnalysis(): Observable<MahadashaAnalysis> {
    return of(analysisMock as MahadashaAnalysis);
  }
}
