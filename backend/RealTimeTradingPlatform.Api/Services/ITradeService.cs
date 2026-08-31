using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public interface ITradeService
{
    Task<TradeDto> CreateTradeAsync(
        int orderId,
        string symbol,
        string side,
        decimal quantity,
        decimal price,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<TradeDto>> GetTradesAsync(
        CancellationToken cancellationToken = default);
}