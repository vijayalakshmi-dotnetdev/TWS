using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TWS.Domain.Models
{
    /// <summary>
    /// Order domain model
    /// </summary>
    public class Order
    {
        public string OrderId { get; set; }
        public string Variety { get; set; } = "NORMAL";
        public string TradingSymbol { get; set; }
        public string SymbolName { get; set; }
        public string Exchange { get; set; }
        public string Token { get; set; }
        public string TransactionType { get; set; } // BUY or SELL
        public string OrderType { get; set; } // MARKET, LIMIT, SL, SL-M
        public string ProductType { get; set; } // INTRADAY, DELIVERY, CARRYFORWARD
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? TriggerPrice { get; set; }
        public decimal? DisclosedQuantity { get; set; }
        public string Validity { get; set; } = "DAY"; // DAY, IOC
        public string Status { get; set; } // PENDING, OPEN, COMPLETE, CANCELLED, REJECTED
        public decimal? FilledQuantity { get; set; }
        public decimal? PendingQuantity { get; set; }
        public decimal? AveragePrice { get; set; }
        public DateTime OrderTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string StatusMessage { get; set; }
        public string Tag { get; set; }

        // Options specific
        public string OptionType { get; set; } // CE, PE
        public decimal? StrikePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }

        public Order()
        {
            OrderTime = DateTime.Now;
            Status = "PENDING";
        }
    }

    /// <summary>
    /// Order request for API
    /// </summary>
    public class OrderRequest
    {
        public string variety { get; set; } = "NORMAL";
        public string tradingsymbol { get; set; }
        public string symbolname { get; set; }
        public string Token { get; set; }
        public string transtype { get; set; }
        public int qty { get; set; }   
        public string trgprc { get; set; }
        public int? disqty { get; set; }
        public string prd { get; set; }
        public string ret { get; set; } = "DAY";
        public string mkt_protection { get; set; } = "0";
        public string source { get; set; } = "WEB";       
        public string exchange { get;  set; }
        public string priceType { get;  set; }
        public string price { get;  set; }
        public string triggerPrice { get;  set; }
        public string product { get;  set; }
        public string transType { get;  set; }
        public string token { get; set; } = "2885";
        public string orderType { get; internal set; }
        public string stopLoss { get; internal set; }
    }

    /// <summary>
    /// Order response from API
    /// </summary>
    public class OrderResponse
    {
        public string stat { get; set; }
        public string request_time { get; set; }
        public string norenordno { get; set; }
        public string result { get; set; }
        public string emsg { get; set; }
    }

    /// <summary>
    /// Order book entry - ViewModel for grid display
    /// </summary>
    public class OrderBookEntry : INotifyPropertyChanged
    {
        private string _status;
        private decimal? _filledQty;
        private decimal? _pendingQty;
        private decimal? _avgPrice;

        public string OrderId { get; set; }
        public DateTime OrderTime { get; set; }
        public string Symbol { get; set; }
        public string Exchange { get; set; }
        public string TransactionType { get; set; }
        public string OrderType { get; set; }
        public string ProductType { get; set; }
        public decimal Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? TriggerPrice { get; set; }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal? FilledQuantity
        {
            get => _filledQty;
            set
            {
                if (_filledQty != value)
                {
                    _filledQty = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal? PendingQuantity
        {
            get => _pendingQty;
            set
            {
                if (_pendingQty != value)
                {
                    _pendingQty = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal? AveragePrice
        {
            get => _avgPrice;
            set
            {
                if (_avgPrice != value)
                {
                    _avgPrice = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Enums for order properties
    /// </summary>
    public static class OrderEnums
    {
        public enum TransactionType
        {
            BUY,
            SELL
        }

        public enum OrderType
        {
            MARKET,
            LIMIT,
            SL,      // Stop Loss Limit
            SLM      // Stop Loss Market
        }

        public enum ProductType
        {
            INTRADAY,    // MIS
            DELIVERY,    // CNC
            CARRYFORWARD // NRML
        }

        public enum ValidityType
        {
            DAY,
            IOC
        }

        public enum OrderStatus
        {
            PENDING,
            OPEN,
            COMPLETE,
            CANCELLED,
            REJECTED
        }
    }
}