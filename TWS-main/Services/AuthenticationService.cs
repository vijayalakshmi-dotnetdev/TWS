using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TWS.Domain.Models;
using TWS.Infrastructure.Configuration;
using TWS.Infrastructure.Logging;
using TWS.Services.Interfaces;

namespace TWS.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfigurationService _configService;
        private readonly ILogger _logger;
        private string _currentAccessToken;
        private string _currentToken;

        public AuthenticationService(
            HttpClient httpClient,
            ILogger logger,
            IConfigurationService configService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<AuthenticationResult> LoginAsync(string userId, string password)
        {
            try
            {
                _logger.LogInformation($"Starting authentication for user: {userId}");

                // Step 1: Validate Password
                var passwordResult = await ValidatePasswordAsync(userId, password);
                if (!passwordResult.Success)
                {
                    return passwordResult;
                }

                // Store token for OTP step
                _currentToken = passwordResult.Token;

                _logger.LogInformation("Password validated. Ready for OTP.");

                return new AuthenticationResult
                {
                    Success = true,
                    Token = passwordResult.Token,
                    Message = "Password validated. Please enter OTP.",
                    RequiresOTP = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Authentication failed", ex);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = $"Authentication error: {ex.Message}"
                };
            }
        }

        public async Task<AuthenticationResult> RequestOTPAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentToken))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Please login first"
                    };
                }

                var sent = await SendOtpAsync(_currentToken, userId);

                return new AuthenticationResult
                {
                    Success = sent,
                    Message = sent ? "OTP sent successfully" : "Failed to send OTP",
                    Token = _currentToken
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("OTP request failed", ex);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = $"OTP request error: {ex.Message}"
                };
            }
        }

        public async Task<AuthenticationResult> ValidateOTPAsync(string userId, string otp, string deviceNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentToken))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Please login first"
                    };
                }

                var result = await ValidateOtpAsync(_currentToken, otp, deviceNumber, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("OTP validation failed", ex);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = $"OTP validation error: {ex.Message}"
                };
            }
        }

        public async Task<AuthenticationResult> ValidateOTPAsync(string token, string userId, string otp, string deviceNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentToken))
                {
                    return new AuthenticationResult
                    {
                        Success = false,
                        Message = "Please login first"
                    };
                }

                var result = await ValidateOtpAsync(token, otp, deviceNumber, userId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("OTP validation failed", ex);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = $"OTP validation error: {ex.Message}"
                };
            }
        }

        private async Task<AuthenticationResult> ValidatePasswordAsync(
            string userId,
            string password,
            CancellationToken cancellationToken = default)
        {
            // ✅ Use IConfigurationService instead of _settings
            var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
            var validatePasswordEndpoint = _configService.GetValue<string>("ApiEndpoints:ValidatePassword");
            var url = $"{baseUrl}{validatePasswordEndpoint}";

            var requestData = new
            {
                userId = userId,
                source = "WEB",
                password = password
            };

            var json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var responseObject = JObject.Parse(responseContent);

            var token = responseObject["result"]?[0]?["token"]?.ToString();

            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationResult
                {
                    Success = false,
                    Message = "Invalid credentials"
                };
            }

            return new AuthenticationResult
            {
                Success = true,
                Token = token
            };
        }

        private async Task<bool> SendOtpAsync(
            string token,
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ✅ Use IConfigurationService
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var sendOtpEndpoint = _configService.GetValue<string>("ApiEndpoints:SendOtp");
                var url = $"{baseUrl}{sendOtpEndpoint}";

                var requestData = new
                {
                    userId = userId,
                    source = "WEB"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {userId} WEB {token}");
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("OTP sent successfully");
                    return true;
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send OTP: {errorContent}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error sending OTP", ex);
                return false;
            }
        }

        private async Task<AuthenticationResult> ValidateOtpAsync(
            string token,
            string otp,
            string deviceNumber,
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ✅ Use IConfigurationService
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var validateOtpEndpoint = _configService.GetValue<string>("ApiEndpoints:ValidateOtp");
                var url = $"{baseUrl}{validateOtpEndpoint}";

                var requestData = new
                {
                    userId = userId,
                    source = "WEB",
                    otp = otp,
                    deviceNumber = deviceNumber
                };

                // Create HttpClient instance
                using (var client = new HttpClient())
                {
                    // Set headers                    
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {userId} WEB {token}");
                    // client.DefaultRequestHeaders.Add("TokenHeader", $"Bearer {tokenHeader}");


                    // Serialize the request body to JSON
                    var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    // Send POST request
                    var response = await client.PostAsync(url, content);

                    // Ensure the response is successful
                    response.EnsureSuccessStatusCode();

                    // Read the response content
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        return new AuthenticationResult
                        {
                            Success = false,
                            Message = "Failed to retrieve access token"
                        };
                    }

                    var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);

                    var accessToken = apiResponse?.Result?[0]?.AccessToken;

                    if (string.IsNullOrEmpty(accessToken))
                    {
                        return new AuthenticationResult
                        {
                            Success = false,
                            Message = "Failed to retrieve access token"
                        };
                    }

                    _currentAccessToken = accessToken;

                    // Invalidate and create WebSocket session
                    await InvalidateWebSocketSessionAsync(accessToken, userId, cancellationToken);
                    var wsToken = await CreateWebSocketSessionAsync(accessToken, userId, cancellationToken);

                    _logger.LogInformation("OTP validated successfully");

                    return new AuthenticationResult
                    {
                        Success = true,
                        Token = accessToken,
                        Message = "Authentication successful",
                        ExpiresAt = DateTime.Now.AddHours(8)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("OTP validation failed", ex);
                return new AuthenticationResult
                {
                    Success = false,
                    Message = $"OTP validation error: {ex.Message}"
                };
            }
        }

     //   private async Task<AuthenticationResult> ValidateOtpAsync(
     //string token,
     //string otp,
     //string deviceNumber,
     //string userId,
     //CancellationToken cancellationToken = default)
     //   {
     //       // 🧪 EXACT COPY OF OLD CODE
     //       string baseUrl = "https://web.gopocket.in/am/access/otp/validate";
     //       string authorizationToken = token;

     //       var requestBody = new
     //       {
     //           userId = "SKYWS1",
     //           source = "WEB",
     //           otp = "123789",  // Hardcoded from working code
     //           deviceNumber = "69c8a62f5e739c5b9bdd620653e4a03e"  // Hardcoded from working code
     //       };
     //       using (var client = new HttpClient())
     //       {
     //           // 🔍 BUILD THE HEADER
     //           var authHeader = $"Bearer SKYWS1 WEB {token}";

     //           // 🔍 LOG THE HEADER BEFORE ADDING
     //           _logger.LogInformation($"===== HEADER CHECK =====");
     //           _logger.LogInformation($"Token parameter: [{token}]");
     //           _logger.LogInformation($"Token is null: {token == null}");
     //           _logger.LogInformation($"Token is empty: {string.IsNullOrEmpty(token)}");
     //           _logger.LogInformation($"Token length: {token?.Length ?? 0}");
     //           _logger.LogInformation($"Authorization header: [{authHeader}]");
     //           _logger.LogInformation($"========================");

     //           client.DefaultRequestHeaders.Add("Authorization", authHeader);

     //           var jsonContent = JsonConvert.SerializeObject(requestBody);
     //           var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

     //           _logger.LogInformation($"Sending request: {jsonContent}");

     //           var response = await client.PostAsync(baseUrl, content);
     //           string responseContent = await response.Content.ReadAsStringAsync();

     //           _logger.LogInformation($"Response: {responseContent}");

     //        if (!response.IsSuccessStatusCode)
     //           {
     //               return new AuthenticationResult { Success = false, Message = responseContent };
     //           }

     //           var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(responseContent);
     //           var accessToken = apiResponse?.Result?[0]?.AccessToken;

     //           return new AuthenticationResult
     //           {
     //               Success = true,
     //               Token = accessToken,
     //               Message = "Success"
     //           };
     //       }
     //   }
        private async Task<bool> InvalidateWebSocketSessionAsync(
            string accessToken,
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ✅ Use IConfigurationService
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var invalidateEndpoint = _configService.GetValue<string>("ApiEndpoints:InvalidateWsSession");
                var url = $"{baseUrl}{invalidateEndpoint}";

                var requestData = new
                {
                    userId = userId,
                    source = "WEB"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to invalidate WebSocket session", ex);
                return false;
            }
        }

        private async Task<string> CreateWebSocketSessionAsync(
            string accessToken,
            string userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // ✅ Use IConfigurationService
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var createSessionEndpoint = _configService.GetValue<string>("ApiEndpoints:CreateWsSession");
                var url = $"{baseUrl}{createSessionEndpoint}";

                var requestData = new
                {
                    userId = userId,
                    source = "WEB"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(requestData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("WebSocket session created successfully");

                return accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create WebSocket session", ex);
                throw;
            }
        }

        public string GetAuthToken()
        {
            return _currentAccessToken;
        }

        public bool IsAuthenticated()
        {
            return !string.IsNullOrEmpty(_currentAccessToken);
        }

        public void Logout()
        {
            _currentAccessToken = null;
            _currentToken = null;
            _logger.LogInformation("User logged out");
        }
    }

    // Helper classes for API response deserialization
    public class ApiResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public Result[] Result { get; set; }
    }

    public class Result
    {
        public string AccessToken { get; set; }
        public string KcRole { get; set; }
        public bool Authorized { get; set; }
    }
}