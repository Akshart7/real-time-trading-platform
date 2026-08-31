using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public interface IPositionService
{
    Task<IReadOnlyCollection<PositionDto>> GetPositionsAsync(
        CancellationToken cancellationToken = default);
}