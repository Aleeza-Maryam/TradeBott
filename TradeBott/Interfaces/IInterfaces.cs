using System.Collections.Generic;
using System.Threading.Tasks;
using TradeBot.Models;

namespace TradeBot.Interfaces
{
    // ITradable: Every tradable asset must implement this interface
    public interface ITradable
    {
        bool CanBuy(decimal amount, decimal walletBalance);
        bool CanSell(decimal amount);
        void Buy(decimal amount);
        void Sell(decimal amount);
    }

    // IReportable: Interface for objects that can be included in reports
    public interface IReportable
    {
        string GenerateReport();
    }

    // IRepository: Generic CRUD interface — now asynchronous in .NET 8
    public interface IRepository<T> where T : class
    {
        Task<T> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(string id);
    }

    // IUserRepository: Specific repository for User-related data operations
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetByUsernameAsync(string username);
        Task<bool> UsernameExistsAsync(string username);
    }

    // IPortfolioRepository: Specific repository for Portfolio-related data operations
    public interface IPortfolioRepository : IRepository<Portfolio>
    {
        Task<Portfolio> GetByUserIdAsync(string userId);
    }

    // ITransactionRepository: Specific repository for Transaction-related data operations
    public interface ITransactionRepository : IRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByPortfolioIdAsync(string portfolioId);
        Task<IEnumerable<Transaction>> GetBySymbolAsync(
            string portfolioId, string symbol);
    }
}