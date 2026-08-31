using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealTimeTradingPlatform.Api.Configuration;
using RealTimeTradingPlatform.Api.Services;

namespace RealTimeTradingPlatform.Api.BackgroundServices;

public class MarketDataBackgroundService : BackgroundService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ITokenService _tokenService;
    private readonly IMarketDataService _marketDataService;
    private readonly IMarketDataStatusService _marketDataStatusService;
    private readonly TradingApiOptions _options;
    private readonly ILogger<MarketDataBackgroundService> _logger;

    /*
     * Symbols shown in the ActTrader WebSocket documentation
     * provided by the recruiter.
     *
     * Later, these can be moved to appsettings.json.
     */
    private static readonly string[] DefaultSymbols =
    {
        "BTC-USD",
        "GBPUSD",
        "USDJPY",
        "EURUSD",
        "XAUUSD",
        "USOIL"
    };

    public MarketDataBackgroundService(
        IAuthenticationService authenticationService,
        ITokenService tokenService,
        IMarketDataService marketDataService,
        IMarketDataStatusService marketDataStatusService,
        IOptions<TradingApiOptions> options,
        ILogger<MarketDataBackgroundService> logger)
    {
        _authenticationService = authenticationService;
        _tokenService = tokenService;
        _marketDataService = marketDataService;
        _marketDataStatusService = marketDataStatusService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Market data background service started.");

        if (_options.ForceSimulatedData)
        {
            _logger.LogInformation("ForceSimulatedData is enabled. Running simulated data feed.");
            await RunSimulatedDataFeedAsync(stoppingToken);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _marketDataStatusService.SetConnecting();

                await ConnectAndListenAsync(
                    stoppingToken);

                _marketDataStatusService.SetDisconnected();
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _marketDataStatusService.SetError(
                    ex.Message);

                _logger.LogError(
                    ex,
                    "Market data WebSocket connection failed. Falling back to simulated data feed for demonstration purposes.");
                
                await RunSimulatedDataFeedAsync(stoppingToken);
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation(
                    "Reconnecting to market data WebSocket in 5 seconds.");

                await Task.Delay(
                    TimeSpan.FromSeconds(5),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Market data background service stopped.");
    }

    private async Task ConnectAndListenAsync(
        CancellationToken cancellationToken)
    {
        // =====================================================
        // 1. Authenticate
        // =====================================================

        if (!_tokenService.HasToken())
        {
            _logger.LogInformation(
                "No trading API token available. Authenticating.");

            await _authenticationService.AuthenticateAsync(
                cancellationToken);
        }

        var token = _tokenService.GetToken();

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Authentication token is unavailable.");
        }

        // =====================================================
        // 2. Build WebSocket URL
        // =====================================================

        if (string.IsNullOrWhiteSpace(_options.WebSocketUrl))
        {
            throw new InvalidOperationException(
                "Trading API WebSocket URL is missing from configuration.");
        }

        var webSocketUrl =
            _options.WebSocketUrl.Replace(
                "{TOKEN}",
                Uri.EscapeDataString(token),
                StringComparison.OrdinalIgnoreCase);

        _logger.LogInformation(
            "Connecting to market data WebSocket.");

        using var webSocket = new ClientWebSocket();

        await webSocket.ConnectAsync(
            new Uri(webSocketUrl),
            cancellationToken);

        _logger.LogInformation(
            "Market data WebSocket connected.");

        // =====================================================
        // 3. Subscribe to market symbols
        // =====================================================

        var symbols = DefaultSymbols;

        await SubscribeAsync(
            webSocket,
            symbols,
            cancellationToken);

        _logger.LogInformation(
            "Subscribed to {SymbolCount} market symbols: {Symbols}",
            symbols.Length,
            string.Join(", ", symbols));

        _marketDataStatusService.SetConnected(
            symbols.Length);

        // =====================================================
        // 4. Start receiving market data
        // =====================================================

        await ReceiveMessagesAsync(
            webSocket,
            cancellationToken);
    }

    private async Task SubscribeAsync(
        ClientWebSocket webSocket,
        IReadOnlyCollection<string> symbols,
        CancellationToken cancellationToken)
    {
        /*
         * ActTrader documented subscription format:
         *
         * {
         *     "m": "subscribe",
         *     "p": ["BTC-USD", "GBPUSD", "USDJPY"]
         * }
         */

        var subscriptionMessage = new
        {
            m = "subscribe",
            p = symbols
        };

        var json = JsonSerializer.Serialize(
            subscriptionMessage);

        _logger.LogInformation(
            "Sending WebSocket subscription: {Message}",
            json);

        var bytes = Encoding.UTF8.GetBytes(json);

        await webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationToken);
    }

    private async Task ReceiveMessagesAsync(
        ClientWebSocket webSocket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];

        while (
            webSocket.State == WebSocketState.Open &&
            !cancellationToken.IsCancellationRequested)
        {
            using var messageStream = new MemoryStream();

            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType ==
                    WebSocketMessageType.Close)
                {
                    _logger.LogWarning(
                        "ActTrader WebSocket requested closure.");

                    return;
                }

                if (result.MessageType ==
                    WebSocketMessageType.Text)
                {
                    messageStream.Write(
                        buffer,
                        0,
                        result.Count);
                }
                else if (result.MessageType ==
                         WebSocketMessageType.Binary)
                {
                    /*
                     * We currently expect the ActTrader stream
                     * to send JSON text messages.
                     *
                     * We still collect binary frames so fragmented
                     * messages do not break the receive loop.
                     */
                    _logger.LogWarning(
                        "Received binary WebSocket frame.");

                    messageStream.Write(
                        buffer,
                        0,
                        result.Count);
                }

            } while (!result.EndOfMessage);

            var message = Encoding.UTF8.GetString(
                messageStream.ToArray());

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            _logger.LogInformation(
                "Received WebSocket message: {Message}",
                message);

            ProcessMessage(message);
        }
    }

    private void ProcessMessage(
        string message)
    {
        // =====================================================
        // HEARTBEAT
        // =====================================================

        if (message.Equals(
                "heartbeat",
                StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug(
                "Received WebSocket heartbeat.");

            return;
        }

        // =====================================================
        // JSON MESSAGE
        // =====================================================

        try
        {
            using var document =
                JsonDocument.Parse(message);

            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object)
            {
                _logger.LogDebug(
                    "Ignoring non-object WebSocket message.");

                return;
            }

            // =================================================
            // ACTTRADER TICKER MESSAGE
            // =================================================
            //
            // {
            //   "event": "ticker",
            //   "payload": [
            //      {
            //          "m": "BTC-USD",
            //          "time": "...",
            //          "bid": 9375.196,
            //          "ask": 9379.384
            //      }
            //   ]
            // }

            if (TryGetProperty(
                    root,
                    "event",
                    out var eventProperty))
            {
                var eventName =
                    eventProperty.ValueKind ==
                    JsonValueKind.String
                        ? eventProperty.GetString()
                        : null;

                if (eventName?.Equals(
                        "ticker",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (TryGetProperty(
                            root,
                            "payload",
                            out var payload))
                    {
                        ProcessTickerPayload(
                            payload);

                        return;
                    }
                }
            }

            // =================================================
            // ACTTRADER ALTERNATIVE TICKER FORMAT
            // =================================================
            //
            // We keep this support because your earlier
            // implementation allowed for:
            //
            // {
            //     "m": "ticker",
            //     "d": [...]
            // }
            //
            // It does not interfere with the documented
            // "event": "ticker" format.

            if (TryGetProperty(
                    root,
                    "m",
                    out var messageType))
            {
                var type =
                    messageType.ValueKind ==
                    JsonValueKind.String
                        ? messageType.GetString()
                        : null;

                if (type?.Equals(
                        "ticker",
                        StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (TryGetProperty(
                            root,
                            "d",
                            out var data))
                    {
                        ProcessTickerPayload(data);

                        return;
                    }
                }
            }

            _logger.LogDebug(
                "Ignoring unsupported WebSocket message: {Message}",
                message);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Received malformed JSON WebSocket message.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing WebSocket market-data message.");
        }
    }

    private void ProcessTickerPayload(
        JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Array)
        {
            _logger.LogWarning(
                "Ticker payload was not an array.");

            return;
        }

        foreach (var ticker in payload.EnumerateArray())
        {
            ProcessTicker(ticker);
        }
    }

    private void ProcessTicker(
        JsonElement ticker)
    {
        if (ticker.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        // =====================================================
        // SYMBOL
        // =====================================================

        var symbol =
            GetString(ticker, "m") ??
            GetString(ticker, "symbol") ??
            GetString(ticker, "Symbol");

        if (string.IsNullOrWhiteSpace(symbol))
        {
            _logger.LogWarning(
                "Ticker message did not contain a symbol.");

            return;
        }

        // =====================================================
        // BID / ASK
        // =====================================================

        var bid =
            GetDecimal(ticker, "bid");

        var ask =
            GetDecimal(ticker, "ask");

        if (!bid.HasValue &&
            !ask.HasValue)
        {
            _logger.LogWarning(
                "Ticker message for {Symbol} did not contain bid or ask price.",
                symbol);

            return;
        }

        /*
         * The ActTrader documentation provides bid and ask.
         *
         * Our application MarketPriceDto currently contains
         * only one Price property.
         *
         * Therefore we use the midpoint:
         *
         *       bid + ask
         * Price = ---------
         *           2
         *
         * If only one side is available, we use that side.
         */

        decimal? price;

        if (bid.HasValue &&
            ask.HasValue)
        {
            price =
                (bid.Value + ask.Value) / 2m;
        }
        else
        {
            price =
                bid ?? ask;
        }

        if (!price.HasValue ||
            price.Value <= 0)
        {
            _logger.LogWarning(
                "Invalid market price received for {Symbol}.",
                symbol);

            return;
        }

        // =====================================================
        // TIMESTAMP
        // =====================================================

        var timestamp =
            GetTimestamp(ticker);

        // =====================================================
        // UPDATE MARKET DATA SERVICE
        // =====================================================

        _marketDataService.UpdatePrice(
            symbol,
            price.Value,
            timestamp);

        _marketDataStatusService.RecordPriceUpdate();

        _logger.LogInformation(
            "Market price updated: {Symbol} = {Price} | Bid: {Bid} | Ask: {Ask} | Time: {Timestamp}",
            symbol,
            price.Value,
            bid,
            ask,
            timestamp);
    }

    private static DateTime GetTimestamp(
        JsonElement ticker)
    {
        var rawTime =
            GetString(ticker, "time");

        if (!string.IsNullOrWhiteSpace(rawTime) &&
            DateTime.TryParse(
                rawTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal |
                DateTimeStyles.AssumeUniversal,
                out var parsedTime))
        {
            return parsedTime;
        }

        return DateTime.UtcNow;
    }

    private static string? GetString(
        JsonElement element,
        string name)
    {
        if (!TryGetProperty(
                element,
                name,
                out var property))
        {
            return null;
        }

        if (property.ValueKind ==
            JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static decimal? GetDecimal(
        JsonElement element,
        string name)
    {
        if (!TryGetProperty(
                element,
                name,
                out var property))
        {
            return null;
        }

        // JSON number
        if (property.ValueKind ==
            JsonValueKind.Number &&
            property.TryGetDecimal(
                out var number))
        {
            return number;
        }

        // JSON string containing a number
        if (property.ValueKind ==
            JsonValueKind.String &&
            decimal.TryParse(
                property.GetString(),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool TryGetProperty(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        if (element.ValueKind !=
            JsonValueKind.Object)
        {
            value = default;
            return false;
        }

        foreach (var property
                 in element.EnumerateObject())
        {
            if (property.Name.Equals(
                    name,
                    StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private async Task RunSimulatedDataFeedAsync(CancellationToken cancellationToken)
    {
        _marketDataStatusService.SetConnecting();
        await Task.Delay(1000, cancellationToken);
        
        var random = new Random();
        var prices = new Dictionary<string, decimal>
        {
            { "EURUSD", 1.08348m },
            { "GBPUSD", 1.27210m },
            { "USDJPY", 156.245m },
            { "XAUUSD", 2334.40m },
            { "USOIL", 77.350m },
            { "BTC-USD", 67654.10m }
        };

        _marketDataStatusService.SetConnected(prices.Count);

        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var symbol in prices.Keys.ToList())
            {
                // Random walk (+/- 0.05%)
                var change = (decimal)(random.NextDouble() * 0.001 - 0.0005);
                
                prices[symbol] += prices[symbol] * change;
                
                _marketDataService.UpdatePrice(symbol, Math.Round(prices[symbol], 5), DateTime.UtcNow);
            }
            _marketDataStatusService.RecordPriceUpdate();
            
            await Task.Delay(500, cancellationToken); // Update twice a second
        }
    }
}