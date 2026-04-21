using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TradeBot.Models;
using TradeBot.Services;
using System.Globalization;

namespace TradeBot.Utils
{
    // All display and input/output logic is handled here
    // Business logic is excluded — this class serves only as the UI layer
    public static class ConsoleUI
    {
        private static readonly string Separator = new string('=', 60);
        private static readonly string ThinLine = new string('-', 60);

        // ── DISPLAY ───────────────────────────────────────────────

        public static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                 TradeBot v1.0                            ║");
            Console.WriteLine("║     Console Portfolio Simulator  |  C# .NET Framework    ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }

        public static void PrintSection(string title)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n" + Separator);
            Console.WriteLine("  " + title);
            Console.WriteLine(Separator);
            Console.ResetColor();
        }

        public static void PrintPortfolio(Portfolio portfolio)
        {
            PrintSection("PORTFOLIO OVERVIEW");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("  " + portfolio.ToString());
            Console.ResetColor();

            Console.WriteLine(ThinLine);

            List<Asset> assets = portfolio.GetAllAssets();

            if (assets.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No assets found. Start by using 'Add Asset'.");
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            foreach (Asset asset in assets)
                Console.WriteLine("  " + asset.ToString());

            Console.ResetColor();
            Console.WriteLine(ThinLine);
        }

        public static void PrintTransactionHistory(Portfolio portfolio, int limit = 10)
        {
            PrintSection("TRANSACTION HISTORY");

            List<Transaction> txs = portfolio.Transactions
                .OrderByDescending(t => t.Timestamp)
                .Take(limit)
                .ToList();

            if (txs.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No transactions have been recorded yet.");
                Console.ResetColor();
                return;
            }

            foreach (Transaction tx in txs)
            {
                Console.ForegroundColor = tx.Type == TransactionType.Buy
                    ? ConsoleColor.Green
                    : ConsoleColor.Red;
                Console.WriteLine("  " + tx.ToString());
            }

            Console.ResetColor();
        }

        public static void PrintMarketUpdate(List<Asset> assets)
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  LIVE MARKET  |  " + DateTime.Now.ToString("HH:mm:ss"));
            Console.ResetColor();

            foreach (Asset asset in assets)
            {
                Console.ForegroundColor = asset is CryptoAsset
                    ? ConsoleColor.Magenta
                    : ConsoleColor.Blue;
                Console.WriteLine("  " + asset.GetMarketUpdate());
            }

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n  Press Enter to go back...");
            Console.ResetColor();
        }

        // ── MENUS ─────────────────────────────────────────────────

        public static string ShowMainMenu(string username)
        {
            PrintSection("MAIN MENU  |  User: " + username);
            Console.WriteLine("  [P] View Portfolio");
            Console.WriteLine("  [AN] Portfolio Analytics");
            Console.WriteLine("  [M] Market Snapshot");
            Console.WriteLine("  [CH] View Price Chart");
            Console.WriteLine("  [B] Buy Asset");
            Console.WriteLine("  [S] Sell Asset");
            Console.WriteLine("  [SL] Set Stop Loss / Take Profit");
            Console.WriteLine("  [VA] View Active Alerts");
            Console.WriteLine("  [A] Add New Asset");
            Console.WriteLine("  [H] Transaction History");
            Console.WriteLine("  [R] Generate Report");
            Console.WriteLine("  [D] Deposit Funds");
            Console.WriteLine("  [TG] Set Trading Goal");
            Console.WriteLine("  [VG] View Goal Progress");
            Console.WriteLine("  [GH] Goal History");
            Console.WriteLine("  [X] Logout");
            Console.Write("\n  Enter your choice: ");
            string input = Console.ReadLine();
            return input != null ? input.Trim().ToUpper() : "";
        }

        public static string ShowAuthMenu()
        {
            PrintSection("TRADEBOT - LOGIN / SIGNUP");
            Console.WriteLine("  [L] Login");
            Console.WriteLine("  [S] Sign Up");
            Console.WriteLine("  [Q] Quit");
            Console.Write("\n  Enter your choice: ");
            string input = Console.ReadLine();
            return input != null ? input.Trim().ToUpper() : "";
        }

        // ── INPUT HELPERS ─────────────────────────────────────────

        public static string Prompt(string message)
        {
            Console.Write("  " + message + ": ");
            string input = Console.ReadLine();
            return input != null ? input.Trim() : "";
        }

