
namespace TWS.Infrastructure.Configuration
{
    /// <summary>
    /// Application settings
    /// </summary>
    public class AppSettings
    {
        public ApiEndpoints ApiEndpoints { get; set; }
        public WebSocketSettings WebSocket { get; set; }
        public CacheSettings Cache { get; set; }
        public LoggingSettings Logging { get; set; }
        public PerformanceSettings Performance { get; set; }
        public UISettings UI { get; set; }
    }

    /// <summary>
    /// API endpoint configuration
    /// </summary>
    public class ApiEndpoints
    {
        public string BaseUrl { get; set; }
        public string Login { get; set; }
        public string RequestOTP { get; set; }
        public string ValidateOTP { get; set; }
        public object ValidateOtp { get; internal set; }
        public string ScripMasterNSE { get; set; }
        public string ScripMasterBSE { get; set; }
        public string ScripMasterNFO { get; set; }
        public string WebSocketUrl { get; set; }
        public string PredefinedMarketwatch { get; set; }
        public string GetMarketwatch { get; set; }
        public string SaveMarketwatch { get; set; }
        public string GetMarketwatchList { get; set; }
        public string DeleteMarketwatch { get; set; }
        public object ValidatePassword { get;  set; }
        public object SendOtp { get;  set; }
        public object InvalidateWsSession { get; internal set; }
        public object CreateWsSession { get; internal set; }
    }

    /// <summary>
    /// WebSocket configuration
    /// </summary>
    public class WebSocketSettings
    {
        public string Url { get; set; }
        public int ReconnectDelayMs { get; set; }
        public int MaxReconnectAttempts { get; set; }
        public int HeartbeatIntervalMs { get; set; }
        public int ConnectionTimeoutMs { get; set; }
    }


    /// <summary>
    /// Cache configuration
    /// </summary>
    public class CacheSettings
    {
        public int ScripCacheExpirationHours { get; set; }
        public string CacheDirectory { get; set; }
    }

    /// <summary>
    /// Logging configuration
    /// </summary>
    public class LoggingSettings
    {
        public string LogLevel { get; set; }
        public string LogFilePath { get; set; }
        public int MaxLogFileSizeMB { get; set; }
    }

    /// <summary>
    /// Performance configuration
    /// </summary>
    public class PerformanceSettings
    {
        public int ParallelDownloadThreads { get; set; }
        public int MaxConcurrentWebSocketSubscriptions { get; set; }
        public int TickProcessingBatchSize { get; set; }
    }

    /// <summary>
    /// UI configuration
    /// </summary>
    public class UISettings
    {
        public string DefaultTheme { get; set; }
        public int AutoRefreshIntervalSeconds { get; set; }
        public bool ShowSplashScreen { get; set; }
    }
}