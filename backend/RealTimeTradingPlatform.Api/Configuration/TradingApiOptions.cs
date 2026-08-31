namespace RealTimeTradingPlatform.Api.Configuration;

public class TradingApiOptions
{
    public string AuthUrl { get; set; } = string.Empty;

    public string WebSocketUrl { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool ForceSimulatedData { get; set; } = false;
}