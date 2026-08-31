using Microsoft.EntityFrameworkCore;
using RealTimeTradingPlatform.Api.Data;
using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public class PositionService : IPositionService
{
    private readonly TradingDbContext _dbContext;
    private readonly IMarketDataService _marketDataService;

    public PositionService(
        TradingDbContext dbContext,
        IMarketDataService marketDataService)
    {
        _dbContext = dbContext;
        _marketDataService = marketDataService;
    }

    public async Task<IReadOnlyCollection<PositionDto>> GetPositionsAsync(
        CancellationToken cancellationToken = default)
    {
        var trades = await _dbContext.Trades
            .AsNoTracking()
            .OrderBy(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        var positions = trades
            .GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var buyTrades = group
                    .Where(t => t.Side.Equals(
                        "BUY",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var sellTrades = group
                    .Where(t => t.Side.Equals(
                        "SELL",
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var buyQuantity = buyTrades.Sum(t => t.Quantity);

                var sellQuantity = sellTrades.Sum(t => t.Quantity);

                var netQuantity = buyQuantity - sellQuantity;

                if (netQuantity == 0)
                {
                    return null;
                }

                var totalBuyValue = buyTrades.Sum(
                    t => t.Quantity * t.Price);

                var averagePrice = buyQuantity > 0
                    ? totalBuyValue / buyQuantity
                    : 0;

                var marketPrice =
                    _marketDataService.GetLatestPrice(
                        group.Key);

                var currentPrice = marketPrice?.Price ?? 0;

                var marketValue =
                    netQuantity * currentPrice;

                var unrealizedProfitLoss =
                    (currentPrice - averagePrice) * netQuantity;

                return new PositionDto
                {
                    Symbol = group.Key,
                    Quantity = netQuantity,
                    AveragePrice = averagePrice,
                    CurrentPrice = currentPrice,
                    MarketValue = marketValue,
                    UnrealizedProfitLoss = unrealizedProfitLoss
                };
            })
            .Where(position => position is not null)
            .Select(position => position!)
            .ToList();

        return positions;
    }
}