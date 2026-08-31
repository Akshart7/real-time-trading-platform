namespace RealTimeTradingPlatform.Api.DTOs;

public class AuthenticationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;
}