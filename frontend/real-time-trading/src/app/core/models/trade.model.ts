export interface Trade {
  id: number;
  symbol: string;
  side: string;
  quantity: number;
  price: number;
  totalValue: number;
  executedAt: string;
}