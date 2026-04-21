using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Manages Stop Loss and Take Profit alerts
    public class AlertService
    {
        private readonly TradingEngine _tradingEngine;

        // Alerts are stored in memory
        private List<PriceAlert> _alerts = new List<PriceAlert>();
        private int _nextId = 1;

        public AlertService(TradingEngine tradingEngine)
        {
            _tradingEngine = tradingEngine;
        }

        // Adds a new price alert
        public async Task<Tuple<bool, string>> AddAlertAsync(
            Portfolio portfolio,
            string symbol,
            AlertType type,
            decimal targetPrice,
            decimal quantityToSell)
        {
            // Check if the asset exists in the portfolio
            Asset asset = portfolio.GetAllAssets()
                .FirstOrDefault(a => a.Symbol == symbol.ToUpper());

            if (asset == null)
                return Tuple.Create(false,
                    symbol + " is not in your portfolio");

            if (quantityToSell <= 0)
                return Tuple.Create(false,
                    "Quantity must be positive");

            if (quantityToSell > asset.Quantity)
                return Tuple.Create(false,
                    string.Format(
                        "Insufficient quantity. You own: {0}",
                        asset.Quantity));

            if (targetPrice <= 0)
                return Tuple.Create(false,
                    "Target price must be positive");

            // Stop Loss validation
            if (type == AlertType.StopLoss &&
                targetPrice >= asset.CurrentPrice)
                return Tuple.Create(false,
                    string.Format(
                        "Stop Loss price must be lower than the current price. " +
                        "Current: ${0:F2}",
                        asset.CurrentPrice));

            // Take Profit validation
            if (type == AlertType.TakeProfit &&
                targetPrice <= asset.CurrentPrice)
                return Tuple.Create(false,
                    string.Format(
                        "Take Profit price must be higher than the current price. " +
                        "Current: ${0:F2}",
                        asset.CurrentPrice));

            // Create the alert object
            var alert = new PriceAlert
            {
                PortfolioId = portfolio.Id,
                AssetSymbol = symbol.ToUpper(),
                Type = type,
                TargetPrice = targetPrice,
                QuantityToSell = quantityToSell,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _alerts.Add(alert);

            string typeStr = type == AlertType.StopLoss
                ? "Stop Loss"
                : "Take Profit";

            return Tuple.Create(true,
                string.Format(
                    "{0} alert set successfully! {1} @ ${2:F2}",
                    typeStr, symbol.ToUpper(), targetPrice));
        }

        // Deactivates an alert
        public Tuple<bool, string> RemoveAlert(string alertId)
        {
            PriceAlert alert = _alerts
                .FirstOrDefault(a => a.Id == alertId && a.IsActive);

            if (alert == null)
                return Tuple.Create(false, "Alert not found");

            alert.IsActive = false;
            return Tuple.Create(true, "Alert removed successfully");
        }

        // Returns all active alerts for a specific portfolio
        public List<PriceAlert> GetActiveAlerts(string portfolioId)
        {
            return _alerts
                .Where(a => a.PortfolioId == portfolioId && a.IsActive)
                .ToList();
        }

        // Checks price conditions on every market tick and executes trades if met
        public List<string> CheckAndExecuteAlerts(Portfolio portfolio)
        {
            var messages = new List<string>();

            var activeAlerts = _alerts
                .Where(a => a.PortfolioId == portfolio.Id && a.IsActive)
                .ToList();

            foreach (PriceAlert alert in activeAlerts)
            {
                Asset asset = portfolio.GetAllAssets()
                    .FirstOrDefault(a => a.Symbol == alert.AssetSymbol);

                if (asset == null) continue;

                bool conditionMet = false;

                // Check Stop Loss condition
                if (alert.Type == AlertType.StopLoss &&
                    asset.CurrentPrice <= alert.TargetPrice)
                    conditionMet = true;

                // Check Take Profit condition
                if (alert.Type == AlertType.TakeProfit &&
                    asset.CurrentPrice >= alert.TargetPrice)
                    conditionMet = true;

                if (conditionMet)
                {
                    decimal sellQty = Math.Min(
                        alert.QuantityToSell,
                        asset.Quantity);

                    if (sellQty > 0)
                    {
                        // Execute the sell order synchronously within the tick loop
                        var result = Task.Run(async () =>
                            await _tradingEngine.SellAsync(
                                portfolio,
                                alert.AssetSymbol,
                                sellQty)).Result;

                        string typeStr = alert.Type == AlertType.StopLoss
                            ? "STOP LOSS"
                            : "TAKE PROFIT";

                        if (result.Item1)
                        {
                            messages.Add(string.Format(
                                "!! {0} TRIGGERED !! Sold {1} {2} units " +
                                "@ ${3:F2}",
                                typeStr,
                                alert.AssetSymbol,
                                sellQty,
                                asset.CurrentPrice));
                        }
                    }

                    // Deactivate alert after execution
                    alert.IsActive = false;
                }
            }

            return messages;
        }
    }
}