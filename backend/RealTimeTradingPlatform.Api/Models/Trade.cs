namespace RealTimeTradingPlatform.Api.Models;

public class Trade
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public decimal TotalValue { get; set; }

    public DateTime ExecutedAt { get; set; }

    public Order? Order { get; set; }
}