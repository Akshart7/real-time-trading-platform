namespace RealTimeTradingPlatform.Api.DTOs;

public class OrderResponseDto
{
    public int OrderId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public string Status { get; set; } = string.Empty;

    public int TradeId { get; set; }

    public DateTime ExecutedAt { get; set; }
}