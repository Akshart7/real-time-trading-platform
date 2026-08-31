namespace RealTimeTradingPlatform.Api.Models;

public class Order
{
    public int Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Side { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = "Executed";

    public Trade? Trade { get; set; }
}