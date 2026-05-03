using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using TradeBot.Data;
using TradeBot.Models;
using TradeBot.Repositories;

namespace TradeBot.Services
{
    public class LeaderboardService
    {
        private readonly MongoDbContext _context;
        private readonly PortfolioRepository _portfolioRepo;
        private readonly AnalyticsService _analyticsService;

        public LeaderboardService(MongoDbContext context,
                                   PortfolioRepository portfolioRepo,
                                   AnalyticsService analyticsService)
        {
            _context = context;
            _portfolioRepo = portfolioRepo;
            _analyticsService = analyticsService;
        }

        public async Task<List<LeaderboardEntry>> GetGlobalLeaderboardAsync()
        {
            var leaderboard = new List<LeaderboardEntry>();
            var users = await _context.Users.Find(u => u.IsActive).ToListAsync();
            var portfolioValues = new List<Tuple<string, string, string, decimal, decimal, decimal>>();

            foreach (var user in users)
            {
                var portfolio = await _portfolioRepo.GetByUserIdAsync(user.Id);
                if (portfolio != null)
                {
                    var analytics = _analyticsService.GetPortfolioAnalytics(portfolio);
                    var totalValue = portfolio.GetTotalValue();
                    var totalProfitLoss = analytics.TotalProfitLoss;
                    var profitLossPercent = analytics.TotalProfitLossPercent;

                    portfolioValues.Add(Tuple.Create(
                        user.Id,
                        user.Username,
                        portfolio.Name,
                        totalValue,
                        totalProfitLoss,
                        profitLossPercent
                    ));
                }
            }

            var sorted = portfolioValues.OrderByDescending(x => x.Item4).ToList();

 
            int rank = 1;
            foreach (var item in sorted)
            {
                leaderboard.Add(new LeaderboardEntry
                {
                    UserId = item.Item1,
                    Username = item.Item2,
                    PortfolioName = item.Item3,
                    PortfolioValue = item.Item4,
                    TotalProfitLoss = item.Item5,
                    ProfitLossPercentage = item.Item6,
                    Rank = rank++,
                    LastUpdated = DateTime.UtcNow
                });
            }

            return leaderboard;
        }

        public async Task<List<LeaderboardEntry>> GetTopUsersAsync(int count = 10)
        {
            var leaderboard = await GetGlobalLeaderboardAsync();
            return leaderboard.Take(count).ToList();
        }

        public async Task<LeaderboardEntry> GetUserRankAsync(string userId, string username)
        {
            var leaderboard = await GetGlobalLeaderboardAsync();
            var found = leaderboard.FirstOrDefault(x => x.UserId == userId);

            if (found != null)
            {
                return found;
            }

            return new LeaderboardEntry
            {
                UserId = userId,
                Username = username,
                PortfolioValue = 0,
                Rank = leaderboard.Count + 1,
                TotalProfitLoss = 0,
                ProfitLossPercentage = 0
            };
        }

        public async Task<LeaderboardSummary> GetLeaderboardSummaryAsync(string userId, string username)
        {
            var leaderboard = await GetGlobalLeaderboardAsync();
            var currentUserRank = await GetUserRankAsync(userId, username);

            var summary = new LeaderboardSummary
            {
                TopUsers = leaderboard.Take(10).ToList(),
                CurrentUserRank = currentUserRank,
                TotalUsers = leaderboard.Count,
                AveragePortfolioValue = leaderboard.Any() ? leaderboard.Average(x => x.PortfolioValue) : 0,
                HighestValue = leaderboard.Any() ? leaderboard.Max(x => x.PortfolioValue) : 0,
                LowestValue = leaderboard.Any() ? leaderboard.Min(x => x.PortfolioValue) : 0
            };

            return summary;
        }

        public async Task<LeaderboardEntry> GetWinnerAsync()
        {
            var leaderboard = await GetGlobalLeaderboardAsync();
            return leaderboard.FirstOrDefault();
        }

      
        public async Task<LeaderboardEntry> GetBestPerformerAsync()
        {
            var leaderboard = await GetGlobalLeaderboardAsync();

            var activeTraders = leaderboard
                .Where(x => x.TotalProfitLoss != 0 || x.ProfitLossPercentage != 0)
                .ToList();

            if (activeTraders.Any())
            {
                return activeTraders
                    .OrderByDescending(x => x.ProfitLossPercentage)
                    .FirstOrDefault();
            }

            return null;
        }
    }
}
