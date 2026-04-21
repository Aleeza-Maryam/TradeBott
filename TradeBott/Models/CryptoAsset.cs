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
    public class CryptoAsset : Asset
    {
        // These properties are specific to Crypto assets
        public decimal GasFee { get; set; }
        public decimal NetworkFeePercent { get; set; }
        public string Blockchain { get; set; }
        public bool IsDefi { get; set; }
        

        public CryptoAsset()
        {
            Blockchain = "Unknown";
        }

        public override string AssetType
        {
            get { return "Crypto"; }
        }

        // POLYMORPHISM: Overriding the Asset's CalculateValue() method here
        public override decimal CalculateValue()
        {
            return Quantity * CurrentPrice;
        }

        // In Crypto, buying incurs an additional GasFee
        public override bool CanBuy(decimal amount, decimal walletBalance)
        {
            decimal totalCost = (amount * CurrentPrice) + GasFee
                                + (amount * CurrentPrice * NetworkFeePercent / 100);
            return amount > 0 && walletBalance >= totalCost;
        }

        // Market update includes specific cryptocurrency information
        public override string GetMarketUpdate()
        {
            Random rand = new Random();
            double changeValue = rand.NextDouble() * 10 - 5;

            return string.Format("{0} on {1}: ${2:F4} ({3:0.00}%) | Gas: ${4:F4} | DeFi: {5}",
                Symbol, Blockchain, CurrentPrice, changeValue, GasFee, (IsDefi ? "Yes" : "No"));
        }

        public override string GenerateReport()
        {
            return base.GenerateReport() +
                string.Format(",GasFee={0:F4},Blockchain={1},IsDeFi={2}", GasFee, Blockchain, IsDefi);
        }

        // Calculates the total cost including all fees
        public decimal GetEffectiveCost(decimal amount)
        {
            return (amount * CurrentPrice) + GasFee
                    + (amount * CurrentPrice * NetworkFeePercent / 100);
        }
    }
}