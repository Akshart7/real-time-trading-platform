export type ConnectionStatus =
  | 'Connected'
  | 'Connecting'
  | 'Disconnected'
  | 'Error';

export interface HealthStatus {
  status: ConnectionStatus;
  subscribedSymbols: number;
  lastPriceAt?: string;
  error?: string;
}
