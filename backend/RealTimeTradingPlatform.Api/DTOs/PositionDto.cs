namespace RealTimeTradingPlatform.Api.DTOs;

public class PositionDto
{
    public string Symbol { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal AveragePrice { get; set; }

    public decimal CurrentPrice { get; set; }

    public decimal MarketValue { get; set; }

    public decimal UnrealizedProfitLoss { get; set; }
}