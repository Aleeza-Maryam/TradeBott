using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Simulates market price fluctuations using random walk theory
    // Runs on a background thread to mimic a live price feed
    public class MarketSimulator
    {
        private readonly Random _rng = new Random();
        private bool _isRunning;
        private Thread _backgroundThread;

        // Reference to AlertService for trigger checking
        private AlertService _alertService;
        private Portfolio _portfolio;

        // Constants for price movement behavior
        private const double CryptoVolatility = 0.08; // High risk/reward
        private const double StockVolatility = 0.03;  // Moderate movement
        private const int TickIntervalMs = 5000;      // 5-second update interval

        // Events to notify the UI or other services
        public event Action<List<Asset>> OnPriceUpdate;
        public event Action<string> OnAlertTriggered;

        // Injects the AlertService dependency
        public void SetAlertService(AlertService alertService)
        {
            _alertService = alertService;
        }

        // Initiates the market simulation
        public void Start(Portfolio portfolio)
        {
            if (_isRunning) return;
            _isRunning = true;
            _portfolio = portfolio;

            _backgroundThread = new Thread(() =>
            {
                while (_isRunning)
                {
                    // 1. Update prices for all assets
                    SimulateOneTick(_portfolio);

                    // 2. Trigger the Price Update event
                    OnPriceUpdate?.Invoke(_portfolio.GetAllAssets());

                    // 3. Evaluate alerts against new prices
                    if (_alertService != null)
                    {
                        // Running the async alert check synchronously within the background thread
                        var alertMessages = Task.Run(async () =>
                            await CheckAlertsAsync()).Result;

                        foreach (string msg in alertMessages)
                            OnAlertTriggered?.Invoke(msg);
                    }

                    // Wait for the next tick interval
                    Thread.Sleep(TickIntervalMs);
                }
            });

            _backgroundThread.IsBackground = true;
            _backgroundThread.Start();
        }

        // Asynchronously checks if any alerts need execution
        private async Task<List<string>> CheckAlertsAsync()
        {
            if (_alertService == null)
                return new List<string>();

            return _alertService.CheckAndExecuteAlerts(_portfolio);
        }

        // Gracefully stops the background simulation
        public void Stop()
        {
            _isRunning = false;
        }

        public bool IsRunning => _isRunning;

        // Updates the price of every asset in the portfolio for a single "tick"
        // POLYMORPHISM: Iterates through List<Asset> regardless of specific type
        public void SimulateOneTick(Portfolio portfolio)
        {
            foreach (Asset asset in portfolio.GetAllAssets())
            {
                // Apply different volatility based on asset type
                double volatility = asset is CryptoAsset
                    ? CryptoVolatility
                    : StockVolatility;

                // Generate a random change factor within the volatility range
                double changeFactor =
                    1 + ((_rng.NextDouble() * 2 - 1) * volatility);

                // Circuit breakers: Prevent extreme single-tick spikes or crashes (max +/- 15%)
                if (changeFactor < 0.85) changeFactor = 0.85;
                if (changeFactor > 1.15) changeFactor = 1.15;

                decimal newPrice = asset.CurrentPrice * (decimal)changeFactor;

                // Floor price: Ensure asset value never hits zero
                asset.CurrentPrice = newPrice < 0.0001m
                    ? 0.0001m
                    : newPrice;
                asset.LastUpdated = DateTime.UtcNow;
            }
        }

        // Returns a formatted string representing the current market state
        // POLYMORPHISM: asset.GetMarketUpdate() is overridden in derived classes
        public string GetMarketSnapshot(Portfolio portfolio)
        {
            var lines = new System.Text.StringBuilder();
            lines.AppendLine("=== LIVE MARKET ===");
            foreach (Asset asset in portfolio.GetAllAssets())
                lines.AppendLine("  " + asset.GetMarketUpdate());
            return lines.ToString();
        }
    }
}