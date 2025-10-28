using System.Collections.Generic;
using System.Threading.Tasks;
using TWS.Domain.Models;

namespace TWS.Services.Interfaces
{
    /// <summary>
    /// Service for managing scrip master data
    /// </summary>
    public interface IScripMasterService
    {
        /// <summary>
        /// Downloads scrip masters for all exchanges
        /// </summary>
        Task<ScripDownloadResult> DownloadAllScripMastersAsync();

        /// <summary>
        /// Gets scrips for a specific exchange
        /// </summary>
        Task<List<Scrip>> GetScripsByExchangeAsync(string exchange);

        /// <summary>
        /// Gets all scrips from all exchanges
        /// </summary>
        Task<List<Scrip>> GetAllScripsAsync();

        /// <summary>
        /// Searches scrips by symbol or name
        /// </summary>
        Task<List<Scrip>> SearchScripsAsync(string searchText);

        /// <summary>
        /// Gets a specific scrip by token and exchange
        /// </summary>
        Task<Scrip> GetScripAsync(string exchange, string token);

        /// <summary>
        /// Clears the scrip cache
        /// </summary>
        void ClearCache();
    }
}