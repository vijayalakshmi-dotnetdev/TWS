// Services/MarketDataService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TWS.Domain.Models;
using TWS.Infrastructure.Configuration;
using TWS.Services.Interfaces;
using WebSocketSharp;

namespace TWS.Services
{
    public class MarketDataService : IMarketDataService
    {
        private readonly ILogger _logger;
        private readonly IConfigurationService _configService;
        private readonly IAuthenticationService _authService;
        private WebSocket _webSocket;
        private bool _isConnected;
        private Timer _heartbeatTimer;
        private string _sessionId;
        private string _clientId;

        public MarketDataService(
            ILogger logger,
            IConfigurationService configService,
            IAuthenticationService authService)
        {
            _logger = logger;
            _configService = configService;
            _authService = authService;
        }

        public event EventHandler<TickData> OnTickReceived;
        public event EventHandler<DepthData> OnDepthReceived;
        public event EventHandler<string> OnError;
        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;

        /// <summary>
        /// Creates WebSocket session using REST API
        /// </summary>
        private async Task<string> CreateWsSessionAsync()
        {
            try
            {
                var accessToken = _authService.GetAuthToken();
                var apiUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var WsSessionendpoint = _configService.GetValue<string>("ApiEndpoints:CreateWsSession");
                var endpoint = $"{apiUrl}{WsSessionendpoint}";           

                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    // Prepare the request body
                    var requestBody = new
                    {
                        userId = "SKYWS1",  // Replace with actual user ID
                        source = "WEB"
                    };

                    var content = new System.Net.Http.StringContent(
                        JsonSerializer.Serialize(requestBody),
                        Encoding.UTF8,
                        "application/json");

                    var response = await httpClient.PostAsync(endpoint, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                        _sessionId = _authService.GetAuthToken();
                        _logger.LogInformation($"WebSocket session created: {_sessionId}");
                        return _sessionId;
                    }
                    else
                    {
                        _logger.LogError($"Failed to create WS session: {responseBody}");
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"CreateWsSessionAsync error: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// SHA256 encryption for session token
        /// </summary>
        private string Sha256Encrypt(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                // Step 1: Create WebSocket session
                _sessionId = await CreateWsSessionAsync();
                if (string.IsNullOrEmpty(_sessionId))
                {
                    _logger.LogError("Cannot connect: Failed to create WebSocket session");
                    OnError?.Invoke(this, "Failed to create WebSocket session");
                    return;
                }

                // Step 2: Get client ID (user ID)
                _clientId = "SKYWS1";
                if (string.IsNullOrEmpty(_clientId))
                {
                    _logger.LogError("Client ID not configured");
                    OnError?.Invoke(this, "Client ID not configured");
                    return;
                }

                var wsUrl = _configService.GetValue<string>("WebSocket:Url");
                _logger.LogInformation($"Connecting to WebSocket: {wsUrl}");

                _webSocket = new WebSocket(wsUrl);

                _webSocket.OnOpen += async (sender, e) =>
                {
                    _logger.LogInformation("WebSocket connection opened, sending connection message...");

                    // Step 3: Send connection message with encrypted session token
                    var susertoken = Sha256Encrypt(Sha256Encrypt(_sessionId));
                    var connectionMessage = new
                    {
                        susertoken = susertoken,
                        t = "c",
                        actid = $"{_clientId}_WEB",
                        uid = $"{_clientId}_WEB",
                        source = "WEB"
                    };

                    var jsonMessage = JsonSerializer.Serialize(connectionMessage);
                    _webSocket.Send(jsonMessage);

                    _isConnected = true;
                    _logger.LogInformation("WebSocket connected and authenticated");
                    OnConnected?.Invoke(this, EventArgs.Empty);

                    // Start heartbeat timer (send every 50 seconds)
                    StartHeartbeat();
                };

                _webSocket.OnMessage += (sender, e) =>
                {
                    HandleMessage(e.Data);
                };

                _webSocket.OnError += (sender, e) =>
                {
                    _logger.LogError($"WebSocket error: {e.Message}");
                    OnError?.Invoke(this, e.Message);
                };

                _webSocket.OnClose += (sender, e) =>
                {
                    _isConnected = false;
                    StopHeartbeat();
                    _logger.LogWarning($"WebSocket closed: {e.Reason}");
                    OnDisconnected?.Invoke(this, EventArgs.Empty);
                };

                _webSocket.Connect();

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ConnectAsync error: {ex.Message}", ex);
                OnError?.Invoke(this, ex.Message);
            }
        }

        /// <summary>
        /// Start sending heartbeat every 50 seconds
        /// </summary>
        private void StartHeartbeat()
        {
            _heartbeatTimer = new Timer(SendHeartbeat, null, TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(50));
            _logger.LogInformation("Heartbeat timer started");
        }

        /// <summary>
        /// Stop heartbeat timer
        /// </summary>
        private void StopHeartbeat()
        {
            _heartbeatTimer?.Dispose();
            _heartbeatTimer = null;
            _logger.LogInformation("Heartbeat timer stopped");
        }

        /// <summary>
        /// Send heartbeat message
        /// </summary>
        private void SendHeartbeat(object state)
        {
            if (_isConnected && _webSocket != null)
            {
                try
                {
                    var heartbeat = new { k = "", t = "h" };
                    var jsonMessage = JsonSerializer.Serialize(heartbeat);
                    _webSocket.Send(jsonMessage);
                    _logger.LogDebug("Heartbeat sent");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"SendHeartbeat error: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Subscribe to Market Data (LTP, Change, OHLC, Volume)
        /// </summary>
        /// <param name="scripCodes">Array of scrip codes in format "EXCHANGE|TOKEN" (e.g., "NSE|11536")</param>
        public async Task SubscribeAsync(string[] scripCodes)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Cannot subscribe: WebSocket not connected");
                return;
            }

            try
            {
                // Format: NFO|54957#MCX|239484
                var scripKey = string.Join("#", scripCodes);

                var subscribeMessage = new
                {
                    k = scripKey,
                    t = "t" // 't' for tick data (market data)
                };

                var jsonMessage = JsonSerializer.Serialize(subscribeMessage);
                _webSocket.Send(jsonMessage);
                _logger.LogInformation($"Subscribed to market data: {scripKey}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SubscribeAsync error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Subscribe to Depth Data (includes market depth with 5 levels)
        /// </summary>
        public async Task SubscribeDepthAsync(string[] scripCodes)
        {
            if (!_isConnected)
            {
                _logger.LogWarning("Cannot subscribe to depth: WebSocket not connected");
                return;
            }

            try
            {
                var scripKey = string.Join("#", scripCodes);

                var subscribeMessage = new
                {
                    k = scripKey,
                    t = "d" // 'd' for depth data
                };

                var jsonMessage = JsonSerializer.Serialize(subscribeMessage);
                _webSocket.Send(jsonMessage);
                _logger.LogInformation($"Subscribed to depth data: {scripKey}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"SubscribeDepthAsync error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unsubscribe from Market Data
        /// </summary>
        public async Task UnsubscribeAsync(string[] scripCodes)
        {
            if (!_isConnected) return;

            try
            {
                var scripKey = string.Join("#", scripCodes);

                var unsubscribeMessage = new
                {
                    k = scripKey,
                    t = "u" // 'u' for unsubscribe
                };

                var jsonMessage = JsonSerializer.Serialize(unsubscribeMessage);
                _webSocket.Send(jsonMessage);
                _logger.LogInformation($"Unsubscribed from market data: {scripKey}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UnsubscribeAsync error: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unsubscribe from Depth Data
        /// </summary>
        public async Task UnsubscribeDepthAsync(string[] scripCodes)
        {
            if (!_isConnected) return;

            try
            {
                var scripKey = string.Join("#", scripCodes);

                var unsubscribeMessage = new
                {
                    k = scripKey,
                    t = "ud" // 'ud' for unsubscribe depth
                };

                var jsonMessage = JsonSerializer.Serialize(unsubscribeMessage);
                _webSocket.Send(jsonMessage);
                _logger.LogInformation($"Unsubscribed from depth data: {scripKey}");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError($"UnsubscribeDepthAsync error: {ex.Message}", ex);
            }
        }

        public void Disconnect()
        {
            if (_webSocket != null && _isConnected)
            {
                StopHeartbeat();
                _webSocket.Close();
                _isConnected = false;
                _logger.LogInformation("WebSocket disconnected");
            }
        }

        public bool IsConnected => _isConnected;

        /// <summary>
        /// Handle incoming WebSocket messages
        /// </summary>
        private void HandleMessage(string message)
        {
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(message))
                {
                    var root = doc.RootElement;

                    if (!root.TryGetProperty("t", out var typeElement))
                        return;

                    var messageType = typeElement.GetString();

                    switch (messageType)
                    {
                        case "tk": // Tick acknowledgement (market data subscription confirmation)
                            _logger.LogInformation($"Tick acknowledgement received: {message}");
                            var tickAck = ParseTickAcknowledgement(root);
                            if (tickAck != null)
                                OnTickReceived?.Invoke(this, tickAck);
                            break;

                        case "tf": // Tick feed (market data updates)
                            var tickData = ParseTickFeed(root);
                            if (tickData != null)
                                OnTickReceived?.Invoke(this, tickData);
                            break;

                        case "dk": // Depth acknowledgement
                            _logger.LogInformation($"Depth acknowledgement received");
                            var depthAck = ParseDepthAcknowledgement(root);
                            if (depthAck != null)
                                OnDepthReceived?.Invoke(this, depthAck);
                            break;

                        case "df": // Depth feed (depth data updates)
                            var depthData = ParseDepthFeed(root);
                            if (depthData != null)
                                OnDepthReceived?.Invoke(this, depthData);
                            break;

                        case "c": // Connection acknowledgement
                            _logger.LogInformation("Connection acknowledged by server");
                            break;

                        default:
                            _logger.LogDebug($"Unknown message type: {messageType}");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"HandleMessage error: {ex.Message}, Message: {message}", ex);
            }
        }

        /// <summary>
        /// Parse tick acknowledgement message (initial snapshot)
        /// </summary>
        private TickData ParseTickAcknowledgement(JsonElement root)
        {
            try
            {
                return new TickData
                {
                    MessageType = "tk",
                    Exchange = GetStringProperty(root, "e"),
                    Token = GetStringProperty(root, "tk"),
                    ScripCode = $"{GetStringProperty(root, "e")}|{GetStringProperty(root, "tk")}",
                    Symbol = GetStringProperty(root, "ts"),
                    LastPrice = GetDecimalProperty(root, "lp"),
                    Change = GetDecimalProperty(root, "c"),
                    PercentageChange = GetDecimalProperty(root, "pc"),
                    Volume = GetLongProperty(root, "v"),
                    Open = GetDecimalProperty(root, "o"),
                    High = GetDecimalProperty(root, "h"),
                    Low = GetDecimalProperty(root, "l"),
                    Close = GetDecimalProperty(root, "c"),
                    AveragePrice = GetDecimalProperty(root, "ap"),
                    OpenInterest = GetLongProperty(root, "oi"),
                    LotSize = GetIntProperty(root, "ls"),
                    TickSize = GetDecimalProperty(root, "ti"),
                    BuyPrice = GetDecimalProperty(root, "bp1"),
                    SellPrice = GetDecimalProperty(root, "sp1"),
                    BuyQuantity1 = GetIntProperty(root, "bq1"),
                    SellQuantity1 = GetIntProperty(root, "sq1"),
                    Timestamp = ParseTimestamp(GetStringProperty(root, "ft"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"ParseTickAcknowledgement error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse tick feed message (price updates)
        /// </summary>
        private TickData ParseTickFeed(JsonElement root)
        {
            try
            {
                return new TickData
                {
                    MessageType = "tf",
                    Exchange = GetStringProperty(root, "e"),
                    Token = GetStringProperty(root, "tk"),
                    ScripCode = $"{GetStringProperty(root, "e")}|{GetStringProperty(root, "tk")}",
                    LastPrice = GetDecimalProperty(root, "lp"),
                    PercentageChange = GetDecimalProperty(root, "pc"),
                    Change = GetDecimalProperty(root, "cv"),
                    Volume = GetLongProperty(root, "v"),
                    BuyPrice = GetDecimalProperty(root, "bp1"),
                    SellPrice = GetDecimalProperty(root, "sp1"),
                    BuyQuantity1 = GetIntProperty(root, "bq1"),
                    SellQuantity1 = GetIntProperty(root, "sq1"),
                    Timestamp = ParseTimestamp(GetStringProperty(root, "ft"))
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"ParseTickFeed error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse depth acknowledgement message
        /// </summary>
        private DepthData ParseDepthAcknowledgement(JsonElement root)
        {
            try
            {
                var depth = new DepthData
                {
                    MessageType = "dk",
                    Exchange = GetStringProperty(root, "e"),
                    Token = GetStringProperty(root, "tk"),
                    Symbol = GetStringProperty(root, "ts"),
                    LastPrice = GetDecimalProperty(root, "lp"),
                    PercentageChange = GetDecimalProperty(root, "pc"),
                    Volume = GetLongProperty(root, "v"),
                    Open = GetDecimalProperty(root, "o"),
                    High = GetDecimalProperty(root, "h"),
                    Low = GetDecimalProperty(root, "l"),
                    Close = GetDecimalProperty(root, "c"),
                    AveragePrice = GetDecimalProperty(root, "ap"),
                    OpenInterest = GetLongProperty(root, "oi"),
                    LastTradedQty = GetIntProperty(root, "ltq"),
                    LastTradedTime = GetStringProperty(root, "ltt"),
                    TotalBuyQty = GetLongProperty(root, "tbq"),
                    TotalSellQty = GetLongProperty(root, "tsq"),
                    UpperCircuit = GetDecimalProperty(root, "uc"),
                    LowerCircuit = GetDecimalProperty(root, "lc"),
                    Timestamp = ParseTimestamp(GetStringProperty(root, "ft"))
                };

                // Parse 5 level depth
                for (int i = 1; i <= 5; i++)
                {
                    depth.BuyPrices.Add(GetDecimalProperty(root, $"bp{i}"));
                    depth.SellPrices.Add(GetDecimalProperty(root, $"sp{i}"));
                    depth.BuyQuantities.Add(GetIntProperty(root, $"bq{i}"));
                    depth.SellQuantities.Add(GetIntProperty(root, $"sq{i}"));
                    depth.BuyOrders.Add(GetIntProperty(root, $"bo{i}"));
                    depth.SellOrders.Add(GetIntProperty(root, $"so{i}"));
                }

                return depth;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ParseDepthAcknowledgement error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse depth feed message
        /// </summary>
        private DepthData ParseDepthFeed(JsonElement root)
        {
            try
            {
                var depth = new DepthData
                {
                    MessageType = "df",
                    Exchange = GetStringProperty(root, "e"),
                    Token = GetStringProperty(root, "tk"),
                    LastPrice = GetDecimalProperty(root, "lp"),
                    PercentageChange = GetDecimalProperty(root, "pc"),
                    LastTradedQty = GetIntProperty(root, "ltq"),
                    TotalSellQty = GetLongProperty(root, "tsq"),
                    Timestamp = ParseTimestamp(GetStringProperty(root, "ft"))
                };

                // Parse available depth levels
                for (int i = 1; i <= 5; i++)
                {
                    if (root.TryGetProperty($"sq{i}", out _))
                        depth.SellQuantities.Add(GetIntProperty(root, $"sq{i}"));
                    if (root.TryGetProperty($"so{i}", out _))
                        depth.SellOrders.Add(GetIntProperty(root, $"so{i}"));
                }

                return depth;
            }
            catch (Exception ex)
            {
                _logger.LogError($"ParseDepthFeed error: {ex.Message}");
                return null;
            }
        }

        // Helper methods for safe property extraction
        private string GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
        }

        private decimal GetDecimalProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return decimal.TryParse(prop.GetString(), out var result) ? result : 0;
                else if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetDecimal();
            }
            return 0;
        }

        private long GetLongProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return long.TryParse(prop.GetString(), out var result) ? result : 0;
                else if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt64();
            }
            return 0;
        }

        private int GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                    return int.TryParse(prop.GetString(), out var result) ? result : 0;
                else if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
            }
            return 0;
        }

        private DateTime ParseTimestamp(string timestamp)
        {
            if (string.IsNullOrEmpty(timestamp))
                return DateTime.Now;

            // Unix timestamp format
            if (long.TryParse(timestamp, out var unixTime))
            {
                return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
            }

            return DateTime.Now;
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}