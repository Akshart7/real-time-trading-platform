using RealTimeTradingPlatform.Api.Data;
using RealTimeTradingPlatform.Api.DTOs;
using RealTimeTradingPlatform.Api.Models;

namespace RealTimeTradingPlatform.Api.Services;

public class OrderService : IOrderService
{
    private readonly TradingDbContext _dbContext;
    private readonly IMarketDataService _marketDataService;
    private readonly ITradeService _tradeService;

    public OrderService(
        TradingDbContext dbContext,
        IMarketDataService marketDataService,
        ITradeService tradeService)
    {
        _dbContext = dbContext;
        _marketDataService = marketDataService;
        _tradeService = tradeService;
    }

    public async Task<OrderResponseDto> PlaceOrderAsync(
        OrderRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol))
        {
            throw new ArgumentException(
                "Symbol is required.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException(
                "Quantity must be greater than zero.");
        }

        if (!request.Side.Equals(
                "BUY",
                StringComparison.OrdinalIgnoreCase) &&
            !request.Side.Equals(
                "SELL",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Side must be either BUY or SELL.");
        }

        var marketPrice =
            _marketDataService.GetLatestPrice(
                request.Symbol);

        if (marketPrice is null)
        {
            throw new InvalidOperationException(
                $"No market price is available for symbol '{request.Symbol}'.");
        }

        var order = new Order
        {
            Symbol = marketPrice.Symbol,
            Side = request.Side.ToUpperInvariant(),
            Quantity = request.Quantity,
            Price = marketPrice.Price,
            CreatedAt = DateTime.UtcNow,
            Status = "Executed"
        };

        _dbContext.Orders.Add(order);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var trade = await _tradeService.CreateTradeAsync(
            order.Id,
            order.Symbol,
            order.Side,
            order.Quantity,
            order.Price,
            cancellationToken);

        return new OrderResponseDto
        {
            OrderId = order.Id,
            Symbol = order.Symbol,
            Side = order.Side,
            Quantity = order.Quantity,
            Price = order.Price,
            Status = order.Status,
            TradeId = trade.Id,
            ExecutedAt = trade.ExecutedAt
        };
    }
}