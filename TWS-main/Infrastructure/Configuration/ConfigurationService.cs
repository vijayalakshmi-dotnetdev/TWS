using System;
using System.IO;
using System.Text.Json;

namespace TWS.Infrastructure.Configuration
{
    public class ConfigurationService : IConfigurationService
    {
        private readonly string _configFilePath;
        private JsonDocument _configDocument;
        private DateTime _lastLoadTime;

        public ConfigurationService() : this("appsettings.json")
        {
        }

        public ConfigurationService(string configFilePath)
        {
            _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (!File.Exists(_configFilePath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {_configFilePath}");
                }

                var json = File.ReadAllText(_configFilePath);
                _configDocument = JsonDocument.Parse(json);
                _lastLoadTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load configuration from {_configFilePath}", ex);
            }
        }

        public T GetValue<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            try
            {
                var keys = key.Split(':');
                JsonElement element = _configDocument.RootElement;

                foreach (var k in keys)
                {
                    if (!element.TryGetProperty(k, out element))
                    {
                        return default(T);
                    }
                }

                // Handle different types
                if (typeof(T) == typeof(string))
                    return (T)(object)element.GetString();
                if (typeof(T) == typeof(int))
                    return (T)(object)element.GetInt32();
                if (typeof(T) == typeof(long))
                    return (T)(object)element.GetInt64();
                if (typeof(T) == typeof(bool))
                    return (T)(object)element.GetBoolean();
                if (typeof(T) == typeof(double))
                    return (T)(object)element.GetDouble();
                if (typeof(T) == typeof(decimal))
                    return (T)(object)element.GetDecimal();

                // For complex types, deserialize
                return JsonSerializer.Deserialize<T>(element.GetRawText());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get configuration value for key: {key}", ex);
            }
        }

        public string GetValue(string key)
        {
            return GetValue<string>(key);
        }

        public T GetSection<T>(string sectionName) where T : class, new()
        {
            if (string.IsNullOrWhiteSpace(sectionName))
                throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));

            try
            {
                if (!_configDocument.RootElement.TryGetProperty(sectionName, out var sectionElement))
                {
                    return new T(); // Return default instance if section not found
                }

                return JsonSerializer.Deserialize<T>(sectionElement.GetRawText());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to deserialize section: {sectionName}", ex);
            }
        }

        public void Reload()
        {
            _configDocument?.Dispose();
            LoadConfiguration();
        }

        public void Dispose()
        {
            _configDocument?.Dispose();
        }
    }
}