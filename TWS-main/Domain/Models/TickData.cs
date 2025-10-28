using System;
using System.Collections.Generic;

namespace TWS.Domain.Models
{
    /// <summary>
    /// Represents market tick data (LTP, OHLC, Volume)
    /// </summary>
    public class TickData
    {
        /// <summary>
        /// Message type: "tk" (acknowledgement) or "tf" (feed)
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Exchange code (e.g., "NSE", "BSE", "NFO", "MCX")
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Security token/instrument identifier
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Combined scrip code in format "EXCHANGE|TOKEN" (e.g., "NSE|11536")
        /// </summary>
        public string ScripCode { get; set; }

        /// <summary>
        /// Symbol name/trading symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Last traded price (LTP)
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Absolute change in price
        /// </summary>
        public decimal Change { get; set; }

        /// <summary>
        /// Percentage change
        /// </summary>
        public decimal PercentageChange { get; set; }

        /// <summary>
        /// Trading volume
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Previous closing price
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Average traded price
        /// </summary>
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// Open interest (for derivatives)
        /// </summary>
        public long OpenInterest { get; set; }

        /// <summary>
        /// Lot size
        /// </summary>
        public int LotSize { get; set; }

        /// <summary>
        /// Tick size (minimum price movement)
        /// </summary>
        public decimal TickSize { get; set; }

        /// <summary>
        /// Best buy price (Level 1)
        /// </summary>
        public decimal BuyPrice { get; set; }

        /// <summary>
        /// Best sell price (Level 1)
        /// </summary>
        public decimal SellPrice { get; set; }

        /// <summary>
        /// Best buy quantity (Level 1)
        /// </summary>
        public int BuyQuantity1 { get; set; }

        /// <summary>
        /// Best sell quantity (Level 1)
        /// </summary>
        public int SellQuantity1 { get; set; }

        /// <summary>
        /// Feed timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates if this is a snapshot (acknowledgement) or update (feed)
        /// </summary>
        public bool IsSnapshot => MessageType == "tk";

        /// <summary>
        /// Indicates if this is a price update
        /// </summary>
        public bool IsUpdate => MessageType == "tf";

        public decimal OpenPrice { get; internal set; }
        public decimal HighPrice { get; internal set; }
        public decimal LowPrice { get; internal set; }
        public decimal ClosePrice { get; internal set; }
    }

    /// <summary>
    /// Represents market depth data (5-level depth with all details)
    /// </summary>
    public class DepthData
    {
        public DepthData()
        {
            BuyPrices = new List<decimal>();
            SellPrices = new List<decimal>();
            BuyQuantities = new List<int>();
            SellQuantities = new List<int>();
            BuyOrders = new List<int>();
            SellOrders = new List<int>();
        }

        /// <summary>
        /// Message type: "dk" (acknowledgement) or "df" (feed)
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// Exchange code
        /// </summary>
        public string Exchange { get; set; }

        /// <summary>
        /// Security token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// Symbol name
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Last traded price
        /// </summary>
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Percentage change
        /// </summary>
        public decimal PercentageChange { get; set; }

        /// <summary>
        /// Trading volume
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        public decimal Open { get; set; }

        /// <summary>
        /// High price
        /// </summary>
        public decimal High { get; set; }

        /// <summary>
        /// Low price
        /// </summary>
        public decimal Low { get; set; }

        /// <summary>
        /// Previous closing price
        /// </summary>
        public decimal Close { get; set; }

        /// <summary>
        /// Average traded price
        /// </summary>
        public decimal AveragePrice { get; set; }

        /// <summary>
        /// Open interest
        /// </summary>
        public long OpenInterest { get; set; }

        /// <summary>
        /// Last traded quantity
        /// </summary>
        public int LastTradedQty { get; set; }

        /// <summary>
        /// Last traded time
        /// </summary>
        public string LastTradedTime { get; set; }

        /// <summary>
        /// Total buy quantity
        /// </summary>
        public long TotalBuyQty { get; set; }

        /// <summary>
        /// Total sell quantity
        /// </summary>
        public long TotalSellQty { get; set; }

        /// <summary>
        /// Upper circuit limit
        /// </summary>
        public decimal UpperCircuit { get; set; }

        /// <summary>
        /// Lower circuit limit
        /// </summary>
        public decimal LowerCircuit { get; set; }

        /// <summary>
        /// 52-week high
        /// </summary>
        public decimal High52Week { get; set; }

        /// <summary>
        /// 52-week low
        /// </summary>
        public decimal Low52Week { get; set; }

        /// <summary>
        /// Buy prices (up to 5 levels) - bp1, bp2, bp3, bp4, bp5
        /// </summary>
        public List<decimal> BuyPrices { get; set; }

        /// <summary>
        /// Sell prices (up to 5 levels) - sp1, sp2, sp3, sp4, sp5
        /// </summary>
        public List<decimal> SellPrices { get; set; }

        /// <summary>
        /// Buy quantities (up to 5 levels) - bq1, bq2, bq3, bq4, bq5
        /// </summary>
        public List<int> BuyQuantities { get; set; }

        /// <summary>
        /// Sell quantities (up to 5 levels) - sq1, sq2, sq3, sq4, sq5
        /// </summary>
        public List<int> SellQuantities { get; set; }

        /// <summary>
        /// Buy order counts (up to 5 levels) - bo1, bo2, bo3, bo4, bo5
        /// </summary>
        public List<int> BuyOrders { get; set; }

        /// <summary>
        /// Sell order counts (up to 5 levels) - so1, so2, so3, so4, so5
        /// </summary>
        public List<int> SellOrders { get; set; }

        /// <summary>
        /// Feed timestamp
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates if this is a snapshot (acknowledgement) or update (feed)
        /// </summary>
        public bool IsSnapshot => MessageType == "dk";

        /// <summary>
        /// Indicates if this is a depth update
        /// </summary>
        public bool IsUpdate => MessageType == "df";
    }
}