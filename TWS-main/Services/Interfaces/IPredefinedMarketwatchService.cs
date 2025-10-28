// Services/Interfaces/IPredefinedMarketwatchService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using TWS.Domain.Models;

namespace TWS.Services.Interfaces
{
    /// <summary>
    /// Service for managing predefined marketwatches
    /// </summary>
    public interface IPredefinedMarketwatchService
    {
        /// <summary>
        /// Gets all available predefined marketwatches
        /// </summary>
        Task<List<PredefinedMarketwatch>> GetPredefinedMarketwatchesAsync();

        /// <summary>
        /// Gets all scrips in a specific marketwatch
        /// </summary>
        /// <param name="marketwatchId">The ID of the marketwatch</param>
        Task<List<Scrip>> GetMarketwatchScripsAsync(string marketwatchId);
    }
}