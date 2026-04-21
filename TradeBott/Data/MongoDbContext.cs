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
            // PRO-TIP: Pehle check karein ke environment variable mein koi 
            // connection string toh nahi di gayi (Docker ke liye zaroori hai)
            var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            // Agar environment variable mil jaye toh usay use karein, warna default wali
            string finalConn = !string.IsNullOrEmpty(envConnectionString)
                               ? envConnectionString
                               : connectionString;

            var client = new MongoClient(finalConn);
            _database = client.GetDatabase(databaseName);
        }

        // Collections (SQL ki Tables ki tarah)
        public IMongoCollection<User> Users
            => _database.GetCollection<User>("Users");

        public IMongoCollection<Portfolio> Portfolios
            => _database.GetCollection<Portfolio>("Portfolios");

        public IMongoCollection<Transaction> Transactions
            => _database.GetCollection<Transaction>("Transactions");

        public IMongoCollection<TradingGoal> TradingGoals
            => _database.GetCollection<TradingGoal>("TradingGoals");

        public IMongoDatabase Database => _database;

        // Search performance behtar karne ke liye Indexes
        public void CreateIndexes()
        {
            try
            {
                // 1. Username Unique rakha jaye
                var usernameIndex = Builders<User>.IndexKeys.Ascending(u => u.Username);
                Users.Indexes.CreateOne(new CreateIndexModel<User>(
                    usernameIndex, new CreateIndexOptions { Unique = true }));

                // 2. UserId par index (Fast Portfolio search)
                var portfolioIndex = Builders<Portfolio>.IndexKeys.Ascending(p => p.UserId);
                Portfolios.Indexes.CreateOne(new CreateIndexModel<Portfolio>(portfolioIndex));

                // 3. PortfolioId par index (Fast Transaction history)
                var transactionIndex = Builders<Transaction>.IndexKeys.Ascending(t => t.PortfolioId);
                Transactions.Indexes.CreateOne(new CreateIndexModel<Transaction>(transactionIndex));
            }
            catch (Exception ex)
            {
                // Agar index pehle se bana ho ya error aaye toh program crash na ho
                Console.WriteLine($"Note: Index creation skipped or error: {ex.Message}");
            }
        }
    }
}