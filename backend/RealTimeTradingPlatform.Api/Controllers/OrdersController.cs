using Microsoft.AspNetCore.Mvc;
using RealTimeTradingPlatform.Api.DTOs;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ITradeService _tradeService;

    public OrdersController(
        IOrderService orderService,
        ITradeService tradeService)
    {
        _orderService = orderService;
        _tradeService = tradeService;
    }

    [HttpPost]
    public async Task<IActionResult> PlaceOrder(
        [FromBody] OrderRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result =
                await _orderService.PlaceOrderAsync(
                    request,
                    cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("trades")]
    public async Task<IActionResult> GetTrades(
        CancellationToken cancellationToken)
    {
        var trades =
            await _tradeService.GetTradesAsync(
                cancellationToken);

        return Ok(trades);
    }
}