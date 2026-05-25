import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class LocationService {

  private http = inject(HttpClient);

  searchLocation(query: string) {

  const url =
    `https://nominatim.openstreetmap.org/search?q=${query},India&countrycodes=in&format=json&addressdetails=1&limit=5`;

  return this.http.get<any[]>(url);
}
}