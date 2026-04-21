using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using TradeBot.Data;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Manages Trading Goals and milestone tracking
    public class GoalService
    {
        private readonly IMongoCollection<TradingGoal> _goals;

        public GoalService(MongoDbContext context)
        {
            // Connects to the TradingGoals collection in MongoDB
            _goals = context.Database
                .GetCollection<TradingGoal>("TradingGoals");
        }

        // Sets a new trading goal for the user
        public async Task<Tuple<bool, string>> SetGoalAsync(
            Portfolio portfolio,
            string goalName,
            decimal targetAmount,
            DateTime targetDate)
        {
            // Input Validation
            if (string.IsNullOrWhiteSpace(goalName))
                return Tuple.Create(false,
                    "Please provide a name for your goal");

            if (targetAmount <= 0)
                return Tuple.Create(false,
                    "Target amount must be positive");

            decimal currentValue = portfolio.GetTotalValue();

            if (targetAmount <= currentValue)
                return Tuple.Create(false,
                    string.Format(
                        "Target amount must be higher than current portfolio value. " +
                        "Current Value: ${0:F2}",
                        currentValue));

            if (targetDate <= DateTime.UtcNow)
                return Tuple.Create(false,
                    "Target date must be in the future");

            // Deactivate any existing active goal
            var oldGoal = await GetActiveGoalAsync(portfolio.Id);
            if (oldGoal != null)
            {
                oldGoal.IsActive = false;
                await UpdateGoalAsync(oldGoal);
            }

            // Initialize new Trading Goal object
            var goal = new TradingGoal
            {
                UserId = portfolio.UserId,
                PortfolioId = portfolio.Id,
                GoalName = goalName,
                TargetAmount = targetAmount,
                StartingAmount = currentValue,
                TargetDate = targetDate,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                IsCompleted = false
            };

            await _goals.InsertOneAsync(goal);

            return Tuple.Create(true,
                string.Format(
                    "Goal set successfully! '{0}' — Reach ${1:F2} by {2:dd/MM/yyyy}",
                    goalName, targetAmount, targetDate));
        }

        // Retrieves the currently active goal for a specific portfolio
        public async Task<TradingGoal> GetActiveGoalAsync(string portfolioId)
        {
            return await _goals
                .Find(g => g.PortfolioId == portfolioId
                        && g.IsActive
                        && !g.IsCompleted)
                .FirstOrDefaultAsync();
        }

        // Retrieves the full history of goals (active and inactive)
        public async Task<List<TradingGoal>> GetAllGoalsAsync(
            string portfolioId)
        {
            return await _goals
                .Find(g => g.PortfolioId == portfolioId)
                .SortByDescending(g => g.CreatedAt)
                .ToListAsync();
        }

        // Updates goal data in the database
        public async Task UpdateGoalAsync(TradingGoal goal)
        {
            var filter = Builders<TradingGoal>.Filter
                .Eq(g => g.Id, goal.Id);
            await _goals.ReplaceOneAsync(filter, goal);
        }

        // Checks if the current portfolio value has reached the target amount
        public async Task<Tuple<bool, string>> CheckGoalCompletionAsync(
            Portfolio portfolio)
        {
            var goal = await GetActiveGoalAsync(portfolio.Id);
            if (goal == null)
                return Tuple.Create(false, "");

            decimal currentValue = portfolio.GetTotalValue();

            // Evaluate completion condition
            if (currentValue >= goal.TargetAmount)
            {
                goal.IsCompleted = true;
                goal.IsActive = false;
                await UpdateGoalAsync(goal);

                return Tuple.Create(true,
                    string.Format(
                        "CONGRATULATIONS! Goal '{0}' has been completed! " +
                        "Target: ${1:F2} | Achieved: ${2:F2}",
                        goal.GoalName,
                        goal.TargetAmount,
                        currentValue));
            }

            return Tuple.Create(false, "");
        }

        // Deactivates the current active goal
        public async Task<Tuple<bool, string>> DeleteGoalAsync(
            string portfolioId)
        {
            var goal = await GetActiveGoalAsync(portfolioId);
            if (goal == null)
                return Tuple.Create(false,
                    "No active goal found to delete");

            goal.IsActive = false;
            await UpdateGoalAsync(goal);

            return Tuple.Create(true, "Goal deleted successfully");
        }
    }
}