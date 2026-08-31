using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RealTimeTradingPlatform.Api.Data;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradesController : ControllerBase
{
    private readonly TradingDbContext _context;

    public TradesController(TradingDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTrades(
        CancellationToken cancellationToken)
    {
        var trades = await _context.Trades
            .AsNoTracking()
            .OrderByDescending(t => t.ExecutedAt)
            .ToListAsync(cancellationToken);

        return Ok(trades);
    }
}