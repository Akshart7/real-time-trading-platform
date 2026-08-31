using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public interface IOrderService
{
    Task<OrderResponseDto> PlaceOrderAsync(
        OrderRequestDto request,
        CancellationToken cancellationToken = default);
}