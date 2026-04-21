using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace TradeBot.Models
{
    public class Wallet
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string PortfolioId { get; set; }
        private decimal _balance;

        public decimal Balance
        {
            get { return _balance; }
            set { _balance = value; }
        }

        public string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual Portfolio Portfolio { get; set; }

        public Wallet()
        {
            Currency = "USD";
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            _balance = 0m;
        }

        public Wallet(decimal initialBalance) : this()
        {
            if (initialBalance < 0)
                throw new ArgumentOutOfRangeException(
                    "initialBalance", "Balance cannot be negative");
            _balance = initialBalance;
        }

        public void Deposit(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    "amount", "Deposit amount must be positive");
            _balance += amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Withdraw(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(
                    "amount", "Withdrawal amount must be positive");
            if (amount > _balance)
                throw new InvalidOperationException(
                    string.Format("Insufficient funds. Available balance: ${0:F2}, Requested: ${1:F2}",
                        _balance, amount));
            _balance -= amount;
            UpdatedAt = DateTime.UtcNow;
        }

        public bool HasSufficientFunds(decimal amount)
        {
            return _balance >= amount;
        }

        public override string ToString()
        {
            return string.Format("Wallet [{0}] | Balance: ${1:F2}",
                Currency, _balance);
        }
    }
}