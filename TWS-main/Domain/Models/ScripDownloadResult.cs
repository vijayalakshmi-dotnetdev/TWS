using System;
using System.Collections.Generic;

namespace TWS.Domain.Models
{
    /// <summary>
    /// Result of scrip master download operation
    /// </summary>
    public class ScripDownloadResult
    {
        /// <summary>
        /// Whether the download was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total number of scrips loaded across all exchanges
        /// </summary>
        public int TotalScripsLoaded { get; set; }

        /// <summary>
        /// Detailed messages about the download process
        /// </summary>
        public List<string> Messages { get; set; }

        /// <summary>
        /// Error message if the download failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Time taken to download
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Timestamp when download completed
        /// </summary>
        public DateTime CompletedAt { get; set; }

        /// <summary>
        /// Number of exchanges processed
        /// </summary>
        public int ExchangesProcessed { get; set; }

        /// <summary>
        /// Whether data was loaded from cache
        /// </summary>
        public bool LoadedFromCache { get; set; }

        public ScripDownloadResult()
        {
            Messages = new List<string>();
            CompletedAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return Success
                ? $"Success: {TotalScripsLoaded:N0} scrips loaded from {ExchangesProcessed} exchanges"
                : $"Failed: {ErrorMessage}";
        }
    }
}