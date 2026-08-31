namespace RealTimeTradingPlatform.Api.Services;

public interface ITokenService
{
    string? GetToken();

    void SetToken(string token);

    void ClearToken();

    bool HasToken();
}