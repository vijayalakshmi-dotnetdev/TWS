// Services/PredefinedMarketwatchService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TWS.Domain.Models;
using TWS.Infrastructure.Configuration;
using TWS.Services.Interfaces;

namespace TWS.Services
{
    public class PredefinedMarketwatchService : IPredefinedMarketwatchService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IConfigurationService _configService;
        private readonly IAuthenticationService _authService; // ← Added

        // ✅ Updated constructor to include IAuthenticationService
        public PredefinedMarketwatchService(
            HttpClient httpClient,
            ILogger logger,
            IConfigurationService configService,
            IAuthenticationService authService) // ← Added parameter
        {
            _httpClient = httpClient;
            _logger = logger;
            _configService = configService;
            _authService = authService; // ← Store it
        }

        public async Task<List<PredefinedMarketwatch>> GetPredefinedMarketwatchesAsync()
        {
            try
            {
                // ✅ Get the access token from auth service
                var accessToken = _authService.GetAuthToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Cannot fetch marketwatches: No access token available");
                    return new List<PredefinedMarketwatch>();
                }

                // ✅ Use IConfigurationService instead of _settings
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var PredefinedMarketwatchEndpoint = _configService.GetValue<string>("ApiEndpoints:PredefinedMarketwatch");
                var apiUrl = $"{baseUrl}{PredefinedMarketwatchEndpoint}";

               // var apiUrl = _configService.GetValue<string>("ApiEndpoints:PredefinedMarketwatch");

                if (string.IsNullOrEmpty(apiUrl))
                {
                    _logger.LogError("PredefinedMarketwatch API URL not configured");
                    return new List<PredefinedMarketwatch>();
                }

                _logger.LogInformation($"Fetching predefined marketwatches from: {apiUrl}");

                // ✅ Create request with authorization header
                var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response Status: {response.StatusCode}");
                _logger.LogInformation($"Response: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch marketwatches: {response.StatusCode}");
                    return new List<PredefinedMarketwatch>();
                }

                var apiResponse = JsonConvert.DeserializeObject<MarketwatchApiResponse>(responseContent);

                if (apiResponse?.Status?.ToLower() != "ok")
                {
                    _logger.LogWarning($"API returned non-OK status: {apiResponse?.Message}");
                    return new List<PredefinedMarketwatch>();
                }

                var marketwatches = new List<PredefinedMarketwatch>();

                if (apiResponse.Result != null)
                {
                    foreach (var item in apiResponse.Result)
                    {
                        marketwatches.Add(new PredefinedMarketwatch
                        {
                            Id = item.Id,
                            Name = item.Name,
                            Category = item.Category,
                            ScripCount = item.ScripCount
                        });
                    }
                }

                _logger.LogInformation($"Successfully loaded {marketwatches.Count} predefined marketwatches");

                return marketwatches;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching predefined marketwatches: {ex.Message}", ex);
                return new List<PredefinedMarketwatch>();
            }
        }

        public async Task<List<Scrip>> GetMarketwatchScripsAsync(string marketwatchId)
        {
            try
            {
                // ✅ Get the access token from auth service
                var accessToken = _authService.GetAuthToken();

                if (string.IsNullOrEmpty(accessToken))
                {
                    _logger.LogError("Cannot fetch marketwatch scrips: No access token available");
                    return new List<Scrip>();
                }

                var apiUrl = _configService.GetValue<string>("ApiEndpoints:MarketwatchScrips");

                if (string.IsNullOrEmpty(apiUrl))
                {
                    _logger.LogError("MarketwatchScrips API URL not configured");
                    return new List<Scrip>();
                }

                // Replace {id} placeholder with actual marketwatch ID
                apiUrl = apiUrl.Replace("{id}", marketwatchId);

                _logger.LogInformation($"Fetching scrips for marketwatch {marketwatchId}");

                // ✅ Create request with authorization header
                var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                request.Headers.Add("Authorization", $"Bearer {accessToken}");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"Response Status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to fetch marketwatch scrips: {response.StatusCode}");
                    return new List<Scrip>();
                }

                var apiResponse = JsonConvert.DeserializeObject<ScripsApiResponse>(responseContent);

                if (apiResponse?.Status?.ToLower() != "ok")
                {
                    _logger.LogWarning($"API returned non-OK status: {apiResponse?.Message}");
                    return new List<Scrip>();
                }

                var scrips = new List<Scrip>();

                if (apiResponse.Result != null)
                {
                    foreach (var item in apiResponse.Result)
                    {
                        scrips.Add(new Scrip
                        {
                            ScripCode = item.ScripCode,
                            ScripName = item.ScripName,
                            Exchange = item.Exchange,
                            Segment = item.Segment
                        });
                    }
                }

                _logger.LogInformation($"Successfully loaded {scrips.Count} scrips");

                return scrips;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error fetching marketwatch scrips: {ex.Message}", ex);
                return new List<Scrip>();
            }
        }

        // ===== RESPONSE MODELS =====

        private class MarketwatchApiResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("result")]
            public MarketwatchItem[] Result { get; set; }
        }

        private class MarketwatchItem
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

            [JsonProperty("scripCount")]
            public int ScripCount { get; set; }
        }

        private class ScripsApiResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("result")]
            public ScripItem[] Result { get; set; }
        }

        private class ScripItem
        {
            [JsonProperty("scripCode")]
            public string ScripCode { get; set; }

            [JsonProperty("scripName")]
            public string ScripName { get; set; }

            [JsonProperty("exchange")]
            public string Exchange { get; set; }

            [JsonProperty("segment")]
            public string Segment { get; set; }
        }
    }
}