namespace RealTimeTradingPlatform.Api.DTOs;

public class HealthStatusDto
{
    public string Status { get; init; } = "Connecting";

    public int SubscribedSymbols { get; init; }

    public DateTime? LastPriceAt { get; init; }

    public string? Error { get; init; }
}
