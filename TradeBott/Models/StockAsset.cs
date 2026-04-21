using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace TradeBot.Models
{
    // INHERITANCE: Inheriting from the Asset class
    public class StockAsset : Asset
    {
        // These properties are specific to Stocks
        public decimal DividendYield { get; set; }
        public decimal PriceEarningsRatio { get; set; }
        public string Exchange { get; set; }
        public string Sector { get; set; }
        public decimal CommissionFee { get; set; }

      

        public StockAsset()
        {
            Exchange = "NYSE";
            Sector = "Unknown";
            CommissionFee = 9.99m;
        }

        public override string AssetType
        {
            get { return "Stock"; }
        }

        // POLYMORPHISM: Calculating basic stock value
        public override decimal CalculateValue()
        {
            return Quantity * CurrentPrice;
        }

        // Stocks require a Commission Fee during a purchase
        public override bool CanBuy(decimal amount, decimal walletBalance)
        {
            decimal totalCost = (amount * CurrentPrice) + CommissionFee;
            return amount > 0 && walletBalance >= totalCost;
        }

        // Market update includes specific stock exchange and financial data
        public override string GetMarketUpdate()
        {
            Random rand = new Random();
            double changeValue = rand.NextDouble() * 6 - 3;

            return string.Format("{0} ({1}): ${2:F2} ({3:+0.00;-0.00}%) | P/E: {4:F1} | Div: {5:F2}% | Sector: {6}",
                Symbol, Exchange, CurrentPrice, changeValue, PriceEarningsRatio, DividendYield, Sector);
        }

        public override string GenerateReport()
        {
            return base.GenerateReport() +
                string.Format(",DividendYield={0:F2}%,Exchange={1},Sector={2}", DividendYield, Exchange, Sector);
        }

        // Calculates the projected annual dividend income
        public decimal GetAnnualDividendIncome()
        {
            return CalculateValue() * (DividendYield / 100);
        }

        // Calculates total cost including the commission fee
        public decimal GetEffectiveCost(decimal amount)
        {
            return (amount * CurrentPrice) + CommissionFee;
        }
    }
}