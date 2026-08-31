using Microsoft.AspNetCore.Mvc;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IMarketDataStatusService _marketDataStatusService;

    public HealthController(IMarketDataStatusService marketDataStatusService)
    {
        _marketDataStatusService = marketDataStatusService;
    }

    [HttpGet]
    public IActionResult Get() => Ok(_marketDataStatusService.GetStatus());
}