        public static string PromptPassword(string message)
        {
            Console.Write("  " + message + ": ");
            StringBuilder password = new StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password.Append(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password.ToString();
        }

        public static decimal PromptDecimal(string message)
        {
            while (true)
            {
                Console.Write("  " + message + ": ");
                decimal value;
                if (decimal.TryParse(Console.ReadLine(), out value) && value > 0)
                    return value;
                PrintError("Please enter a positive numeric value.");
            }
        }

        // ── MESSAGES ──────────────────────────────────────────────

        public static void PrintSuccess(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  >> SUCCESS: " + message);
            Console.ResetColor();
            Pause();
        }

        public static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  !! ERROR: " + message);
            Console.ResetColor();
        }

        public static void PrintInfo(string message)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n  -- INFO: " + message);
            Console.ResetColor();
        }

        public static void Pause(string msg = null)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(msg ?? "\n  Press Enter to continue...");
            Console.ResetColor();
            Console.ReadLine();
        }

        public static Asset BuildAssetInteractively()
        {
            Console.WriteLine("\n  Select Asset Type:");
            Console.WriteLine("    [C] Crypto  (e.g., BTC, ETH)");
            Console.WriteLine("    [S] Stock   (e.g., AAPL, MSFT)");
            Console.Write("  Choice: ");
            string type = Console.ReadLine()?.Trim().ToUpper();

            if (type == "C")
            {
                CryptoAsset crypto = new CryptoAsset();
                crypto.Symbol = Prompt("Symbol (e.g., BTC)");
                crypto.Name = Prompt("Full Name (e.g., Bitcoin)");
                crypto.CurrentPrice = PromptDecimal("Current Price (USD)");
                crypto.GasFee = PromptDecimal("Gas fee per trade (USD)");
                crypto.NetworkFeePercent = PromptDecimal("Network fee % (e.g., 0.1)");
                crypto.Blockchain = Prompt("Blockchain (e.g., Ethereum)");
                crypto.IsDefi = Prompt("Is it DeFi? (y/n)").ToLower() == "y";
                crypto.Quantity = 0;
                return crypto;
            }
            else if (type == "S")
            {
                StockAsset stock = new StockAsset();
                stock.Symbol = Prompt("Symbol (e.g., AAPL)");
                stock.Name = Prompt("Company Name");
                stock.CurrentPrice = PromptDecimal("Current Price (USD)");
                stock.Exchange = Prompt("Exchange (e.g., NYSE / NASDAQ)");
                stock.Sector = Prompt("Sector (e.g., Technology)");
                stock.DividendYield = PromptDecimal("Dividend yield % (enter 0 if none)");
                stock.PriceEarningsRatio = PromptDecimal("P/E Ratio");
                stock.CommissionFee = PromptDecimal("Broker commission per trade (USD)");
                stock.Quantity = 0;
                return stock;
            }

            PrintError("Invalid type selection. Please choose C or S.");
            return null;
        }

        public static void PrintAnalytics(PortfolioAnalytics analytics)
        {
            PrintSection("PORTFOLIO ANALYTICS");

            if (analytics.AssetAnalytics.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No assets found or no trades have been executed yet.");
                Console.ResetColor();
                Pause();
                return;
            }

            // ── OVERALL SUMMARY ───────────────────────────────────
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("  OVERALL SUMMARY");
            Console.WriteLine("  " + new string('-', 50));

            Console.Write("  Total Invested:      ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{analytics.TotalInvested:C2}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Current Value:       ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"{analytics.TotalCurrentValue:C2}");

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Total Profit/Loss:   ");

            if (analytics.TotalProfitLoss >= 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"+{analytics.TotalProfitLoss:C2} (+{analytics.TotalProfitLossPercent:F2}%)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"-{Math.Abs(analytics.TotalProfitLoss):C2} ({analytics.TotalProfitLossPercent:F2}%)");
            }

            // ── ASSET WISE BREAKDOWN ──────────────────────────────
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n  ASSET BREAKDOWN");
            Console.WriteLine("  " + new string('-', 50));

            foreach (var asset in analytics.AssetAnalytics)
            {
                Console.ForegroundColor = asset.AssetType == "Crypto" ? ConsoleColor.Magenta : ConsoleColor.Blue;
                Console.WriteLine($"\n  [{asset.AssetType}] {asset.Symbol}");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("    Invested:    ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{asset.InvestedAmount:C2}");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("    Current:     ");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"{asset.CurrentValue:C2}");

                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("    P/L:         ");

                if (asset.ProfitLoss >= 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"+{asset.ProfitLoss:C2} (+{asset.ProfitLossPercent:F2}%) >>");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"-{Math.Abs(asset.ProfitLoss):C2} ({asset.ProfitLossPercent:F2}%) <<");
                }
            }

            // ── PERFORMANCE ────────────────────────────────────
            Console.WriteLine("\n  " + new string('-', 50));

            if (analytics.BestPerformer != null)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  BEST PERFORMER:   ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{analytics.BestPerformer.Symbol}  +{analytics.BestPerformer.ProfitLossPercent:F2}%");
            }

            if (analytics.WorstPerformer != null)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("  WORST PERFORMER:  ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{analytics.WorstPerformer.Symbol}  {analytics.WorstPerformer.ProfitLossPercent:F2}%");
            }

            Console.ResetColor();
            Console.WriteLine();
            Pause();
        }

        public static string ShowAlertTypeMenu()
        {
            PrintSection("STOP LOSS / TAKE PROFIT");
            Console.WriteLine("  [1] Stop Loss");
            Console.WriteLine("      → Automatically sell if price drops below target");
            Console.WriteLine("  [2] Take Profit");
            Console.WriteLine("      → Automatically sell if price reaches profit target");
            Console.Write("\n  Select Option: ");
            string input = Console.ReadLine();
            return input != null ? input.Trim() : "";
        }

        public static void PrintActiveAlerts(List<PriceAlert> alerts)
        {
            PrintSection("ACTIVE ALERTS");

            if (alerts.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No active alerts found.");
                Console.ResetColor();
                Pause();
                return;
            }

            foreach (PriceAlert alert in alerts)
            {
                Console.ForegroundColor = alert.Type == AlertType.StopLoss ? ConsoleColor.Red : ConsoleColor.Green;
                Console.WriteLine($"  [{alert.Id}] {alert.ToString()}");
            }

            Console.ResetColor();
            Console.WriteLine();
            Pause();
        }

        public static void PrintAlertNotification(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  *** ALERT TRIGGERED: " + message + " ***");
            Console.ResetColor();
        }

        public static void PrintPriceChart(string symbol, List<PricePoint> history, decimal highPrice, decimal lowPrice, decimal lastPrice, decimal firstPrice)
        {
            PrintSection(symbol + " — PRICE CHART");

            if (history.Count < 2)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Insufficient data to display chart.");
                Console.WriteLine("  Please wait for more price updates...");
                Console.ResetColor();
                Pause();
                return;
            }

            int chartHeight = 10;
            decimal priceRange = highPrice - lowPrice;
            if (priceRange == 0) priceRange = 1;

            int[] rows = new int[history.Count];
            for (int i = 0; i < history.Count; i++)
            {
                decimal normalized = (history[i].Price - lowPrice) / priceRange;
                rows[i] = (int)(normalized * (chartHeight - 1));
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();

            for (int row = chartHeight - 1; row >= 0; row--)
            {
                decimal rowPrice = lowPrice + (priceRange * row / (chartHeight - 1));
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{rowPrice,12:F2} |");

                for (int col = 0; col < history.Count; col++)
                {
                    if (rows[col] == row)
                    {
                        if (col > 0 && history[col].Price > history[col - 1].Price)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.Write("  *");
                        }
                        else if (col > 0 && history[col].Price < history[col - 1].Price)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.Write("  *");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.White;
                            Console.Write("  *");
                        }
                    }
                    else if (col > 0 && row > Math.Min(rows[col], rows[col - 1]) && row < Math.Max(rows[col], rows[col - 1]))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write("  |");
                    }
                    else
                    {
                        Console.Write("   ");
                    }
                }
                Console.WriteLine();
            }

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.Write("             +");
            for (int i = 0; i < history.Count; i++) Console.Write("---");
            Console.WriteLine("\n               ");
            for (int i = 0; i < history.Count; i++) Console.Write($"{i + 1,3}");
            Console.WriteLine("\n                    (Ticks — 1 tick = 5 seconds)");

            Console.WriteLine("\n" + new string('-', 40));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  HIGH:  "); Console.ForegroundColor = ConsoleColor.Green; Console.WriteLine($"{highPrice:C2}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  LOW:   "); Console.ForegroundColor = ConsoleColor.Red; Console.WriteLine($"{lowPrice:C2}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  LAST:  "); Console.ForegroundColor = ConsoleColor.Cyan; Console.WriteLine($"{lastPrice:C2}");

            decimal change = lastPrice - firstPrice;
            decimal changePercent = firstPrice > 0 ? (change / firstPrice) * 100 : 0;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  CHANGE: ");
            Console.ForegroundColor = change >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"{change:+#.##;-#.##;0.00} ({changePercent:F2}%)");

            Console.ResetColor();
            Pause();
        }

        public static string ShowChartSymbolMenu(List<string> symbols)
        {
            PrintSection("PRICE CHART — SELECT SYMBOL");

            if (symbols.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No price data available yet.");
                Console.WriteLine("  Market simulation is initializing...");
                Console.ResetColor();
                Pause();
                return "";
            }

            for (int i = 0; i < symbols.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {symbols[i]}");
            }

            Console.Write("\n  Enter Symbol (e.g., BTC): ");
            string input = Console.ReadLine();
            return input != null ? input.Trim().ToUpper() : "";
        }

        public static DateTime PromptDate(string message)
        {
            while (true)
            {
                Console.Write("  " + message + " (dd/mm/yyyy): ");
                string input = Console.ReadLine();
                DateTime date;
                if (DateTime.TryParseExact(
                    input, "dd/MM/yyyy",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out date))
                {
                    if (date > DateTime.Now)
                        return date;
                    PrintError("Date should be in the future.");
                }
                else
                {
                    PrintError("Incorrect format. Use dd/mm/yyyy.");
                }
            }
        }

        public static void PrintGoalProgress(TradingGoal goal, decimal currentValue)
        {
            PrintSection("TRADING GOAL PROGRESS");

            if (goal == null)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No active goal.");
                Console.WriteLine("  [TG] Set new goal.");
                Console.ResetColor();
                Pause();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("  Goal Name:      {0}", goal.GoalName));
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("  Target Amount:  ${0:F2}", goal.TargetAmount));
            Console.WriteLine(string.Format("  Starting Point: ${0:F2}", goal.StartingAmount));
            Console.WriteLine(string.Format("  Current Value:  ${0:F2}", currentValue));

            Console.WriteLine();

            decimal percent = goal.GetProgressPercent(currentValue);
            int daysLeft = goal.GetDaysRemaining();
            decimal dailyTarget = goal.GetDailyTarget(currentValue);
            decimal remaining = goal.TargetAmount - currentValue;

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("  Progress:      [");

            int totalBars = 30;
            int filledBars = (int)(percent / 100 * totalBars);

            Console.ForegroundColor = ConsoleColor.Green;
            for (int i = 0; i < filledBars; i++)
                Console.Write("█");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            for (int i = filledBars; i < totalBars; i++)
                Console.Write("░");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("] {0:F1}%", percent));

            Console.WriteLine();

            if (remaining > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Format("  Remaining:      ${0:F2}", remaining));
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("  Goal COMPLETED!");
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(string.Format("  Target Date:    {0:dd/MM/yyyy}", goal.TargetDate));

            if (daysLeft > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(string.Format("  Time Remaining: {0} days", daysLeft));

                if (dailyTarget > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(string.Format("  Daily Target:   ${0:F2} per day", dailyTarget));
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  Target date has expired!");
            }

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            if (percent >= 75)
                Console.WriteLine("  *** Excellent! You are almost there! ***");
            else if (percent >= 50)
                Console.WriteLine("  *** Halfway point reached! Keep going! ***");
            else if (percent >= 25)
                Console.WriteLine("  *** On the right track! ***");
            else
                Console.WriteLine("  *** Stay focused! The journey has begun! ***");

            Console.ResetColor();
            Console.WriteLine();
            Pause();
        }

        public static void PrintGoalHistory(List<TradingGoal> goals)
        {
            PrintSection("GOAL HISTORY");

            if (goals == null || goals.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  No goal history found.");
                Console.ResetColor();
                Pause();
                return;
            }

            foreach (TradingGoal goal in goals)
            {
                if (goal.IsCompleted)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (!goal.IsActive)
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                else
                    Console.ForegroundColor = ConsoleColor.Cyan;

                string status = goal.IsCompleted
                    ? "COMPLETE"
                    : goal.IsActive
                        ? "ACTIVE"
                        : "CANCELLED";

                Console.WriteLine(string.Format(
                    "  [{0}] {1} | Target: ${2:F2} | Date: {3:dd/MM/yyyy}",
                    status,
                    goal.GoalName,
                    goal.TargetAmount,
                    goal.TargetDate));
            }

            Console.ResetColor();
            Console.WriteLine();
            Pause();
        }
    }
}