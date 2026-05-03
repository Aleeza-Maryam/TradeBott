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
            Console.WriteLine("  [RC] Risk Calculator");
            Console.WriteLine("  [TG] Set Trading Goal");
            Console.WriteLine("  [VG] View Goal Progress");
            Console.WriteLine("  [GH] Goal History");
            Console.WriteLine("  [L]  Leaderboard & Competition");
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

        // Displays the risk calculation summary produced by RiskManagementService
        public static void PrintRiskSummary(RiskCalculationResult res)
        {
            PrintSection("RISK & REWARD CALCULATOR");

            if (res == null)
            {
                PrintError("No result to display.");
                return;
            }

            if (!res.IsValid)
            {
                PrintError(res.ErrorMessage ?? "Invalid parameters provided.");
                return;
            }

            Console.WriteLine($"  Balance:           ${res.Balance:F2}");
            Console.WriteLine($"  Entry Price:       ${res.EntryPrice:F8}");
            Console.WriteLine($"  Stop Loss:         ${res.StopLoss:F8}");
            Console.WriteLine($"  Risk %:            {res.RiskPercent:F2}%");
            Console.WriteLine($"  Risk Amount:       ${res.RiskAmount:F8}");
            Console.WriteLine($"  Recommended Qty:   {res.PositionSize:F8} (units)");
            Console.WriteLine($"  TP (1:2 R:R):      ${res.TakeProfit_1to2:F8}");
            Console.WriteLine($"  TP (1:3 R:R):      ${res.TakeProfit_1to3:F8}");

            Console.WriteLine();
            Pause();
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
            if (msg != null)
                Console.WriteLine(msg);
            else
                Console.WriteLine("\n  Press Enter to continue...");
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

        // Leaderboard display method
        public static void PrintLeaderboard(List<LeaderboardEntry> leaderboard)
        {
            PrintSection("🏆 GLOBAL LEADERBOARD 🏆");

            if (leaderboard == null || !leaderboard.Any())
            {
                PrintInfo("No users found in leaderboard yet!");
                return;
            }

            Console.WriteLine();
            // Simple aligned header to avoid complex box characters and width issues
            Console.WriteLine($"{"RANK",-8} {"USERNAME",-22} {"PORTFOLIO VALUE",18} {"PROFIT/LOSS",14} {"CHANGE %",10}");
            Console.WriteLine(new string('-', 80));

            foreach (var entry in leaderboard)
            {
                string medal = entry.Rank == 1 ? "" : entry.Rank == 2 ? "" : entry.Rank == 3 ? "" : "";
                string rankDisplay = medal != "" ? $"{entry.Rank} {medal}" : $"#{entry.Rank}";

                ConsoleColor profitColor = entry.TotalProfitLoss >= 0 ? ConsoleColor.Green : ConsoleColor.Red;

                // Format values consistently with header
                Console.Write($"{rankDisplay,-8} ");
                Console.Write($"{entry.Username,-22} ");
                Console.Write($"{entry.PortfolioValue,18:C2} ");

                Console.ForegroundColor = profitColor;
                Console.Write($"{entry.TotalProfitLoss,14:C2} ");
                Console.Write($"{entry.ProfitLossPercentage,9:F2}%");
                Console.ResetColor();

                Console.WriteLine();
            }

            Console.WriteLine("└──────┴────────────────────────┴──────────────────────┴────────────┴─────────────┘");

            if (leaderboard.Any())
            {
                var winner = leaderboard.First();
                PrintInfo($"\n Current Leader: {winner.Username} with ${winner.PortfolioValue:F2}! ");
            }
        }

        // User rank display
        public static void PrintUserRank(LeaderboardEntry userRank, LeaderboardSummary summary)
        {
            PrintSection("YOUR RANKING ");

            if (userRank == null)
            {
                PrintInfo("You are not on the leaderboard yet!");
                return;
            }

            Console.WriteLine($"\n  Your Username: {userRank.Username}");
            Console.WriteLine($"  Your Rank: #{userRank.Rank} out of {summary.TotalUsers} users");
            Console.WriteLine($"  Your Portfolio Value: ${userRank.PortfolioValue:F2}");

            Console.ForegroundColor = userRank.TotalProfitLoss >= 0 ? ConsoleColor.Green : ConsoleColor.Red;
            Console.WriteLine($"  Your Profit/Loss: ${userRank.TotalProfitLoss:F2} ({userRank.ProfitLossPercentage:F2}%)");
            Console.ResetColor();

            Console.WriteLine($"\n  Leaderboard Stats:");
            Console.WriteLine($"  ───────────────────");
            Console.WriteLine($"  Total Users: {summary.TotalUsers}");
            Console.WriteLine($"  Average Value: ${summary.AveragePortfolioValue:F2}");
            Console.WriteLine($"  Highest Value: ${summary.HighestValue:F2}");
            Console.WriteLine($"  Lowest Value: ${summary.LowestValue:F2}");
        }

        // Competition menu
        public static string ShowCompetitionMenu()
        {
            PrintSection("🏆 COMPETITION ZONE 🏆");
            Console.WriteLine("  [1] View Global Leaderboard");
            Console.WriteLine("  [2] View Top 10 Traders");
            Console.WriteLine("  [3] My Rank & Stats");
            Console.WriteLine("  [4] Best Performer");
            Console.WriteLine("  [5] Back to Main Menu");
            Console.Write("\n  Choice: ");
            string input = Console.ReadLine();
            return input != null ? input.Trim() : "";
        }

        // Goal progress visualization
        public static void PrintGoalProgress(TradingGoal goal, decimal currentAmount)
        {
            PrintSection("TRADING GOAL — PROGRESS");

            if (goal == null)
            {
                PrintInfo("No active trading goal found.");
                return;
            }

            decimal progressPercent = goal.GetProgressPercent(currentAmount);
            int daysRemaining = goal.GetDaysRemaining();
            decimal dailyTarget = goal.GetDailyTarget(currentAmount);

            Console.WriteLine($"  Goal: {goal.GoalName}");
            Console.WriteLine($"  Target Amount: ${goal.TargetAmount:F2}");
            Console.WriteLine($"  Starting Amount: ${goal.StartingAmount:F2}");
            Console.WriteLine($"  Current Portfolio Value: ${currentAmount:F2}");
            Console.WriteLine($"  Progress: {progressPercent:F1}%");
            Console.WriteLine($"  Days Remaining: {daysRemaining}");
            Console.WriteLine($"  Daily Required (approx): ${dailyTarget:F2}");
            Console.WriteLine($"  Target Date: {goal.TargetDate:dd/MM/yyyy}");
            Console.WriteLine($"  Status: {(goal.IsCompleted ? "Completed" : goal.IsActive ? "Active" : "Inactive")}");

            Console.WriteLine();
            Pause();
        }

        // Display history of goals for a portfolio
        public static void PrintGoalHistory(List<TradingGoal> goals)
        {
            PrintSection("GOAL HISTORY");

            if (goals == null || goals.Count == 0)
            {
                PrintInfo("No goals found for this portfolio.");
                return;
            }

            foreach (var g in goals)
            {
                string status = g.IsCompleted ? "Completed" : g.IsActive ? "Active" : "Inactive";
                Console.ForegroundColor = g.IsCompleted ? ConsoleColor.Green : ConsoleColor.White;
                Console.WriteLine($"  {g.GoalName} — Target: ${g.TargetAmount:F2} | Set: {g.CreatedAt:dd/MM/yyyy} | Due: {g.TargetDate:dd/MM/yyyy} | Status: {status}");
                Console.ResetColor();
            }

            Console.WriteLine();
            Pause();
        }
    }
}