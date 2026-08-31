export interface OrderRequest {
  symbol: string;
  side: string;
  quantity: number;
}

export interface OrderResponse {
  orderId: number;
  symbol: string;
  side: string;
  quantity: number;
  price: number;
  status: string;
  tradeId: number;
  executedAt: string;
}