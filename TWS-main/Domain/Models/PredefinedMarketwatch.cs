// Domain/Models/PredefinedMarketwatch.cs
namespace TWS.Domain.Models
{
    /// <summary>
    /// Represents a predefined marketwatch provided by the broker
    /// </summary>
    public class PredefinedMarketwatch
    {
        /// <summary>
        /// Unique identifier for the marketwatch
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Display name of the marketwatch
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Category (e.g., "Indices", "F&O", "Top Gainers", etc.)
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Number of scrips in this marketwatch
        /// </summary>
        public int ScripCount { get; set; }

        /// <summary>
        /// Description (optional)
        /// </summary>
        public string Description { get; set; }

        public override string ToString()
        {
            return $"{Name} ({ScripCount} scrips)";
        }
    }
}