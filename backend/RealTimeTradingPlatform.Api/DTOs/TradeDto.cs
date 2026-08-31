namespace RealTimeTradingPlatform.Api.DTOs;

public class TradeDto
{
    public int Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalValue { get; set; }

    public DateTime ExecutedAt { get; set; }
}