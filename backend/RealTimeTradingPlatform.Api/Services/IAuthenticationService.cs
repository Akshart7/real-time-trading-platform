namespace RealTimeTradingPlatform.Api.Services;

public interface IAuthenticationService
{
    Task<string> AuthenticateAsync(
        CancellationToken cancellationToken = default);
}