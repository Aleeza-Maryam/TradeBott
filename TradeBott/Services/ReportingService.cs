using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TradeBot.Models;

namespace TradeBot.Services
{
    // Generates CSV and JSON reports for portfolio data
    // Utilizes System.IO for file system operations
    public class ReportingService
    {
        private readonly string _reportsDirectory;

        public ReportingService(string reportsDirectory = null)
        {
            _reportsDirectory = reportsDirectory
                ?? Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "reports");

            // Create the directory if it does not exist
            if (!Directory.Exists(_reportsDirectory))
                Directory.CreateDirectory(_reportsDirectory);
        }

        // Exports transaction history to a CSV file
        public async Task<string> ExportTransactionsCsvAsync(
            Portfolio portfolio, string username = "user")
        {
            string filename = string.Format(
                "trades_{0}_{1:yyyyMMdd_HHmmss}.csv",
                username,
                DateTime.Now);

            string filepath = Path.Combine(_reportsDirectory, filename);

            var sb = new StringBuilder();

            // CSV Header
            sb.AppendLine(
                "Timestamp,Type,Symbol,AssetType," +
                "Quantity,PricePerUnit,TotalCost,FeeApplied,Notes");

            // Sort transactions by time and append data rows
            var sorted = portfolio.Transactions
                .OrderBy(t => t.Timestamp).ToList();

            foreach (Transaction tx in sorted)
            {
                sb.AppendLine(string.Format(
                    "{0:yyyy-MM-dd HH:mm:ss},{1},{2},{3}," +
                    "{4},{5},{6},{7},\"{8}\"",
                    tx.Timestamp,
                    tx.Type,
                    tx.AssetSymbol,
                    tx.AssetType,
                    tx.Quantity,
                    tx.PricePerUnit,
                    tx.TotalCost,
                    tx.FeeApplied,
                    tx.Notes));
            }

            // Write the file to disk using System.IO
            await File.WriteAllTextAsync(
                filepath, sb.ToString(), Encoding.UTF8);

            return filepath;
        }

        // Exports a current portfolio snapshot to CSV
        public async Task<string> ExportPortfolioSnapshotCsvAsync(
            Portfolio portfolio, string username = "user")
        {
            string filename = string.Format(
                "portfolio_{0}_{1:yyyyMMdd_HHmmss}.csv",
                username,
                DateTime.Now);

            string filepath = Path.Combine(_reportsDirectory, filename);

            var sb = new StringBuilder();
            sb.AppendLine(
                "AssetType,Symbol,Name,Quantity," +
                "CurrentPrice,TotalValue,LastUpdated");

            // POLYMORPHISM: asset.GenerateReport() provides specific formatting for each asset type
            foreach (Asset asset in portfolio.GetAllAssets())
                sb.AppendLine(asset.GenerateReport());

            await File.WriteAllTextAsync(
                filepath, sb.ToString(), Encoding.UTF8);

            return filepath;
        }

        // Exports full portfolio and transaction data to a JSON file
        public async Task<string> ExportPortfolioJsonAsync(
            Portfolio portfolio, string username = "user")
        {
            string filename = string.Format(
                "report_{0}_{1:yyyyMMdd_HHmmss}.json",
                username,
                DateTime.Now);

            string filepath = Path.Combine(_reportsDirectory, filename);

            // Construct anonymous object for structured JSON reporting
            var reportData = new
            {
                GeneratedAt = DateTime.UtcNow
                    .ToString("yyyy-MM-dd HH:mm:ss"),
                Username = username,
                PortfolioName = portfolio.Name,
                WalletBalance = portfolio.Wallet.Balance,
                TotalPortfolioValue = portfolio.GetTotalValue(),
                Assets = portfolio.GetAllAssets().Select(a => new
                {
                    Type = a.AssetType,
                    Symbol = a.Symbol,
                    Name = a.Name,
                    Quantity = a.Quantity,
                    CurrentPrice = a.CurrentPrice,
                    TotalValue = a.CalculateValue(),
                    LastUpdated = a.LastUpdated
                        .ToString("yyyy-MM-dd HH:mm:ss")
                }).ToList(),
                Transactions = portfolio.Transactions
                    .OrderByDescending(t => t.Timestamp)
                    .Select(t => new
                    {
                        Timestamp = t.Timestamp
                            .ToString("yyyy-MM-dd HH:mm:ss"),
                        Type = t.Type.ToString(),
                        Symbol = t.AssetSymbol,
                        AssetType = t.AssetType,
                        Quantity = t.Quantity,
                        PricePerUnit = t.PricePerUnit,
                        TotalCost = t.TotalCost,
                        FeeApplied = t.FeeApplied,
                        Notes = t.Notes
                    }).ToList()
            };

            // Serialize data using System.Text.Json with pretty-printing enabled
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(reportData, options);

            await File.WriteAllTextAsync(
                filepath, json, Encoding.UTF8);

            return filepath;
        }

        // Returns a list of all filenames in the reports directory
        public IEnumerable<string> ListReports()
        {
            return Directory
                .GetFiles(_reportsDirectory, "*.*")
                .Select(Path.GetFileName);
        }

        public string ReportsDirectory => _reportsDirectory;
    }
}