using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RealTimeTradingPlatform.Api.Data;
using RealTimeTradingPlatform.Api.DTOs;
using RealTimeTradingPlatform.Api.Models;

namespace RealTimeTradingPlatform.Api.Services;

public class TradeService : ITradeService
{
    private readonly TradingDbContext _dbContext;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<RealTimeTradingPlatform.Api.Hubs.MarketDataHub> _hubContext;

    public TradeService(
        TradingDbContext dbContext,
        Microsoft.AspNetCore.SignalR.IHubContext<RealTimeTradingPlatform.Api.Hubs.MarketDataHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    public async Task<TradeDto> CreateTradeAsync(
        int orderId,
        string symbol,
        string side,
        decimal quantity,
        decimal price,
        CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
        {
            throw new ArgumentException(
                "Order ID must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException(
                "Trade symbol is required.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentException(
                "Trade quantity must be greater than zero.");
        }

        if (price <= 0)
        {
            throw new ArgumentException(
                "Trade price must be greater than zero.");
        }

        var orderExists = await _dbContext.Orders
            .AnyAsync(
                o => o.Id == orderId,
                cancellationToken);

        if (!orderExists)
        {
            throw new InvalidOperationException(
                $"Order with ID {orderId} does not exist.");
        }

        var trade = new Trade
        {
            OrderId = orderId,
            Symbol = symbol,
            Side = side.ToUpperInvariant(),
            Quantity = quantity,
            Price = price,
            TotalValue = quantity * price,
            ExecutedAt = DateTime.UtcNow
        };

        _dbContext.Trades.Add(trade);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        var tradeDto = new TradeDto
        {
            Id = trade.Id,
            Symbol = trade.Symbol,
            Side = trade.Side,
            Quantity = trade.Quantity,
            Price = trade.Price,
            TotalValue = trade.TotalValue,
            ExecutedAt = trade.ExecutedAt
        };

        await _hubContext.Clients.All.SendAsync("TradeCreated", tradeDto, cancellationToken);

        return tradeDto;
    }

    public async Task<IReadOnlyCollection<TradeDto>> GetTradesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Trades
            .AsNoTracking()
            .OrderByDescending(t => t.ExecutedAt)
            .Select(t => new TradeDto
            {
                Id = t.Id,
                Symbol = t.Symbol,
                Side = t.Side,
                Quantity = t.Quantity,
                Price = t.Price,
                TotalValue = t.TotalValue,
                ExecutedAt = t.ExecutedAt
            })
            .ToListAsync(cancellationToken);
    }
}