using System;

namespace TWS.Domain.Models
{
    /// <summary>
    /// Represents a trading scrip/instrument
    /// </summary>
    public class Scrip
    {
        /// <summary>
        /// Unique identifier for the scrip
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Trading symbol (e.g., RELIANCE, INFY)
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Company or instrument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Exchange (NSE, BSE, NFO, MCX)
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Exchange segment (NSE_EQ, BSE_EQ, NFO_FO, etc.)
        /// </summary>
        public string ExchangeSegment { get; set; }

        /// <summary>
        /// Instrument type (EQ, FUT, CE, PE, etc.)
        /// </summary>
        public string InstrumentType { get; set; }

        /// <summary>
        /// Expiry date for derivatives
        /// </summary>
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// Strike price for options
        /// </summary>
        public decimal StrikePrice { get; set; }

        /// <summary>
        /// Option type (CE for Call, PE for Put)
        /// </summary>
        public string OptionType { get; set; }

        /// <summary>
        /// Lot size for the instrument
        /// </summary>
        public int LotSize { get; set; }

        /// <summary>
        /// Minimum tick size for price movement
        /// </summary>
        public decimal TickSize { get; set; }

        /// <summary>
        /// ISIN code
        /// </summary>
        public string ISIN { get; set; }

        /// <summary>
        /// Display name for UI
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(Name) ? Symbol : $"{Symbol} - {Name}";

        public string ScripCode { get; internal set; }
        public string ScripName { get; internal set; }
        public string Segment { get; internal set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}