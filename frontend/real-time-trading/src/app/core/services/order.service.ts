import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  OrderRequest,
  OrderResponse
} from '../models/order.model';

import { Trade } from '../models/trade.model';

@Injectable({
  providedIn: 'root'
})
export class OrderService {

  private readonly apiUrl = 'http://localhost:5050/api/orders';
  private readonly apiBaseUrl = 'http://localhost:5050/api';

  constructor(private http: HttpClient) {}

  placeOrder(
    request: OrderRequest
  ): Observable<OrderResponse> {

    return this.http.post<OrderResponse>(
      this.apiUrl,
      request
    );
  }

  getTrades(): Observable<Trade[]> {

    return this.http.get<Trade[]>(`${this.apiBaseUrl}/trades`);
  }
}
