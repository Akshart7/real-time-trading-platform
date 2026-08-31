using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public class MarketDataStatusService : IMarketDataStatusService
{
    private readonly object _sync = new();
    private string _status = "Connecting";
    private string? _error;
    private int _subscribedSymbols;
    private DateTime? _lastPriceAt;

    public HealthStatusDto GetStatus()
    {
        lock (_sync)
        {
            return new HealthStatusDto
            {
                Status = _status,
                Error = _error,
                SubscribedSymbols = _subscribedSymbols,
                LastPriceAt = _lastPriceAt
            };
        }
    }

    public void SetConnecting() => SetState("Connecting");

    public void SetConnected(int subscribedSymbols)
    {
        lock (_sync)
        {
            _status = "Connected";
            _error = null;
            _subscribedSymbols = subscribedSymbols;
        }
    }

    public void SetDisconnected() => SetState("Disconnected");

    public void SetError(string error)
    {
        lock (_sync)
        {
            _status = "Error";
            _error = error;
        }
    }

    public void RecordPriceUpdate()
    {
        lock (_sync)
        {
            _lastPriceAt = DateTime.UtcNow;
        }
    }

    private void SetState(string status)
    {
        lock (_sync)
        {
            _status = status;
            if (status != "Error")
            {
                _error = null;
            }
        }
    }
}
