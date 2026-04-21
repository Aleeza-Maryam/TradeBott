using System;
using System.Collections.Generic;
using System.Linq;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Saves the price history for each asset
    public class PriceHistoryService
    {
        // Key = Symbol, Value = list of price points
        private Dictionary<string, List<PricePoint>> _history
            = new Dictionary<string, List<PricePoint>>();

        // Maximum number of data points to keep in memory per asset
        private const int MaxHistory = 20;

        // Records a single price point for a symbol
        public void RecordPrice(string symbol, decimal price)
        {
            if (!_history.ContainsKey(symbol))
                _history[symbol] = new List<PricePoint>();

            _history[symbol].Add(new PricePoint
            {
                Price = price,
                Timestamp = DateTime.UtcNow
            });

            // Maintain a rolling window of history by removing the oldest data
            if (_history[symbol].Count > MaxHistory)
                _history[symbol].RemoveAt(0);
        }

        // Records current prices for all assets in the portfolio
        // POLYMORPHISM: Iterates through List<Asset> to record data for any asset type
        public void RecordAllPrices(Portfolio portfolio)
        {
            foreach (Asset asset in portfolio.GetAllAssets())
                RecordPrice(asset.Symbol, asset.CurrentPrice);
        }

        // Retrieves the history list for a specific symbol
        public List<PricePoint> GetHistory(string symbol)
        {
            if (_history.ContainsKey(symbol))
                return _history[symbol];
            return new List<PricePoint>();
        }

        // Returns a list of all asset symbols currently tracked in history
        public List<string> GetAllSymbols()
        {
            return new List<string>(_history.Keys);
        }

        // Calculates the highest price recorded in the current history window
        public decimal GetHighPrice(string symbol)
        {
            var history = GetHistory(symbol);
            if (history.Count == 0) return 0;
            decimal high = history[0].Price;
            foreach (var p in history)
                if (p.Price > high) high = p.Price;
            return high;
        }

        // Calculates the lowest price recorded in the current history window
        public decimal GetLowPrice(string symbol)
        {
            var history = GetHistory(symbol);
            if (history.Count == 0) return 0;
            decimal low = history[0].Price;
            foreach (var p in history)
                if (p.Price < low) low = p.Price;
            return low;
        }

        // Retrieves the most recent price point
        public decimal GetLastPrice(string symbol)
        {
            var history = GetHistory(symbol);
            if (history.Count == 0) return 0;
            return history[history.Count - 1].Price;
        }

        // Retrieves the oldest price point in the current window
        public decimal GetFirstPrice(string symbol)
        {
            var history = GetHistory(symbol);
            if (history.Count == 0) return 0;
            return history[0].Price;
        }
    }

    // Represents a single point of price data at a specific time
    public class PricePoint
    {
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}