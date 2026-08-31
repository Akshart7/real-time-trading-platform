using Microsoft.AspNetCore.Mvc;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService)
    {
        _positionService = positionService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPositions(
        CancellationToken cancellationToken)
    {
        var positions = await _positionService.GetPositionsAsync(
            cancellationToken);

        return Ok(positions);
    }
}