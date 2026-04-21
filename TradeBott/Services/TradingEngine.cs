using System;
using System.Linq;
using System.Threading.Tasks;
using TradeBot.Interfaces;
using TradeBot.Models;

namespace TradeBot.Services
{
    // The core engine where Buy and Sell trades are executed
    public class TradingEngine
    {
        private readonly IPortfolioRepository _portfolioRepo;

        public TradingEngine(IPortfolioRepository portfolioRepo)
        {
            _portfolioRepo = portfolioRepo;
        }

        // Executes an asset purchase
        public async Task<Tuple<bool, string, Transaction>> BuyAsync(
            Portfolio portfolio, string symbol, decimal quantity)
        {
            // Locate the asset within the portfolio
            Asset asset = FindAsset(portfolio, symbol);
            if (asset == null)
                return Tuple.Create(false,
                    symbol + " was not found in the portfolio.",
                    (Transaction)null);

            if (quantity <= 0)
                return Tuple.Create(false,
                    "Quantity must be a positive value",
                    (Transaction)null);

            // Calculate applicable fees
            decimal fee = GetFee(asset, quantity);
            decimal totalCost = (quantity * asset.CurrentPrice) + fee;

            // ENCAPSULATION: The Wallet handles its own fund validation
            if (!portfolio.Wallet.HasSufficientFunds(totalCost))
                return Tuple.Create(false,
                    string.Format(
                        "Insufficient funds. Required: ${0:F2}, Available: ${1:F2}",
                        totalCost, portfolio.Wallet.Balance),
                    (Transaction)null);

            // Execute the trade
            // ENCAPSULATION: Wallet handles the withdrawal logic
            portfolio.Wallet.Withdraw(totalCost);

            // POLYMORPHISM: asset.Buy() performs type-specific balance updates
            asset.Buy(quantity);

            // Create a record of the transaction
            var tx = new Transaction
            {
                PortfolioId = portfolio.Id,
                AssetSymbol = asset.Symbol,
                AssetType = asset.AssetType,
                Type = TransactionType.Buy,
                Quantity = quantity,
                PricePerUnit = asset.CurrentPrice,
                TotalCost = totalCost,
                FeeApplied = fee,
                Timestamp = DateTime.UtcNow,
                Notes = string.Format(
                    "{0} {1} bought @ ${2:F4}",
                    quantity, asset.Symbol, asset.CurrentPrice)
            };

            // Add to transaction history
            portfolio.Transactions.Add(tx);

            // Persist changes to MongoDB
            await _portfolioRepo.UpdateAsync(portfolio);

            string msg = string.Format(
                "Purchase Successful! {0} {1} @ ${2:F4} | " +
                "Total Cost: ${3:F2} | Wallet Balance: ${4:F2}",
                quantity, symbol, asset.CurrentPrice,
                totalCost, portfolio.Wallet.Balance);

            return Tuple.Create(true, msg, tx);
        }

        // Executes an asset sale
        public async Task<Tuple<bool, string, Transaction>> SellAsync(
            Portfolio portfolio, string symbol, decimal quantity)
        {
            Asset asset = FindAsset(portfolio, symbol);
            if (asset == null)
                return Tuple.Create(false,
                    "You do not own any " + symbol,
                    (Transaction)null);

            if (quantity <= 0)
                return Tuple.Create(false,
                    "Quantity must be a positive value",
                    (Transaction)null);

            // POLYMORPHISM: Check if the specific asset type has enough quantity to sell
            if (!asset.CanSell(quantity))
                return Tuple.Create(false,
                    string.Format(
                        "Insufficient quantity. Owned: {0}, Requested: {1}",
                        asset.Quantity, quantity),
                    (Transaction)null);

            decimal fee = GetFee(asset, quantity);
            decimal grossProceeds = quantity * asset.CurrentPrice;
            decimal netProceeds = grossProceeds - fee;

            // POLYMORPHISM: Execute the sale based on asset-specific rules
            asset.Sell(quantity);

            // ENCAPSULATION: Deposit the net proceeds into the wallet
            portfolio.Wallet.Deposit(netProceeds);

            var tx = new Transaction
            {
                PortfolioId = portfolio.Id,
                AssetSymbol = asset.Symbol,
                AssetType = asset.AssetType,
                Type = TransactionType.Sell,
                Quantity = quantity,
                PricePerUnit = asset.CurrentPrice,
                TotalCost = netProceeds,
                FeeApplied = fee,
                Timestamp = DateTime.UtcNow,
                Notes = string.Format(
                    "{0} {1} sold @ ${2:F4}",
                    quantity, asset.Symbol, asset.CurrentPrice)
            };

            portfolio.Transactions.Add(tx);

            // Persist changes to MongoDB
            await _portfolioRepo.UpdateAsync(portfolio);

            string msg = string.Format(
                "Sale Successful! {0} {1} @ ${2:F4} | " +
                "Net Proceeds: ${3:F2} | Wallet Balance: ${4:F2}",
                quantity, symbol, asset.CurrentPrice,
                netProceeds, portfolio.Wallet.Balance);

            return Tuple.Create(true, msg, tx);
        }

        // Adds a new asset type to the portfolio
        public async Task<Tuple<bool, string>> AddAssetAsync(
            Portfolio portfolio, Asset asset)
        {
            bool exists = portfolio.GetAllAssets()
                .Any(a => a.Symbol == asset.Symbol);

            if (exists)
                return Tuple.Create(false,
                    asset.Symbol + " is already present in the portfolio.");

            // Determine specific collection for the asset
            if (asset is CryptoAsset)
                portfolio.CryptoAssets.Add((CryptoAsset)asset);
            else if (asset is StockAsset)
                portfolio.StockAssets.Add((StockAsset)asset);

            // Persist changes to MongoDB
            await _portfolioRepo.UpdateAsync(portfolio);

            return Tuple.Create(true,
                asset.AssetType + " '" + asset.Symbol + "' added successfully!");
        }

        // Helper method to find an asset by its symbol
        private Asset FindAsset(Portfolio portfolio, string symbol)
        {
            string upper = symbol.ToUpper();
            foreach (Asset a in portfolio.GetAllAssets())
                if (a.Symbol == upper) return a;
            return null;
        }

        // Calculates transaction fees based on asset type specific properties
        private decimal GetFee(Asset asset, decimal quantity)
        {
            if (asset is CryptoAsset)
            {
                CryptoAsset crypto = (CryptoAsset)asset;
                return crypto.GasFee +
                    (quantity * asset.CurrentPrice *
                    crypto.NetworkFeePercent / 100);
            }
            else if (asset is StockAsset)
            {
                return ((StockAsset)asset).CommissionFee;
            }
            return 0m;
        }
    }
}