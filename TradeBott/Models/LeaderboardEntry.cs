using System;
using System.Collections.Generic;

namespace TradeBot.Models
{
    public class LeaderboardEntry
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        public string PortfolioName { get; set; }
        public decimal PortfolioValue { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public decimal ProfitLossPercentage { get; set; }
        public int Rank { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class LeaderboardSummary
    {
        public List<LeaderboardEntry> TopUsers { get; set; }
        public LeaderboardEntry CurrentUserRank { get; set; }
        public int TotalUsers { get; set; }
        public decimal AveragePortfolioValue { get; set; }
        public decimal HighestValue { get; set; }
        public decimal LowestValue { get; set; }
    }
}