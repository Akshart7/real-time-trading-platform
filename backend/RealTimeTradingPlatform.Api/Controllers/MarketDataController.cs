using Microsoft.AspNetCore.Mvc;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/prices")]
[Route("api/market-data")]
public class MarketDataController : ControllerBase
{
    private readonly IMarketDataService _marketDataService;

    public MarketDataController(
        IMarketDataService marketDataService)
    {
        _marketDataService = marketDataService;
    }

    [HttpGet]
    public IActionResult GetLatestPrices()
    {
        var prices = _marketDataService.GetLatestPrices();

        return Ok(prices);
    }

    [HttpGet("{symbol}")]
    public IActionResult GetLatestPrice(string symbol)
    {
        var price = _marketDataService.GetLatestPrice(symbol);

        if (price is null)
        {
            return NotFound(new
            {
                message = $"No market price found for {symbol}."
            });
        }

        return Ok(price);
    }
}
