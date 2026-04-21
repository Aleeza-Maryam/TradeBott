using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using TradeBot.Data;
using TradeBot.Interfaces;
using TradeBot.Models;

namespace TradeBot.Repositories
{
    // REPOSITORY PATTERN: This layer handles all MongoDB data access.
    // Services do not interact with the Database directly; they use these repositories.

    // ── BASE REPOSITORY ───────────────────────────────────
    // A generic base class to handle standard CRUD operations for any entity.
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IMongoCollection<T> _collection;

        protected BaseRepository(IMongoCollection<T> collection)
        {
            _collection = collection;
        }

        public virtual async Task<T> GetByIdAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            await _collection.InsertOneAsync(entity);
        }

        public virtual async Task UpdateAsync(T entity)
        {
            // Uses reflection to find the "Id" property and update the record
            var filter = Builders<T>.Filter.Eq("_id",
                entity.GetType().GetProperty("Id")?.GetValue(entity));
            await _collection.ReplaceOneAsync(filter, entity);
        }

        public virtual async Task DeleteAsync(string id)
        {
            var filter = Builders<T>.Filter.Eq("_id", id);
            await _collection.DeleteOneAsync(filter);
        }
    }

    // ── USER REPOSITORY ───────────────────────────────────
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(MongoDbContext context)
            : base(context.Users)
        {
        }

        // Retrieves a user by their username
        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _collection
                .Find(u => u.Username == username && u.IsActive)
                .FirstOrDefaultAsync();
        }

        // Checks if a username already exists in the database
        public async Task<bool> UsernameExistsAsync(string username)
        {
            var count = await _collection
                .CountDocumentsAsync(u => u.Username == username);
            return count > 0;
        }
    }

    // ── PORTFOLIO REPOSITORY ──────────────────────────────
    public class PortfolioRepository : BaseRepository<Portfolio>,
        IPortfolioRepository
    {
        public PortfolioRepository(MongoDbContext context)
            : base(context.Portfolios)
        {
        }

        // Retrieves a portfolio associated with a specific UserId
        public async Task<Portfolio> GetByUserIdAsync(string userId)
        {
            return await _collection
                .Find(p => p.UserId == userId)
                .FirstOrDefaultAsync();
        }

        // Updates the portfolio, including nested assets and wallet balance
        public override async Task UpdateAsync(Portfolio portfolio)
        {
            var filter = Builders<Portfolio>.Filter
                .Eq(p => p.Id, portfolio.Id);
            await _collection.ReplaceOneAsync(filter, portfolio);
        }
    }

    // ── TRANSACTION REPOSITORY ────────────────────────────
    public class TransactionRepository : BaseRepository<Transaction>,
        ITransactionRepository
    {
        public TransactionRepository(MongoDbContext context)
            : base(context.Transactions)
        {
        }

        // Retrieves the full transaction history for a specific portfolio
        public async Task<IEnumerable<Transaction>> GetByPortfolioIdAsync(
            string portfolioId)
        {
            return await _collection
                .Find(t => t.PortfolioId == portfolioId)
                .SortByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        // Retrieves transaction history for a specific asset symbol
        public async Task<IEnumerable<Transaction>> GetBySymbolAsync(
            string portfolioId, string symbol)
        {
            return await _collection
                .Find(t => t.PortfolioId == portfolioId
                        && t.AssetSymbol == symbol.ToUpper())
                .SortByDescending(t => t.Timestamp)
                .ToListAsync();
        }
    }
}