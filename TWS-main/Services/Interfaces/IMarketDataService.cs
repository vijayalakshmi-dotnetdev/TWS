// Services/Interfaces/IMarketDataService.cs
using System;
using System.Threading.Tasks;
using TWS.Domain.Models;

namespace TWS.Services.Interfaces
{
    public interface IMarketDataService : IDisposable
    {
        /// <summary>
        /// Event triggered when tick data (market data) is received
        /// </summary>
        event EventHandler<TickData> OnTickReceived;

        /// <summary>
        /// Event triggered when depth data is received
        /// </summary>
        event EventHandler<DepthData> OnDepthReceived;

        /// <summary>
        /// Event triggered when an error occurs
        /// </summary>
        event EventHandler<string> OnError;

        /// <summary>
        /// Event triggered when WebSocket connection is established
        /// </summary>
        event EventHandler OnConnected;

        /// <summary>
        /// Event triggered when WebSocket connection is closed
        /// </summary>
        event EventHandler OnDisconnected;

        /// <summary>
        /// Connect to WebSocket server
        /// Creates session, authenticates, and starts heartbeat
        /// </summary>
        Task ConnectAsync();

        /// <summary>
        /// Subscribe to market data (LTP, OHLC, Volume) for specified scrips
        /// </summary>
        /// <param name="scripCodes">Array of scrip codes in format "EXCHANGE|TOKEN" (e.g., "NSE|11536", "NFO|54957")</param>
        Task SubscribeAsync(string[] scripCodes);

        /// <summary>
        /// Subscribe to depth data (5-level market depth) for specified scrips
        /// </summary>
        /// <param name="scripCodes">Array of scrip codes in format "EXCHANGE|TOKEN"</param>
        Task SubscribeDepthAsync(string[] scripCodes);

        /// <summary>
        /// Unsubscribe from market data for specified scrips
        /// </summary>
        /// <param name="scripCodes">Array of scrip codes to unsubscribe from</param>
        Task UnsubscribeAsync(string[] scripCodes);

        /// <summary>
        /// Unsubscribe from depth data for specified scrips
        /// </summary>
        /// <param name="scripCodes">Array of scrip codes to unsubscribe from</param>
        Task UnsubscribeDepthAsync(string[] scripCodes);

        /// <summary>
        /// Disconnect from WebSocket server
        /// Stops heartbeat and closes connection
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Gets the connection status
        /// </summary>
        bool IsConnected { get; }
    }
}