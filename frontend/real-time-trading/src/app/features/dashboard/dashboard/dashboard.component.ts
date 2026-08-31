import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subscription, timer, bufferTime } from 'rxjs';

import { MarketDataService } from '../../../core/services/market-data.service';
import { OrderService } from '../../../core/services/order.service';
import { PositionService } from '../../../core/services/position.service';
import { HttpErrorService } from '../../../core/services/http-error.service';
import { MarketPrice } from '../../../core/models/market-price.model';
import { HealthStatus } from '../../../core/models/health-status.model';
import { OrderRequest, OrderResponse } from '../../../core/models/order.model';
import { Position } from '../../../core/models/position.model';
import { Trade } from '../../../core/models/trade.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  marketPrices: MarketPrice[] = [];
  positions: Position[] = [];
  trades: Trade[] = [];
  health: HealthStatus = { status: 'Connecting', subscribedSymbols: 0 };

  selectedSymbol = '';
  selectedSide = 'BUY';
  quantity: number | null = null;

  loadingMarketData = true;
  loadingPositions = true;
  loadingTrades = true;
  placingOrder = false;
  errorMessage = '';
  successMessage = '';

  private readonly subscriptions = new Subscription();

  constructor(
    private marketDataService: MarketDataService,
    private orderService: OrderService,
    private positionService: PositionService,
    private httpErrorService: HttpErrorService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
    void this.marketDataService.connectToLivePrices();

    this.subscriptions.add(this.marketDataService.priceUpdates$
      .pipe(bufferTime(250))
      .subscribe(prices => {
        if (prices.length > 0) {
          this.applyPriceUpdates(prices);
        }
      }));

    // A low-frequency REST refresh is retained as a resilient fallback if the
    // browser temporarily loses its SignalR connection.
    this.subscriptions.add(timer(5000, 5000).subscribe(() => {
      this.loadMarketPrices();
      this.loadHealth();
    }));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    void this.marketDataService.disconnectFromLivePrices();
  }

  loadDashboard(): void {
    this.loadHealth();
    this.loadMarketPrices();
    this.loadPositions();
    this.loadTrades();
  }

  loadHealth(): void {
    this.marketDataService.getHealth().subscribe({
      next: health => this.health = health,
      error: error => {
        this.health = { status: 'Error', subscribedSymbols: 0 };
        this.errorMessage = this.httpErrorService.getErrorMessage(error);
      }
    });
  }

  loadMarketPrices(): void {
    this.loadingMarketData = this.marketPrices.length === 0;
    this.marketDataService.getMarketPrices().subscribe({
      next: prices => {
        this.marketPrices = [...prices].sort((a, b) =>
          a.symbol.localeCompare(b.symbol));
        this.loadingMarketData = false;
        if (!this.selectedSymbol && prices.length > 0) {
          this.selectedSymbol = prices[0].symbol;
        }
      },
      error: error => {
        this.loadingMarketData = false;
        this.errorMessage = this.httpErrorService.getErrorMessage(error);
      }
    });
  }

  loadPositions(): void {
    this.loadingPositions = true;
    this.positionService.getPositions().subscribe({
      next: positions => {
        this.positions = positions;
        this.loadingPositions = false;
      },
      error: error => {
        this.loadingPositions = false;
        this.errorMessage = this.httpErrorService.getErrorMessage(error);
      }
    });
  }

  loadTrades(): void {
    this.loadingTrades = true;
    this.orderService.getTrades().subscribe({
      next: trades => {
        this.trades = trades;
        this.loadingTrades = false;
      },
      error: error => {
        this.loadingTrades = false;
        this.errorMessage = this.httpErrorService.getErrorMessage(error);
      }
    });
  }

  getSelectedPrice(): number {
    return this.marketPrices.find(price => price.symbol === this.selectedSymbol)?.price ?? 0;
  }

  get totalUnrealizedProfitLoss(): number {
    return this.positions.reduce((total, position) =>
      total + position.unrealizedProfitLoss, 0);
  }

  get estimatedOrderValue(): number {
    return (this.quantity ?? 0) * this.getSelectedPrice();
  }

  placeOrder(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.selectedSymbol) {
      this.errorMessage = 'Select an instrument before placing an order.';
      return;
    }

    if (!this.quantity || this.quantity <= 0) {
      this.errorMessage = 'Quantity must be greater than zero.';
      return;
    }

    const request: OrderRequest = {
      symbol: this.selectedSymbol,
      side: this.selectedSide,
      quantity: this.quantity
    };

    this.placingOrder = true;
    this.orderService.placeOrder(request).subscribe({
      next: (response: OrderResponse) => {
        this.placingOrder = false;
        this.successMessage = `Order #${response.orderId} filled at ${response.price}.`;
        this.quantity = null;
        this.loadPositions();
        this.loadTrades();
      },
      error: error => {
        this.placingOrder = false;
        this.errorMessage = this.httpErrorService.getErrorMessage(error);
      }
    });
  }

  trackBySymbol(_: number, item: MarketPrice): string {
    return item.symbol;
  }

  private applyPriceUpdates(updates: MarketPrice[]): void {
    if (!updates || updates.length === 0) return;

    // Deduplicate updates in the buffer (keep the latest per symbol)
    const latestUpdates = new Map<string, MarketPrice>();
    for (const update of updates) {
      latestUpdates.set(update.symbol, update);
    }

    let pricesChanged = false;
    let newSelectedSymbol = this.selectedSymbol;
    let nextPrices = [...this.marketPrices];

    for (const update of latestUpdates.values()) {
      const index = nextPrices.findIndex(price => price.symbol === update.symbol);
      if (index === -1) {
        nextPrices.push(update);
        pricesChanged = true;
      } else {
        const oldPrice = nextPrices[index].price;
        const newPrice = update.price;
        if (oldPrice !== newPrice) {
          nextPrices[index] = update;
          pricesChanged = true;
        }
      }

      if (!newSelectedSymbol) {
        newSelectedSymbol = update.symbol;
      }
    }

    if (pricesChanged) {
      this.marketPrices = nextPrices.sort((a, b) => a.symbol.localeCompare(b.symbol));
    }
    
    if (!this.selectedSymbol && newSelectedSymbol) {
      this.selectedSymbol = newSelectedSymbol;
    }
  }
}
