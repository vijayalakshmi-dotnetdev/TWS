using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TWS.Domain.Models;
using TWS.Infrastructure.Configuration;
using TWS.Infrastructure.Logging;
using TWS.Services.Interfaces;
using static TWS.Services.TCPConnectionHelper;

namespace TWS.Services
{
    /// <summary>
    /// Service for order management operations with integrated authentication
    /// </summary>
    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IAuthenticationService _authService;
        private readonly IConfigurationService _configService;
        private readonly string _baseUrl;
        private readonly TcpConnectionManager _tcpManager;

        public OrderService(
            HttpClient httpClient,
            ILogger logger,
            IConfigurationService configService,
            IAuthenticationService authService) // ← Added parameter
        {
            _httpClient = httpClient;
            _logger = logger;
            _configService = configService;
            _authService = authService; // ← Store it
            _baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
            _tcpManager = new TcpConnectionManager("127.0.0.1", 7004);
        }

        /// <summary>
        /// Adds authentication headers to the request
        /// </summary>
        private void AddAuthenticationHeaders()
        {
            var authResult = _authService.GetAuthToken();

            if (authResult == null || string.IsNullOrEmpty(authResult))
            {
                throw new InvalidOperationException("User is not authenticated. Please login first.");
            }

            // Clear existing auth headers to avoid duplicates
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            //_httpClient.DefaultRequestHeaders.Remove("jwtToken");
            //_httpClient.DefaultRequestHeaders.Remove("userId");

            // Add authentication headers based on your API requirements
            // Adjust these based on your actual API header requirements
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {authResult}");
          //  _httpClient.DefaultRequestHeaders.Add("jwtToken", authResult);

            //if (!string.IsNullOrEmpty(authResult.UserId))
            //{
            //    _httpClient.DefaultRequestHeaders.Add("userId", authResult.UserId);
            //}

           // _logger.LogInfo($"Authentication headers added for user: {authResult.UserId}");
        }

        /// <summary>
        /// Places a new order
        /// CRITICAL: The API expects an ARRAY with a single order object
        /// Endpoint: POST /orders/web/execute
        /// </summary>
        public async Task<PlaceOrderResult> PlaceOrderAsync(OrderRequest request)
        {
            try
            {
                _logger.LogInformation($"Placing order: {request.tradingsymbol} {request.transType} {request.qty}@{request.price}");

                // Add authentication headers
                AddAuthenticationHeaders();

                // CRITICAL: Wrap the request in an array - API expects [{...}] not {...}
                var requestArray = new[] { request };
                var json = JsonSerializer.Serialize(requestArray);
                    /*, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });*/

                _logger.LogInformation($"Order request payload: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var hardcoded = "[\r\n    {\r\n        \"exchange\": \"NSE\",\r\n        \"qty\": \"1\",\r\n        \"price\": \"7.45\",\r\n        \"product\": \"MIS\",\r\n        \"transType\": \"B\",\r\n        \"priceType\": \"L\",\r\n        \"triggerPrice\": \"0\",\r\n        \"ret\": \"DAY\",\r\n        \"disclosedQty\": \"0\",\r\n        \"mktProtection\": \"\",\r\n        \"target\": \"0\",\r\n        \"stopLoss\": \"0\",\r\n        \"orderType\": \"Regular\",\r\n        \"token\": \"14366\",\r\n        \"source\": \"WEB\",\r\n        \"tradingSymbol\": \"IDEA_EQ\"\r\n    }\r\n]";

                // CRITICAL: Use correct endpoint - /orders/web/execute not /orders/execute
                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/web/execute", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Place order response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Place order failed with status {response.StatusCode}: {responseContent}");

                    // Check if it's an authentication error
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired or invalid. Please login again.");
                    }

                    throw new HttpRequestException($"Order placement failed: {response.StatusCode} - {responseContent}");
                }

                var result = JsonSerializer.Deserialize<PlaceOrderResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Order placed successfully: {result.Result[0].OrderNo}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to place order", ex);
                throw;
            }
        }

        /// <summary>
        /// Modifies an existing order
        /// Endpoint: POST /orders/modify
        /// </summary>
        /// 
        public async Task<PlaceOrderResult> PlaceTcpOrderAsync(OrderRequest request)
        {
            var requestPacket = new
            {
                PacketType = "PLACE_ORDER",
                Data = request
            };

            var json = JsonSerializer.Serialize(requestPacket);

            string rawResponse = await _tcpManager.SendAndReceiveAsync(json);

            var result = JsonSerializer.Deserialize<PlaceOrderResult>(rawResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return result;
        }
        public async Task<ModifyOrderResult> ModifyOrderAsync(ModifyOrderRequest request)
        {
            try
            {
                _logger.LogInformation($"Modifying order: {request.OrderNo}");

                AddAuthenticationHeaders();

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/modify", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Modify order response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Modify order failed: {responseContent}");
                    throw new HttpRequestException($"Order modification failed: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ModifyOrderResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Order modified successfully: {request.OrderNo}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to modify order", ex);
                throw;
            }
        }

