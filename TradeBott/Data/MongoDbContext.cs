using MongoDB.Driver;
using TradeBot.Models;
using System;

namespace TradeBot.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
           
            var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            string finalConn = !string.IsNullOrEmpty(envConnectionString)
                               ? envConnectionString
                               : connectionString;

            var client = new MongoClient(finalConn);
            _database = client.GetDatabase(databaseName);
        }

  
        public IMongoCollection<User> Users
            => _database.GetCollection<User>("Users");

        public IMongoCollection<Portfolio> Portfolios
            => _database.GetCollection<Portfolio>("Portfolios");

        public IMongoCollection<Transaction> Transactions
            => _database.GetCollection<Transaction>("Transactions");

        public IMongoCollection<TradingGoal> TradingGoals
            => _database.GetCollection<TradingGoal>("TradingGoals");

        public IMongoDatabase Database => _database;

      
        public void CreateIndexes()
        {
            try
            {
              
                var usernameIndex = Builders<User>.IndexKeys.Ascending(u => u.Username);
                Users.Indexes.CreateOne(new CreateIndexModel<User>(
                    usernameIndex, new CreateIndexOptions { Unique = true }));

          
                var portfolioIndex = Builders<Portfolio>.IndexKeys.Ascending(p => p.UserId);
                Portfolios.Indexes.CreateOne(new CreateIndexModel<Portfolio>(portfolioIndex));

                var transactionIndex = Builders<Transaction>.IndexKeys.Ascending(t => t.PortfolioId);
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(transactionIndex));
            }
            catch (Exception ex)
            {
              
                Console.WriteLine($"Note: Index creation skipped or error: {ex.Message}");
            }
        }
    }
}