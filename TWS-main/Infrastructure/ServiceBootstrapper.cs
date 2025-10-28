// Infrastructure/DependencyInjection/ServiceBootstrapper.cs
using System;
using System.Net.Http;
using TWS.Infrastructure.Configuration;
using TWS.Infrastructure.Logging;
using TWS.Services;
using TWS.Services.Interfaces;

namespace TWS.Infrastructure.DependencyInjection
{
    public static class ServiceBootstrapper
    {
        public static ServiceContainer InitializeServices()
        {
            var container = new ServiceContainer();

            // Register infrastructure services first
            RegisterInfrastructure(container);

            // Register business services
            RegisterServices(container);

            return container;
        }

        //private static void RegisterInfrastructure(ServiceContainer container)
        //{
        //    // Configuration Service - Singleton
        //    container.RegisterSingleton<IConfigurationService>(c =>
        //        new ConfigurationService("appsettings.json"));

        //    // Logger - Singleton
        //    container.RegisterSingleton<ILogger>(c =>
        //    {
        //        var configService = c.GetService<IConfigurationService>();
        //        var logPath = configService.GetValue<string>("Logging:LogPath") ?? "Logs";
        //        var logLevel = configService.GetValue<string>("Logging:LogLevel") ?? "Information";
        //        return new FileLogger(logPath, logLevel);
        //    });

        //    // HttpClient - Singleton (IMPORTANT: Reuse HttpClient!)
        //    container.RegisterSingleton<HttpClient>(c =>
        //    {
        //        var client = new HttpClient();
        //        client.Timeout = TimeSpan.FromSeconds(30);
        //        return client;
        //    });
        //}

        private static void RegisterInfrastructure(ServiceContainer container)
        {
            // Configuration Service - Singleton
            container.RegisterSingleton<IConfigurationService>(c =>
                new ConfigurationService("appsettings.json"));

            // Logger - Singleton
            container.RegisterSingleton<ILogger>(c =>
            {
                var configService = c.GetService<IConfigurationService>();
                var logPath = configService.GetValue<string>("Logging:LogPath") ?? "Logs";
                var logLevelString = configService.GetValue<string>("Logging:LogLevel") ?? "Information";

                // ✅ FIX 1: Parse string to LogLevel enum
                LogLevel logLevel = LogLevel.Information; // Default
                if (Enum.TryParse<LogLevel>(logLevelString, true, out var parsedLevel))
                {
                    logLevel = parsedLevel;
                }

                return new FileLogger(logPath, logLevel);
            });

            // HttpClient - Singleton (IMPORTANT: Reuse HttpClient!)
            container.RegisterSingleton<HttpClient>(c =>
            {
                var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);
                return client;
            });
        }

        private static void RegisterServices(ServiceContainer container)
        {
            // ✅ FIX 2: Authentication Service - Include HttpClient and ConfigService
            container.RegisterSingleton<IAuthenticationService>(c =>
            {
                var httpClient = c.GetService<HttpClient>();
                var logger = c.GetService<ILogger>();
                var configService = c.GetService<IConfigurationService>();
                return new AuthenticationService(httpClient, logger, configService);
            });

            // Scrip Master Service - Singleton
            container.RegisterSingleton<IScripMasterService>(c =>
            {
                var httpClient = c.GetService<HttpClient>();
                var logger = c.GetService<ILogger>();
                var configService = c.GetService<IConfigurationService>();
                return new ScripMasterService(httpClient, logger, configService);
            });

            // Market Data Service - Singleton (needs AuthService for token)
            container.RegisterSingleton<IMarketDataService>(c =>
            {
                var logger = c.GetService<ILogger>();
                var configService = c.GetService<IConfigurationService>();
                var authService = c.GetService<IAuthenticationService>();
                return new MarketDataService(logger, configService, authService);
            });

            // Market Data Service - Singleton (needs AuthService for token)
            container.RegisterSingleton<IOrderService>(c =>
            {
                var httpClient = c.GetService<HttpClient>();
                var logger = c.GetService<ILogger>();
                var configService = c.GetService<IConfigurationService>();
                var authService = c.GetService<IAuthenticationService>();
                return new OrderService(httpClient, logger, configService, authService); ;
            });
            // Predefined Marketwatch Service - Singleton (needs AuthService for token)
            container.RegisterSingleton<IPredefinedMarketwatchService>(c =>
            {
                var httpClient = c.GetService<HttpClient>();
                var logger = c.GetService<ILogger>();
                var configService = c.GetService<IConfigurationService>();
                var authService = c.GetService<IAuthenticationService>();
                return new PredefinedMarketwatchService(httpClient, logger, configService, authService);
            });


        }

        //private static void RegisterServices(ServiceContainer container)
        //{
        //    // ✅ Authentication Service - Singleton (Register FIRST since others depend on it)
        //    container.RegisterSingleton<IAuthenticationService>(c =>
        //    {
        //        var logger = c.GetService<ILogger>();
        //        return new AuthenticationService(logger);
        //    });

        //    // ✅ Scrip Master Service - Singleton
        //    container.RegisterSingleton<IScripMasterService>(c =>
        //    {
        //        var httpClient = c.GetService<HttpClient>();
        //        var logger = c.GetService<ILogger>();
        //        var configService = c.GetService<IConfigurationService>();
        //        return new ScripMasterService(httpClient, logger, configService);
        //    });

        //    // ✅ Market Data Service - Singleton (needs AuthService for token)
        //    container.RegisterSingleton<IMarketDataService>(c =>
        //    {
        //        var logger = c.GetService<ILogger>();
        //        var configService = c.GetService<IConfigurationService>();
        //        var authService = c.GetService<IAuthenticationService>(); // ← Added!
        //        return new MarketDataService(logger, configService, authService);
        //    });

        //    // ✅ Predefined Marketwatch Service - Singleton (needs AuthService for token)
        //    container.RegisterSingleton<IPredefinedMarketwatchService>(c =>
        //    {
        //        var httpClient = c.GetService<HttpClient>();
        //        var logger = c.GetService<ILogger>();
        //        var configService = c.GetService<IConfigurationService>();
        //        var authService = c.GetService<IAuthenticationService>(); // ← Added!
        //        return new PredefinedMarketwatchService(httpClient, logger, configService, authService);
        //    });
        //}
    }
}