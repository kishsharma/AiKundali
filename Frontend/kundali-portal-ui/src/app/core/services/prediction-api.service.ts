import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { apiEndpoints } from '../../config/api-endpoints';
import { PredictionQuestionRequest, PredictionResponse } from '../../shared/models/api.models';
import { ApiResponse, BaseApiService } from './base-api.service';

@Injectable({
  providedIn: 'root',
})
export class PredictionApiService extends BaseApiService {
  askQuestion(request: PredictionQuestionRequest): Observable<ApiResponse<PredictionResponse>> {
    return this.post<PredictionResponse>(apiEndpoints.prediction.question, request);
  }
}
