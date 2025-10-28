using System.Collections.Generic;
using System.Threading.Tasks;
using TWS.Domain.Models;

namespace TWS.Services.Interfaces
{
    /// <summary>
    /// Interface for order management operations
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Places a new order
        /// </summary>
        Task<PlaceOrderResult> PlaceOrderAsync(OrderRequest request);

        /// <summary>
        /// Modifies an existing order
        /// </summary>
        Task<ModifyOrderResult> ModifyOrderAsync(ModifyOrderRequest request);

        /// <summary>
        /// Cancels an existing order
        /// </summary>
        Task<CancelOrderResult> CancelOrderAsync(string orderNo);

        /// <summary>
        /// Gets the order book for the current day
        /// </summary>
        Task<List<OrderBookItem>> GetOrderBookAsync();

        /// <summary>
        /// Gets order history for a specific order
        /// </summary>
        Task<List<OrderHistoryItem>> GetOrderHistoryAsync(string orderNo);

        /// <summary>
        /// Gets the trade book for the current day
        /// </summary>
        Task<List<TradeBookItem>> GetTradeBookAsync();

        /// <summary>
        /// Calculates margin required for an order
        /// </summary>
        Task<MarginResult> GetOrderMarginAsync(MarginRequest request);

        /// <summary>
        /// Places a GTT (Good Till Triggered) order
        /// </summary>
        Task<PlaceOrderResult> PlaceGTTOrderAsync(PlaceGTTOrderRequest request);

        /// <summary>
        /// Modifies an existing GTT order
        /// </summary>
        Task<ModifyOrderResult> ModifyGTTOrderAsync(ModifyGTTOrderRequest request);

        /// <summary>
        /// Gets all GTT orders
        /// </summary>
        Task<List<GTTOrderItem>> GetGTTOrderBookAsync();
    }

    #region Request Models

    public class PlaceOrderRequest
    {
        public string Exchange { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string Product { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string TriggerPrice { get; set; }
        public string Ret { get; set; }
        public string DisclosedQty { get; set; }
        public string MktProtection { get; set; }
        public string Target { get; set; }
        public string StopLoss { get; set; }
        public string OrderType { get; set; }
        public string Token { get; set; }
        public string Source { get; set; }
        public string TradingSymbol { get; set; }
    }

    public class ModifyOrderRequest
    {
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public string OrderNo { get; set; }
        public string Qty { get; set; }
        public string Ret { get; set; }
        public string PriceType { get; set; }
        public string TransType { get; set; }
        public string Price { get; set; }
        public string TriggerPrice { get; set; }
        public string DisclosedQty { get; set; }
        public string MktProtection { get; set; }
        public string Target { get; set; }
        public string StopLoss { get; set; }
        public string TrailingPrice { get; set; }
    }

    public class MarginRequest
    {
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string Product { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string OrderType { get; set; }
        public string TriggerPrice { get; set; }
        public string StopLoss { get; set; }
    }

    public class PlaceGTTOrderRequest
    {
        public string Exchange { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string Product { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string Ret { get; set; }
        public string OrderType { get; set; }
        public string Token { get; set; }
        public string TradingSymbol { get; set; }
        public string Validity { get; set; }
        public string GttValue { get; set; }
        public string GttType { get; set; }
    }

    public class ModifyGTTOrderRequest
    {
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string Product { get; set; }
        public string Ret { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string OrderType { get; set; }
        public string Token { get; set; }
        public string GttType { get; set; }
        public string GttValue { get; set; }
        public string Validity { get; set; }
        public string OrderNo { get; set; }
    }

    #endregion

    #region Response Models

    public class PlaceOrderResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<OrderResponse> Result { get; set; }
    }

    public class OrderResponse
    {
        public string RequestTime { get; set; }
        public string OrderNo { get; set; }
    }

    public class ModifyOrderResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<OrderResponse> Result { get; set; }
    }

    public class CancelOrderResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<OrderResponse> Result { get; set; }
    }

    public class MarginResult
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public List<MarginResponse> Result { get; set; }
    }

    public class MarginResponse
    {
        public string MarginUsed { get; set; }
        public string MarginUsedTrade { get; set; }
    }

    public class OrderBookItem
    {
        public string OrderNo { get; set; }
        public string UserId { get; set; }
        public string ActId { get; set; }
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public string Qty { get; set; }
        public string TransType { get; set; }
        public string Ret { get; set; }
        public string Token { get; set; }
        public string Price { get; set; }
        public string AvgTradePrice { get; set; }
        public string DisclosedQty { get; set; }
        public string Product { get; set; }
        public string PriceType { get; set; }
        public string OrderType { get; set; }
        public string OrderStatus { get; set; }
        public string FillShares { get; set; }
        public string OrderTime { get; set; }
        public string RejectedReason { get; set; }
    }

    public class OrderHistoryItem
    {
        public string OrderNo { get; set; }
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public string Quantity { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string OrderType { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }
        public string Report { get; set; }
        public string Time { get; set; }
    }

    public class TradeBookItem
    {
        public string OrderNo { get; set; }
        public string Exchange { get; set; }
        public string TradingSymbol { get; set; }
        public string Qty { get; set; }
        public string FillId { get; set; }
        public string FillTime { get; set; }
        public string TranType { get; set; }
        public string FillPrice { get; set; }
        public string Product { get; set; }
    }

    public class GTTOrderItem
    {
        public string GttType { get; set; }
        public string OrderNo { get; set; }
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public string Token { get; set; }
        public string Validity { get; set; }
        public string OrderTime { get; set; }
        public string TransType { get; set; }
        public string PriceType { get; set; }
        public string Qty { get; set; }
        public string Price { get; set; }
        public string Product { get; set; }
        public string GttValue { get; set; }
        public string OrderType { get; set; }
    }

    #endregion
}