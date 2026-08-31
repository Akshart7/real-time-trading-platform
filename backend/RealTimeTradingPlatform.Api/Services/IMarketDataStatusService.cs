using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public interface IMarketDataStatusService
{
    HealthStatusDto GetStatus();

    void SetConnecting();

    void SetConnected(int subscribedSymbols);

    void SetDisconnected();

    void SetError(string error);

    void RecordPriceUpdate();
}
