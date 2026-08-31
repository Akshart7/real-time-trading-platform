using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using RealTimeTradingPlatform.Api.DTOs;
using RealTimeTradingPlatform.Api.Hubs;

namespace RealTimeTradingPlatform.Api.Services;

public class MarketDataService : IMarketDataService
{
    private readonly ConcurrentDictionary<
        string,
        MarketPriceDto> _latestPrices = new(
            StringComparer.OrdinalIgnoreCase);

    private readonly IHubContext<MarketDataHub> _hubContext;

    public MarketDataService(
        IHubContext<MarketDataHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public IReadOnlyCollection<MarketPriceDto>
        GetLatestPrices()
    {
        return _latestPrices.Values.ToList();
    }

    public MarketPriceDto? GetLatestPrice(
        string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return null;
        }

        _latestPrices.TryGetValue(
            symbol,
            out var price);

        return price;
    }

    public void UpdatePrice(
        string symbol,
        decimal price,
        DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            return;
        }

        var marketPrice = new MarketPriceDto
        {
            Symbol = symbol,
            Price = price,
            Timestamp = timestamp
        };

        _latestPrices.AddOrUpdate(
            symbol,
            marketPrice,
            (_, _) => marketPrice);

        _ = BroadcastPriceAsync(
            marketPrice);
    }

    private async Task BroadcastPriceAsync(
        MarketPriceDto marketPrice)
    {
        await _hubContext.Clients.All.SendAsync(
            "PriceUpdated",
            marketPrice);
    }
}