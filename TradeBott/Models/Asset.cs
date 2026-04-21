using System;
using TradeBot.Interfaces;

namespace TradeBot.Models
{
    // ABSTRACTION: This is an abstract class
    // It cannot be instantiated directly
    public abstract class Asset : ITradable, IReportable
    {
        // ENCAPSULATION: Private fields
        private string _symbol = string.Empty;
        private decimal _currentPrice;
        private decimal _quantity;

        public int Id { get; set; }
        public int PortfolioId { get; set; }
        public string Name { get; set; }

        public DateTime LastUpdated { get; set; }

        // Constructor to initialize default values
        public Asset()
        {
            Name = string.Empty;
            LastUpdated = DateTime.UtcNow;
        }

        // Access via Properties
        public string Symbol
        {
            get { return _symbol; }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("Symbol cannot be empty");
                _symbol = value.ToUpper().Trim();
            }
        }

        public decimal CurrentPrice
        {
            get { return _currentPrice; }
            set { _currentPrice = value >= 0 ? value : throw new ArgumentOutOfRangeException("Price cannot be negative"); }
        }

        public decimal Quantity
        {
            get { return _quantity; }
            set { _quantity = value >= 0 ? value : throw new ArgumentOutOfRangeException("Quantity cannot be negative"); }
        }

        // ABSTRACTION: Implementation is required in derived classes
        public abstract decimal CalculateValue();
        public abstract string GetMarketUpdate();
        public abstract string AssetType { get; }

        // ITradable interface methods
        public virtual bool CanBuy(decimal amount, decimal walletBalance)
        {
            return amount > 0 && walletBalance >= (amount * CurrentPrice);
        }

        public virtual bool CanSell(decimal amount)
        {
            return amount > 0 && Quantity >= amount;
        }

        public virtual void Buy(decimal amount)
        {
            Quantity += amount;
            LastUpdated = DateTime.UtcNow;
        }

        public virtual void Sell(decimal amount)
        {
            if (!CanSell(amount))
                throw new InvalidOperationException("Insufficient quantity available.");

            Quantity -= amount;
            LastUpdated = DateTime.UtcNow;
        }

        // IReportable interface method
        public virtual string GenerateReport()
        {
            return string.Format("{0},{1},{2},{3},{4:F4},{5:F2},{6:yyyy-MM-dd HH:mm:ss}",
                AssetType, Symbol, Name, Quantity, CurrentPrice, CalculateValue(), LastUpdated);
        }

        public override string ToString()
        {
            return string.Format("[{0}] {1} | Price: ${2:F4} | Qty: {3} | Value: ${4:F2}",
                AssetType, Symbol, CurrentPrice, Quantity, CalculateValue());
        }
    }
}