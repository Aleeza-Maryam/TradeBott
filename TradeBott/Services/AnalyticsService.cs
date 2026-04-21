using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Calculates the performance of the user's portfolio
    public class AnalyticsService
    {
        // Calculates the total invested amount for a specific asset
        private decimal GetInvestedAmount(Portfolio portfolio, string symbol)
        {
            decimal totalInvested = 0;

            // Filter for Buy transactions
            var buyTransactions = portfolio.Transactions
                .Where(t => t.AssetSymbol == symbol
                         && t.Type == TransactionType.Buy)
                .ToList();

            // Filter for Sell transactions
            var sellTransactions = portfolio.Transactions
                .Where(t => t.AssetSymbol == symbol
                         && t.Type == TransactionType.Sell)
                .ToList();

            foreach (Transaction tx in buyTransactions)
                totalInvested += tx.TotalCost;

            foreach (Transaction tx in sellTransactions)
                totalInvested -= tx.TotalCost;

            // Ensure invested amount doesn't go negative
            if (totalInvested < 0) totalInvested = 0;

            return totalInvested;
        }

        // Generates analytics for a single asset
        public AssetAnalytics GetAssetAnalytics(
            Portfolio portfolio, Asset asset)
        {
            decimal invested = GetInvestedAmount(
                portfolio, asset.Symbol);
            decimal currentValue = asset.CalculateValue();
            decimal profitLoss = currentValue - invested;
            decimal profitLossPercent = 0;

            if (invested > 0)
                profitLossPercent = (profitLoss / invested) * 100;

            return new AssetAnalytics
            {
                Symbol = asset.Symbol,
                AssetType = asset.AssetType,
                InvestedAmount = invested,
                CurrentValue = currentValue,
                ProfitLoss = profitLoss,
                ProfitLossPercent = profitLossPercent
            };
        }

        // Generates comprehensive analytics for the entire portfolio
        public PortfolioAnalytics GetPortfolioAnalytics(Portfolio portfolio)
        {
            var assetAnalyticsList = new List<AssetAnalytics>();

            // POLYMORPHISM: Iterate through the List<Asset> using the base class
            foreach (Asset asset in portfolio.GetAllAssets())
            {
                if (asset.Quantity > 0)
                {
                    var analytics = GetAssetAnalytics(portfolio, asset);
                    assetAnalyticsList.Add(analytics);
                }
            }

            // Calculate total invested amount
            decimal totalInvested = 0;
            foreach (var a in assetAnalyticsList)
                totalInvested += a.InvestedAmount;

            // Calculate total current value
            decimal totalCurrentValue = 0;
            foreach (var a in assetAnalyticsList)
                totalCurrentValue += a.CurrentValue;

            // Calculate total profit/loss
            decimal totalProfitLoss = totalCurrentValue - totalInvested;

            // Calculate total profit/loss percentage
            decimal totalProfitLossPercent = 0;
            if (totalInvested > 0)
                totalProfitLossPercent =
                    (totalProfitLoss / totalInvested) * 100;

            // Identify best and worst performers
            AssetAnalytics bestPerformer = null;
            AssetAnalytics worstPerformer = null;

            if (assetAnalyticsList.Count > 0)
            {
                bestPerformer = assetAnalyticsList
                    .OrderByDescending(a => a.ProfitLossPercent)
                    .First();

                worstPerformer = assetAnalyticsList
                    .OrderBy(a => a.ProfitLossPercent)
                    .First();
            }

            return new PortfolioAnalytics
            {
                TotalInvested = totalInvested,
                TotalCurrentValue = totalCurrentValue,
                TotalProfitLoss = totalProfitLoss,
                TotalProfitLossPercent = totalProfitLossPercent,
                AssetAnalytics = assetAnalyticsList,
                BestPerformer = bestPerformer,
                WorstPerformer = worstPerformer
            };
        }
    }

    // Data structure for individual asset analytics
    public class AssetAnalytics
    {
        public string Symbol { get; set; }
        public string AssetType { get; set; }
        public decimal InvestedAmount { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal ProfitLoss { get; set; }
        public decimal ProfitLossPercent { get; set; }
    }

    // Data structure for overall portfolio analytics
    public class PortfolioAnalytics
    {
        public decimal TotalInvested { get; set; }
        public decimal TotalCurrentValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal TotalProfitLossPercent { get; set; }
        public List<AssetAnalytics> AssetAnalytics { get; set; }
        public AssetAnalytics BestPerformer { get; set; }
        public AssetAnalytics WorstPerformer { get; set; }
    }
}