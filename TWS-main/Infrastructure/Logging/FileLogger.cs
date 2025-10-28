using System;
using System.IO;
using TWS.Services.Interfaces;

namespace TWS.Infrastructure.Logging
{
    /// <summary>
    /// File-based logger implementation
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly string _logPath;
        private readonly LogLevel _logLevel;
        private readonly object _lockObject = new object();

        /// <summary>
        /// Creates a new file logger
        /// </summary>
        /// <param name="logPath">Directory path for log files</param>
        /// <param name="logLevel">Minimum log level to write</param>
        public FileLogger(string logPath, LogLevel logLevel)
        {
            _logPath = logPath ?? throw new ArgumentNullException(nameof(logPath));
            _logLevel = logLevel;

            // Ensure log directory exists
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
        }

        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, message, null);
        }

        public void LogInformation(string message)
        {
            Log(LogLevel.Information, message, null);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Warning, message, null);
        }

        public void LogError(string message, Exception exception = null)
        {
            Log(LogLevel.Error, message, exception);
        }

        public void LogCritical(string message, Exception exception = null)
        {
            Log(LogLevel.Critical, message, exception);
        }

        private void Log(LogLevel level, string message, Exception exception)
        {
            // Check if we should log this level
            if (level < _logLevel)
                return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {message}";

                if (exception != null)
                {
                    logEntry += $"\nException: {exception.GetType().Name}\nMessage: {exception.Message}\nStackTrace: {exception.StackTrace}";

                    if (exception.InnerException != null)
                    {
                        logEntry += $"\nInner Exception: {exception.InnerException.Message}";
                    }
                }

                var logFileName = GetLogFileName();

                lock (_lockObject)
                {
                    File.AppendAllText(logFileName, logEntry + Environment.NewLine);
                }
            }
            catch
            {
                // Suppress logging errors to prevent application crashes
            }
        }

        private string GetLogFileName()
        {
            var dateString = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(_logPath, $"tws_{dateString}.log");
        }

        public void Dispose()
        {
            // Nothing to dispose in file logger
        }
    }
}