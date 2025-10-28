using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using TWS.Domain.Models;
using TWS.Infrastructure.Configuration;
using TWS.Infrastructure.Logging;
using TWS.Infrastructure.Utilities;
using TWS.Services.Interfaces;

namespace TWS.Services
{
    public class ScripMasterService : IScripMasterService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IConfigurationService _configService;
        private readonly MemoryCache<string, List<Scrip>> _cache;
        private readonly string _cacheDirectory;


        public ScripMasterService(
            HttpClient httpClient,
            ILogger logger,
            IConfigurationService configService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));

            var cacheHours = _configService.GetValue<int>("Cache:ScripCacheExpirationHours");
            _cache = new MemoryCache<string, List<Scrip>>(TimeSpan.FromHours(cacheHours > 0 ? cacheHours : 24));

            _cacheDirectory = _configService.GetValue<string>("Cache:CacheDirectory") ?? "Cache";
            EnsureCacheDirectoryExists();
        }

        public async Task<ScripDownloadResult> DownloadAllScripMastersAsync()
        {
            var result = new ScripDownloadResult { Success = true };
            var exchanges = new[] { "NSE", "BSE", "NFO" };

            _logger.LogInformation("Starting parallel scrip master download for all exchanges");

            try
            {
                var tasks = exchanges.Select(exchange => DownloadScripMasterAsync(exchange)).ToArray();
                var results = await Task.WhenAll(tasks);

                foreach (var r in results)
                {
                    result.TotalScripsLoaded += r.Count;
                    result.Messages.Add($"{r.Exchange}: {r.Count} scrips loaded");
                }

                result.Success = results.All(r => r.Success);
                _logger.LogInformation($"Scrip master download completed. Total scrips: {result.TotalScripsLoaded}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error downloading scrip masters", ex);
                result.Success = false;
                result.Messages.Add($"Error: {ex.Message}");
            }

            return result;
        }

        private async Task<ExchangeDownloadResult> DownloadScripMasterAsync(string exchange)
        {
            var result = new ExchangeDownloadResult { Exchange = exchange };

            try
            {
                _logger.LogInformation($"Downloading scrip master for {exchange}");

                // Check cache first
                var cacheKey = $"{exchange}_{DateTime.UtcNow:yyyyMMdd}";
                if (_cache.TryGet(cacheKey, out var cachedScrips))
                {
                    _logger.LogInformation($"Using cached scrips for {exchange}: {cachedScrips.Count}");
                    result.Success = true;
                    result.Count = cachedScrips.Count;
                    return result;
                }

                // Try to load from disk cache
                var cacheFile = GetCacheFilePath(exchange);
                if (File.Exists(cacheFile) && IsCacheValid(cacheFile))
                {
                    var scrips = await LoadFromCacheFileAsync(cacheFile);
                    _cache.Set(cacheKey, scrips);
                    _logger.LogInformation($"Loaded {scrips.Count} scrips from cache file for {exchange}");
                    result.Success = true;
                    result.Count = scrips.Count;
                    return result;
                }

                // Download from API
                var baseUrl = _configService.GetValue<string>("ApiEndpoints:BaseUrl");
                var endpoint = _configService.GetValue<string>($"ApiEndpoints:ScripMaster{exchange}");
                var url = $"{baseUrl}{endpoint}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var scripsFromApi = ParseScripMasterResponse(json, exchange);

                // Save to cache
                _cache.Set(cacheKey, scripsFromApi);
                await SaveToCacheFileAsync(cacheFile, scripsFromApi);

                result.Success = true;
                result.Count = scripsFromApi.Count;
                _logger.LogInformation($"Downloaded and cached {result.Count} scrips for {exchange}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error downloading scrip master for {exchange}", ex);
                result.Success = false;
            }

            return result;
        }

        public async Task<List<Scrip>> GetScripsByExchangeAsync(string exchange)
        {
            try
            {
                var cacheKey = $"{exchange}_{DateTime.UtcNow:yyyyMMdd}";
                if (_cache.TryGet(cacheKey, out var cachedScrips))
                {
                    return cachedScrips;
                }

                // Try to load from disk
                var cacheFile = GetCacheFilePath(exchange);
                if (File.Exists(cacheFile) && IsCacheValid(cacheFile))
                {
                    var scrips = await LoadFromCacheFileAsync(cacheFile);
                    _cache.Set(cacheKey, scrips);
                    return scrips;
                }

                // Download if not in cache
                await DownloadScripMasterAsync(exchange);

                if (_cache.TryGet(cacheKey, out var newScrips))
                {
                    return newScrips;
                }

                return new List<Scrip>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting scrips for {exchange}", ex);
                return new List<Scrip>();
            }
        }

        public async Task<List<Scrip>> GetAllScripsAsync()
        {
            try
            {
                var allScrips = new List<Scrip>();
                var exchanges = new[] { "NSE", "BSE", "NFO" };

                foreach (var exchange in exchanges)
                {
                    var scrips = await GetScripsByExchangeAsync(exchange);
                    allScrips.AddRange(scrips);
                }

                _logger.LogInformation($"Retrieved total {allScrips.Count} scrips from all exchanges");
                return allScrips;
            }
            catch (Exception ex)
            {
                _logger.LogError("Error getting all scrips", ex);
                return new List<Scrip>();
            }
        }

        public async Task<List<Scrip>> SearchScripsAsync(string searchText)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchText))
                    return new List<Scrip>();

                var allScrips = await GetAllScripsAsync();
                var searchUpper = searchText.ToUpper();

                return allScrips
                    .Where(s => s.Symbol.ToUpper().Contains(searchUpper) ||
                               (s.Name != null && s.Name.ToUpper().Contains(searchUpper)))
                    .Take(100)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error searching scrips: {searchText}", ex);
                return new List<Scrip>();
            }
        }

        public async Task<Scrip> GetScripAsync(string exchange, string token)
        {
            try
            {
                var scrips = await GetScripsByExchangeAsync(exchange);
                return scrips.FirstOrDefault(s => s.Token == token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting scrip {exchange}|{token}", ex);
                return null;
            }
        }

        public void ClearCache()
        {
            _cache.Clear();
            _logger.LogInformation("Scrip master cache cleared");
        }

        private List<Scrip> ParseScripMasterResponse(string json, string exchange)
        {
            try
            {
                var doc = JsonDocument.Parse(json);
                var scrips = new List<Scrip>();

                if (doc.RootElement.TryGetProperty(exchange, out var dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            scrips.Add(ParseScripItem(item, exchange));
                        }
                    }
                }

                return scrips;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing scrip master response for {exchange}", ex);
                return new List<Scrip>();
            }
        }

        private Scrip ParseScripItem(JsonElement item, string exchange)
        {
            return new Scrip
            {
                Token = GetJsonString(item, "token"),
                Symbol = GetJsonString(item, "symbol") ?? GetJsonString(item, "tradingSymbol"),
                Name = GetJsonString(item, "name") ?? GetJsonString(item, "companyName"),
                Exchange = exchange,
                ExchangeSegment = GetJsonString(item, "exchangeSegment"),
                InstrumentType = GetJsonString(item, "instrumentType"),
                ExpiryDate = GetJsonDateTime(item, "expiry"),
                StrikePrice = GetJsonDecimal(item, "strikePrice"),
                OptionType = GetJsonString(item, "optionType"),
                LotSize = GetJsonInt(item, "lotSize"),
                TickSize = GetJsonDecimal(item, "tickSize"),
                ISIN = GetJsonString(item, "isin")
            };
        }

        private string GetJsonString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) ? prop.GetString() : null;
        }

        private decimal GetJsonDecimal(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetDecimal() : 0m;
        }

        private int GetJsonInt(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number
                ? prop.GetInt32() : 0;
        }

        private DateTime? GetJsonDateTime(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                var str = prop.GetString();
                if (!string.IsNullOrWhiteSpace(str) && DateTime.TryParse(str, out var date))
                    return date;
            }
            return null;
        }

        private string GetCacheFilePath(string exchange)
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            return Path.Combine(_cacheDirectory, $"scrip_master_{exchange}_{date}.json");
        }

        private bool IsCacheValid(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            return fileInfo.Exists && (DateTime.UtcNow - fileInfo.LastWriteTimeUtc).TotalHours < 24;
        }

        private Task<List<Scrip>> LoadFromCacheFileAsync(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var result = JsonSerializer.Deserialize<List<Scrip>>(json) ?? new List<Scrip>();
            return Task.FromResult(result);
        }

        private Task SaveToCacheFileAsync(string filePath, List<Scrip> scrips)
        {
            var json = JsonSerializer.Serialize(scrips, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
            return Task.CompletedTask;
        }

        private void EnsureCacheDirectoryExists()
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        private class ExchangeDownloadResult
        {
            public string Exchange { get; set; }
            public bool Success { get; set; }
            public int Count { get; set; }
        }
    }
}