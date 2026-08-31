import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Position } from '../models/position.model';

@Injectable({
  providedIn: 'root'
})
export class PositionService {

  private readonly apiUrl = 'http://localhost:5050/api/positions';

  constructor(private http: HttpClient) {}

  getPositions(): Observable<Position[]> {

    return this.http.get<Position[]>(
      this.apiUrl
    );
  }
}