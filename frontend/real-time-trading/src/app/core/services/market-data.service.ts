import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState
} from '@microsoft/signalr';

import { MarketPrice } from '../models/market-price.model';
import { ConnectionStatus, HealthStatus } from '../models/health-status.model';

@Injectable({
  providedIn: 'root'
})
export class MarketDataService {

  private readonly apiUrl = 'http://localhost:5050/api';
  private readonly hubUrl = 'http://localhost:5050/hubs/market-data';
  private readonly priceUpdatesSubject = new Subject<MarketPrice>();
  private readonly hubStatusSubject = new BehaviorSubject<ConnectionStatus>('Connecting');
  private hubConnection?: HubConnection;

  readonly priceUpdates$ = this.priceUpdatesSubject.asObservable();
  readonly hubStatus$ = this.hubStatusSubject.asObservable();

  constructor(
    private http: HttpClient,
    private zone: NgZone
  ) {}

  getMarketPrices(): Observable<MarketPrice[]> {
    return this.http.get<MarketPrice[]>(
      `${this.apiUrl}/prices`
    );
  }

  getHealth(): Observable<HealthStatus> {
    return this.http.get<HealthStatus>(`${this.apiUrl}/health`);
  }

  async connectToLivePrices(): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected ||
        this.hubConnection?.state === HubConnectionState.Connecting ||
        this.hubConnection?.state === HubConnectionState.Reconnecting) {
      return;
    }

    this.hubStatusSubject.next('Connecting');

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .build();

    this.hubConnection.on('PriceUpdated', (price: MarketPrice) => {
      this.zone.run(() => this.priceUpdatesSubject.next(price));
    });

    this.hubConnection.onreconnecting(() =>
      this.zone.run(() => this.hubStatusSubject.next('Connecting')));

    this.hubConnection.onreconnected(() =>
      this.zone.run(() => this.hubStatusSubject.next('Connected')));

    this.hubConnection.onclose(() =>
      this.zone.run(() => this.hubStatusSubject.next('Disconnected')));

    try {
      await this.hubConnection.start();
      this.zone.run(() => this.hubStatusSubject.next('Connected'));
    } catch {
      this.zone.run(() => this.hubStatusSubject.next('Error'));
    }
  }

  async disconnectFromLivePrices(): Promise<void> {
    await this.hubConnection?.stop();
  }
}
