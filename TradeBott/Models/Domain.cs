using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TradeBot.Models
{
    // User Account information
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }

        public User()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
        }
    }

    // User Portfolio management
    public class Portfolio
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }

        public Wallet Wallet { get; set; }
        public List<Transaction> Transactions { get; set; }
        public List<CryptoAsset> CryptoAssets { get; set; }
        public List<StockAsset> StockAssets { get; set; }

        public Portfolio()
        {
            Name = "My Portfolio";
            CreatedAt = DateTime.UtcNow;
            Transactions = new List<Transaction>();
            CryptoAssets = new List<CryptoAsset>();
            StockAssets = new List<StockAsset>();
            Wallet = new Wallet(10000m);
        }

        // POLYMORPHISM: Retrieves all assets together
        public List<Asset> GetAllAssets()
        {
            var all = new List<Asset>();
            all.AddRange(CryptoAssets);
            all.AddRange(StockAssets);
            return all;
        }

        // POLYMORPHISM: Calculates total value in a single loop
        public decimal GetTotalValue()
        {
            decimal assetValue = 0;
            foreach (Asset asset in GetAllAssets())
                assetValue += asset.CalculateValue();
            return assetValue + (Wallet != null ? Wallet.Balance : 0);
        }

        public override string ToString()
        {
            return string.Format(
                "Portfolio '{0}' | Wallet: ${1:F2} | Total: ${2:F2}",
                Name,
                Wallet != null ? Wallet.Balance : 0,
                GetTotalValue());
        }
    }

    // Record for every Buy/Sell action
    public class Transaction
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PortfolioId { get; set; }
        public string AssetSymbol { get; set; }
        public string AssetType { get; set; }
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalCost { get; set; }
        public decimal FeeApplied { get; set; }
        public DateTime Timestamp { get; set; }
        public string Notes { get; set; }

        public Transaction()
        {
            Timestamp = DateTime.UtcNow;
            Notes = string.Empty;
        }

        public override string ToString()
        {
            return string.Format(
                "{0:g} | {1} {2} {3} @ ${4:F4} = ${5:F2}",
                Timestamp,
                Type,
                Quantity,
                AssetSymbol,
                PricePerUnit,
                TotalCost);
        }
    }

    // Transaction action types
    public enum TransactionType
    {
        Buy,
        Sell
    }

    // Stop Loss or Take Profit alert settings
    public class PriceAlert
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PortfolioId { get; set; }
        public string AssetSymbol { get; set; }
        public AlertType Type { get; set; }
        public decimal TargetPrice { get; set; }
        public decimal QuantityToSell { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        public PriceAlert()
        {
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
        }

        public override string ToString()
        {
            string typeStr = Type == AlertType.StopLoss
                ? "Stop Loss"
                : "Take Profit";

            string condition = Type == AlertType.StopLoss
                ? "drops below"
                : "rises above";

            return string.Format(
                "[{0}] {1} — If {2} {3} ${4:F2}, then sell {5} units",
                typeStr,
                AssetSymbol,
                AssetSymbol,
                condition,
                TargetPrice,
                QuantityToSell);
        }
    }

    // Alert classification
    public enum AlertType
    {
        StopLoss,
        TakeProfit
    }

    // User trading goals and progress tracking
    public class TradingGoal
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }
        public string PortfolioId { get; set; }

        // Name of the goal
        public string GoalName { get; set; }

        // Target amount to be achieved
        public decimal TargetAmount { get; set; }

        // Amount at the time the goal was set
        public decimal StartingAmount { get; set; }

        // Target date for completion
        public DateTime TargetDate { get; set; }

        // Timestamp of goal creation
        public DateTime CreatedAt { get; set; }

        // Status of the goal
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }

        public TradingGoal()
        {
            CreatedAt = DateTime.UtcNow;
            IsActive = true;
            IsCompleted = false;
        }

        // Calculates completion percentage
        public decimal GetProgressPercent(decimal currentAmount)
        {
            if (TargetAmount <= StartingAmount) return 100;

            decimal progress = currentAmount - StartingAmount;
            decimal total = TargetAmount - StartingAmount;

            decimal percent = (progress / total) * 100;

            // Clamping between 0 and 100
            if (percent < 0) return 0;
            if (percent > 100) return 100;

            return Math.Round(percent, 1);
        }

        // Returns days remaining until the target date
        public int GetDaysRemaining()
        {
            int days = (int)(TargetDate - DateTime.UtcNow).TotalDays;
            return days < 0 ? 0 : days;
        }

        // Calculates daily profit required to reach the goal
        public decimal GetDailyTarget(decimal currentAmount)
        {
            decimal remaining = TargetAmount - currentAmount;
            int daysLeft = GetDaysRemaining();

            if (daysLeft == 0) return remaining;
            if (remaining <= 0) return 0;

            return Math.Round(remaining / daysLeft, 2);
        }

        public override string ToString()
        {
            return string.Format(
                "Goal: {0} | Target: ${1:F2} | Date: {2:dd/MM/yyyy}",
                GoalName,
                TargetAmount,
                TargetDate);
        }
    }
}