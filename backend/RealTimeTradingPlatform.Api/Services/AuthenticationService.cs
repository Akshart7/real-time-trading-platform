using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RealTimeTradingPlatform.Api.Configuration;
using RealTimeTradingPlatform.Api.DTOs;

namespace RealTimeTradingPlatform.Api.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly TradingApiOptions _options;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IOptions<TradingApiOptions> options,
        ITokenService tokenService,
        ILogger<AuthenticationService> logger)
    {
        _options = options.Value;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<string> AuthenticateAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Username))
        {
            throw new InvalidOperationException(
                "Trading API username is missing from configuration.");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException(
                "Trading API password is missing from configuration.");
        }

        if (!Uri.TryCreate(_options.AuthUrl, UriKind.Absolute, out var authUri))
        {
            throw new InvalidOperationException(
                "Trading API authentication URL is missing or invalid.");
        }

        // This server binds its nonce to the TCP connection. Limit the client to
        // one pooled connection so the signed request reuses the challenge's
        // connection (the same behavior as curl --digest).
        using var handler = new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 1
        };
        using var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/8.15.0");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("*/*");

        _logger.LogInformation(
            "Sending Digest authentication request to trading provider.");

        // This provider's Digest challenge is not handled consistently by
        // HttpClientHandler. Obtain the challenge, then send the RFC 7616
        // response explicitly.
        string digestChallengeParameter;

        using (var challengeResponse = await httpClient.GetAsync(
                   authUri,
                   cancellationToken))
        {
            if (challengeResponse.StatusCode != HttpStatusCode.Unauthorized)
            {
                throw new HttpRequestException(
                    $"Trading API did not return a Digest challenge. Status code: {challengeResponse.StatusCode}.");
            }

            var digestChallenge = challengeResponse.Headers.WwwAuthenticate
                .FirstOrDefault(header => header.Scheme.Equals(
                    "Digest",
                    StringComparison.OrdinalIgnoreCase));

            if (digestChallenge is null)
            {
                throw new InvalidOperationException(
                    "Trading API did not provide a Digest authentication challenge.");
            }

            digestChallengeParameter = digestChallenge.Parameter ?? string.Empty;
            await challengeResponse.Content.LoadIntoBufferAsync(cancellationToken);
        }

        var authorization = CreateDigestAuthorizationHeader(
            digestChallengeParameter,
            authUri,
            _options.Username,
            _options.Password);

        using var request = new HttpRequestMessage(HttpMethod.Get, authUri);
        request.Headers.TryAddWithoutValidation(
            "Authorization",
            authorization);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        var responseContent =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        _logger.LogInformation(
            "Authentication HTTP status: {StatusCode}",
            response.StatusCode);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError(
                "Trading API rejected the Digest authentication request.");

            throw new HttpRequestException(
                "Trading API authentication failed with 401 Unauthorized.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Trading API authentication failed. Status code: {StatusCode}",
                response.StatusCode);

            throw new HttpRequestException(
                $"Authentication request failed with status code {response.StatusCode}.");
        }

        AuthenticationResponse? authResponse;

        try
        {
            authResponse = JsonSerializer.Deserialize<AuthenticationResponse>(
                responseContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize authentication response.");

            throw new InvalidOperationException(
                "Trading API returned an invalid authentication response.",
                ex);
        }

        if (authResponse is null)
        {
            throw new InvalidOperationException(
                "Trading API returned an empty authentication response.");
        }

        if (!authResponse.Success)
        {
            _logger.LogError(
                "Trading API authentication was unsuccessful. Message: {Message}",
                authResponse.Message);

            throw new InvalidOperationException(
                $"Trading API authentication failed: {authResponse.Message}");
        }

        if (string.IsNullOrWhiteSpace(authResponse.Result))
        {
            throw new InvalidOperationException(
                "Trading API did not return an authentication token.");
        }

        var token = authResponse.Result;

        _tokenService.SetToken(token);

        _logger.LogInformation(
            "Successfully authenticated with trading provider.");

        return token;
    }

    private static string CreateDigestAuthorizationHeader(
        string challenge,
        Uri requestUri,
        string username,
        string password)
    {
        var values = Regex.Matches(
                challenge,
                @"(?<name>[a-zA-Z]+)=(?:""(?<quoted>(?:\\.|[^""])*)""|(?<unquoted>[^,\s]+))")
            .ToDictionary(
                match => match.Groups["name"].Value,
                match => Regex.Unescape(match.Groups["quoted"].Success
                    ? match.Groups["quoted"].Value
                    : match.Groups["unquoted"].Value),
                StringComparer.OrdinalIgnoreCase);

        if (!values.TryGetValue("realm", out var realm) ||
            !values.TryGetValue("nonce", out var nonce))
        {
            throw new InvalidOperationException(
                "Trading API returned an incomplete Digest authentication challenge.");
        }

        var qop = values.TryGetValue("qop", out var qopValue) &&
                  qopValue.Split(',').Any(value =>
                      value.Trim().Equals("auth", StringComparison.OrdinalIgnoreCase))
            ? "auth"
            : null;

        var uri = requestUri.PathAndQuery;
        var nonceCount = "00000001";
        var clientNonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var ha1 = Md5($"{username}:{realm}:{password}");
        var ha2 = Md5($"GET:{uri}");
        var response = qop is null
            ? Md5($"{ha1}:{nonce}:{ha2}")
            : Md5($"{ha1}:{nonce}:{nonceCount}:{clientNonce}:{qop}:{ha2}");

        var parts = new List<string>
        {
            $"username=\"{Escape(username)}\"",
            $"realm=\"{Escape(realm)}\"",
            $"nonce=\"{Escape(nonce)}\"",
            $"uri=\"{Escape(uri)}\"",
            $"cnonce=\"{clientNonce}\"",
            $"nc={nonceCount}",
            $"response=\"{response}\""
        };

        if (values.TryGetValue("opaque", out var opaque))
        {
            parts.Add($"opaque=\"{Escape(opaque)}\"");
        }

        if (qop is not null)
        {
            parts.Add($"qop=\"{qop}\"");
        }

        return "Digest " + string.Join(", ", parts);
    }

    private static string Md5(string value) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