        /// <summary>
        /// Cancels an existing order
        /// Endpoint: POST /orders/cancel
        /// </summary>
        public async Task<CancelOrderResult> CancelOrderAsync(string orderNo)
        {
            try
            {
                _logger.LogInformation($"Canceling order: {orderNo}");

                AddAuthenticationHeaders();

                var request = new { orderNo = orderNo };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/cancel", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Cancel order response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Cancel order failed: {responseContent}");
                    throw new HttpRequestException($"Order cancellation failed: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<CancelOrderResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Order canceled successfully: {orderNo}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to cancel order", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the order book for the current day
        /// Endpoint: GET /info/orderbook
        /// </summary>
        public async Task<List<OrderBookItem>> GetOrderBookAsync()
        {
            try
            {
                _logger.LogInformation("Fetching order book");

                AddAuthenticationHeaders();

                var response = await _httpClient.GetAsync($"{_baseUrl}/info/orderbook");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Get order book failed: {responseContent}");
                    throw new HttpRequestException($"Failed to get order book: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ApiResponse<List<OrderBookItem>>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Order book fetched: {result.Result.Count} orders");
                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch order book", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets order history for a specific order
        /// Endpoint: POST /info/history
        /// </summary>
        public async Task<List<OrderHistoryItem>> GetOrderHistoryAsync(string orderNo)
        {
            try
            {
                _logger.LogInformation($"Fetching order history for: {orderNo}");

                AddAuthenticationHeaders();

                var request = new { orderNo = orderNo };
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_baseUrl}/info/history", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Get order history failed: {responseContent}");
                    throw new HttpRequestException($"Failed to get order history: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ApiResponse<List<OrderHistoryItem>>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Order history fetched: {result.Result.Count} records");
                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch order history", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets the trade book for the current day
        /// Endpoint: GET /info/tradebook
        /// </summary>
        public async Task<List<TradeBookItem>> GetTradeBookAsync()
        {
            try
            {
                _logger.LogInformation("Fetching trade book");

                AddAuthenticationHeaders();

                var response = await _httpClient.GetAsync($"{_baseUrl}/info/tradebook");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Get trade book failed: {responseContent}");
                    throw new HttpRequestException($"Failed to get trade book: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ApiResponse<List<TradeBookItem>>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Trade book fetched: {result.Result.Count} trades");
                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch trade book", ex);
                throw;
            }
        }

        /// <summary>
        /// Calculates margin required for an order
        /// Endpoint: POST /orders/getmargin
        /// </summary>
        public async Task<MarginResult> GetOrderMarginAsync(MarginRequest request)
        {
            try
            {
                _logger.LogInformation($"Calculating margin for: {request.TradingSymbol}");

                AddAuthenticationHeaders();

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/getmargin", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Get margin failed: {responseContent}");
                    throw new HttpRequestException($"Failed to get margin: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<MarginResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"Margin calculated: {result.Result[0].MarginUsed}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to calculate margin", ex);
                throw;
            }
        }

        /// <summary>
        /// Places a GTT (Good Till Triggered) order
        /// Endpoint: POST /orders/gtt/execute
        /// </summary>
        public async Task<PlaceOrderResult> PlaceGTTOrderAsync(PlaceGTTOrderRequest request)
        {
            try
            {
                _logger.LogInformation($"Placing GTT order: {request.TradingSymbol} @ trigger {request.GttValue}");

                AddAuthenticationHeaders();

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/gtt/execute", content);

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Place GTT order response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Place GTT order failed: {responseContent}");
                    throw new HttpRequestException($"GTT order placement failed: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<PlaceOrderResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"GTT order placed successfully: {result.Result[0].OrderNo}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to place GTT order", ex);
                throw;
            }
        }

        /// <summary>
        /// Modifies an existing GTT order
        /// Endpoint: POST /orders/gtt/modify
        /// </summary>
        public async Task<ModifyOrderResult> ModifyGTTOrderAsync(ModifyGTTOrderRequest request)
        {
            try
            {
                _logger.LogInformation($"Modifying GTT order: {request.OrderNo}");

                AddAuthenticationHeaders();

                var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}/orders/gtt/modify", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Modify GTT order failed: {responseContent}");
                    throw new HttpRequestException($"GTT order modification failed: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ModifyOrderResult>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation($"GTT order modified successfully: {request.OrderNo}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to modify GTT order", ex);
                throw;
            }
        }

        /// <summary>
        /// Gets all GTT orders
        /// Endpoint: GET /info/gtt/orderbook
        /// </summary>
        public async Task<List<GTTOrderItem>> GetGTTOrderBookAsync()
        {
            try
            {
                _logger.LogInformation("Fetching GTT order book");

                AddAuthenticationHeaders();

                var response = await _httpClient.GetAsync($"{_baseUrl}/info/gtt/orderbook");
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        throw new UnauthorizedAccessException("Authentication token expired. Please login again.");
                    }

                    _logger.LogError($"Get GTT order book failed: {responseContent}");
                    throw new HttpRequestException($"Failed to get GTT order book: {response.StatusCode}");
                }

                var result = JsonSerializer.Deserialize<ApiResponse<List<GTTOrderItem>>>(
                    responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"GTT order book fetched: {result.Result.Count} orders");
                return result.Result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to fetch GTT order book", ex);
                throw;
            }
        }

        // Helper class for deserializing API responses
        private class ApiResponse<T>
        {
            public string Status { get; set; }
            public string Message { get; set; }
            public T Result { get; set; }
        }
    }
}