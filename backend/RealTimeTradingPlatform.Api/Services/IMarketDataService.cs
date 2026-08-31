using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public interface IMarketDataService
{
    IReadOnlyCollection<MarketPriceDto>
        GetLatestPrices();

    MarketPriceDto?
        GetLatestPrice(string symbol);

    void UpdatePrice(
        string symbol,
        decimal price,
        DateTime timestamp);
}