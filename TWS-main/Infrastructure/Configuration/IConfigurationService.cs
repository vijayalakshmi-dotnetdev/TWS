using System;

namespace TWS.Infrastructure.Configuration
{
    /// <summary>
    /// Service for reading application configuration
    /// </summary>
    public interface IConfigurationService : IDisposable
    {
        /// <summary>
        /// Gets a configuration value by key with type conversion
        /// </summary>
        T GetValue<T>(string key);

        /// <summary>
        /// Gets a configuration value by key as string
        /// </summary>
        string GetValue(string key);

        /// <summary>
        /// Gets a configuration section as a strongly-typed object
        /// </summary>
        T GetSection<T>(string sectionName) where T : class, new();

        /// <summary>
        /// Reloads configuration from file
        /// </summary>
        void Reload();
    }
}