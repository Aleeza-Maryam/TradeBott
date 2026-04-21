using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using TradeBot.Data;
using TradeBot.Models;
using TradeBot.Repositories;
using TradeBot.Services;
using TradeBot.Utils;

namespace TradeBot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // ── MongoDB Connection ────────────────────────────────
            string connectionString = "mongodb://localhost:27017";
            string databaseName = "TradeBotDB";

            MongoDbContext context = new MongoDbContext(
                connectionString, databaseName);

            // Create indexes for optimized search performance
            context.CreateIndexes();

            // ── Repositories ──────────────────────────────────────
            UserRepository userRepo = new UserRepository(context);
            PortfolioRepository portfolioRepo =
                new PortfolioRepository(context);

            // ── Services ──────────────────────────────────────────
            AuthService authService = new AuthService(
                userRepo, portfolioRepo);
            TradingEngine tradingEngine =
                new TradingEngine(portfolioRepo);
            MarketSimulator simulator = new MarketSimulator();
            ReportingService reporter = new ReportingService();
            AnalyticsService analyticsService = new AnalyticsService();
            PriceHistoryService priceHistory =
                new PriceHistoryService();
            GoalService goalService = new GoalService(context);

            // ── Banner ────────────────────────────────────────────
            ConsoleUI.PrintBanner();
            ConsoleUI.PrintInfo("Connecting to MongoDB...");
            ConsoleUI.PrintSuccess("Database is ready!");

            // ── AUTH LOOP ─────────────────────────────────────────
            User currentUser = null;
            Portfolio portfolio = null;

            while (currentUser == null)
            {
                string authChoice = ConsoleUI.ShowAuthMenu();

                if (authChoice == "Q")
                {
                    Console.WriteLine("  Goodbye!");
                    return;
                }
                else if (authChoice == "L")
                {
                    string username = ConsoleUI.Prompt("Username");
                    string password = ConsoleUI.PromptPassword("Password");

                    var result = await authService.LoginAsync(
                        username, password);

                    if (result.Item1)
                    {
                        currentUser = result.Item3;
                        portfolio = await portfolioRepo
                            .GetByUserIdAsync(currentUser.Id);
                        ConsoleUI.PrintSuccess(result.Item2);
                    }
                    else
                        ConsoleUI.PrintError(result.Item2);
                }
                else if (authChoice == "S")
                {
                    string username = ConsoleUI.Prompt("Username");
                    string email = ConsoleUI.Prompt("Email");
                    string password = ConsoleUI.PromptPassword("Password");

                    var result = await authService.SignupAsync(
                        username, password, email);

                    if (result.Item1)
                    {
                        currentUser = result.Item3;
                        portfolio = await portfolioRepo
                            .GetByUserIdAsync(currentUser.Id);
                        ConsoleUI.PrintSuccess(result.Item2);
                    }
                    else
                        ConsoleUI.PrintError(result.Item2);
                }
            }

            if (portfolio == null)
            {
                ConsoleUI.PrintError(
                    "Portfolio not found. Application is shutting down.");
                return;
            }

            // ── Default Assets ────────────────────────────────────
            // Initialize portfolio with default assets if empty
            if (portfolio.GetAllAssets().Count == 0)
            {
                await tradingEngine.AddAssetAsync(portfolio,
                    new CryptoAsset
                    {
                        Symbol = "BTC",
                        Name = "Bitcoin",
                        CurrentPrice = 67420.50m,
                        GasFee = 2.50m,
                        NetworkFeePercent = 0.1m,
                        Blockchain = "Bitcoin",
                        IsDefi = false,
                        Quantity = 0
                    });

                await tradingEngine.AddAssetAsync(portfolio,
                    new CryptoAsset
                    {
                        Symbol = "ETH",
                        Name = "Ethereum",
                        CurrentPrice = 3512.80m,
                        GasFee = 1.20m,
                        NetworkFeePercent = 0.15m,
                        Blockchain = "Ethereum",
                        IsDefi = true,
                        Quantity = 0
                    });

                await tradingEngine.AddAssetAsync(portfolio,
                    new StockAsset
                    {
                        Symbol = "AAPL",
                        Name = "Apple Inc.",
                        CurrentPrice = 189.30m,
                        Exchange = "NASDAQ",
                        Sector = "Technology",
                        DividendYield = 0.5m,
                        PriceEarningsRatio = 28.5m,
                        CommissionFee = 9.99m,
                        Quantity = 0
                    });

                await tradingEngine.AddAssetAsync(portfolio,
                    new StockAsset
                    {
                        Symbol = "MSFT",
                        Name = "Microsoft Corporation",
                        CurrentPrice = 378.85m,
                        Exchange = "NASDAQ",
                        Sector = "Technology",
                        DividendYield = 0.7m,
                        PriceEarningsRatio = 32.1m,
                        CommissionFee = 9.99m,
                        Quantity = 0
                    });

                portfolio = await portfolioRepo
                    .GetByUserIdAsync(currentUser.Id);

                ConsoleUI.PrintInfo(
                    "Default assets loaded: BTC, ETH, AAPL, MSFT");
            }

            // ── Alert Service Setup ───────────────────────────────
            AlertService alertService =
                new AlertService(tradingEngine);
            simulator.SetAlertService(alertService);

            // ── Market Simulation ─────────────────────────────────
            simulator.Start(portfolio);

            // Record price history on each update
            simulator.OnPriceUpdate += (assets) =>
            {
                priceHistory.RecordAllPrices(portfolio);

                // Check if goal has been reached on every simulation tick
                Task.Run(async () =>
                {
                    var result = await goalService
                        .CheckGoalCompletionAsync(portfolio);

                    if (result.Item1)
                        ConsoleUI.PrintAlertNotification(result.Item2);
                });
            };

            // Handle alert notifications
            simulator.OnAlertTriggered += (message) =>
            {
                ConsoleUI.PrintAlertNotification(message);
            };

            ConsoleUI.PrintInfo(
                "Market simulation started! Updates occur every 5 seconds.");

            // ── MAIN APP LOOP ─────────────────────────────────────
            bool running = true;

            while (running)
            {
                string choice = ConsoleUI.ShowMainMenu(
                    currentUser.Username);

                // Fetch fresh portfolio data from DB to ensure accuracy
                Portfolio freshPortfolio = await portfolioRepo
                    .GetByUserIdAsync(currentUser.Id) ?? portfolio;

                switch (choice)
                {
                    case "P":
                        ConsoleUI.PrintPortfolio(freshPortfolio);
                        ConsoleUI.Pause();
                        break;

                    case "M":
                        ConsoleUI.PrintMarketUpdate(
                            freshPortfolio.GetAllAssets());
                        Console.ReadLine();
                        break;

                    case "B":
                        ConsoleUI.PrintSection("BUY ASSET");
                        string buySymbol = ConsoleUI
                            .Prompt("Symbol (e.g., BTC)").ToUpper();
                        decimal buyQty = ConsoleUI
                            .PromptDecimal("Enter quantity");

                        var buyResult = await tradingEngine.BuyAsync(
                            freshPortfolio, buySymbol, buyQty);

                        if (buyResult.Item1)
                            ConsoleUI.PrintSuccess(buyResult.Item2);
                        else
                        {
                            ConsoleUI.PrintError(buyResult.Item2);
                            ConsoleUI.Pause();
                        }
                        break;

                    case "S":
                        ConsoleUI.PrintSection("SELL ASSET");
                        string sellSymbol = ConsoleUI
                            .Prompt("Symbol (e.g., BTC)").ToUpper();
                        decimal sellQty = ConsoleUI
                            .PromptDecimal("Enter quantity");

                        var sellResult = await tradingEngine.SellAsync(
                            freshPortfolio, sellSymbol, sellQty);

                        if (sellResult.Item1)
                            ConsoleUI.PrintSuccess(sellResult.Item2);
                        else
                        {
                            ConsoleUI.PrintError(sellResult.Item2);
                            ConsoleUI.Pause();
                        }
                        break;

                    case "A":
                        ConsoleUI.PrintSection("ADD NEW ASSET");
                        Asset newAsset =
                            ConsoleUI.BuildAssetInteractively();
                        if (newAsset != null)
                        {
                            var addResult = await tradingEngine
                                .AddAssetAsync(freshPortfolio, newAsset);

                            if (addResult.Item1)
                                ConsoleUI.PrintSuccess(addResult.Item2);
                            else
                                ConsoleUI.PrintError(addResult.Item2);
                        }
                        break;

                    case "AN":
                        PortfolioAnalytics analytics = analyticsService
                            .GetPortfolioAnalytics(freshPortfolio);
                        ConsoleUI.PrintAnalytics(analytics);
                        break;

                    case "SL":
                        ConsoleUI.PrintSection(
                            "STOP LOSS / TAKE PROFIT");
                        string alertSymbol = ConsoleUI
                            .Prompt("Symbol (e.g., BTC)").ToUpper();

                        string alertTypeChoice =
                            ConsoleUI.ShowAlertTypeMenu();
                        AlertType alertType;

                        if (alertTypeChoice == "1")
                            alertType = AlertType.StopLoss;
                        else if (alertTypeChoice == "2")
                            alertType = AlertType.TakeProfit;
                        else
                        {
                            ConsoleUI.PrintError("Invalid choice.");
                            break;
                        }

                        decimal targetPrice = ConsoleUI
                            .PromptDecimal("Target price (USD)");
                        decimal alertQty = ConsoleUI
                            .PromptDecimal("Quantity to sell on trigger");

                        var alertResult = await alertService.AddAlertAsync(
                            freshPortfolio,
                            alertSymbol,
                            alertType,
                            targetPrice,
                            alertQty);

                        if (alertResult.Item1)
                            ConsoleUI.PrintSuccess(alertResult.Item2);
                        else
                        {
                            ConsoleUI.PrintError(alertResult.Item2);
                            ConsoleUI.Pause();
                        }
                        break;

                    case "VA":
                        List<PriceAlert> activeAlerts = alertService
                            .GetActiveAlerts(freshPortfolio.Id);
                        ConsoleUI.PrintActiveAlerts(activeAlerts);
                        break;

                    case "CH":
                        List<string> availableSymbols =
                            priceHistory.GetAllSymbols();
                        string chartSymbol = ConsoleUI
                            .ShowChartSymbolMenu(availableSymbols);

                        if (!string.IsNullOrEmpty(chartSymbol))
                        {
                            List<PricePoint> chartHistory =
                                priceHistory.GetHistory(chartSymbol);

                            if (chartHistory.Count < 2)
                            {
                                ConsoleUI.PrintError(
                                    "Insufficient data. Please wait for more market cycles.");
                                ConsoleUI.Pause();
                            }
                            else
                            {
                                ConsoleUI.PrintPriceChart(
                                    chartSymbol,
                                    chartHistory,
                                    priceHistory.GetHighPrice(chartSymbol),
                                    priceHistory.GetLowPrice(chartSymbol),
                                    priceHistory.GetLastPrice(chartSymbol),
                                    priceHistory.GetFirstPrice(chartSymbol));
                            }
                        }
                        break;

                    case "H":
                        ConsoleUI.PrintTransactionHistory(freshPortfolio);
                        ConsoleUI.Pause();
                        break;

                    case "R":
                        ConsoleUI.PrintSection("GENERATE REPORT");
                        Console.WriteLine("  [1] Transaction history (CSV)");
                        Console.WriteLine("  [2] Portfolio snapshot (CSV)");
                        Console.WriteLine("  [3] Full report (JSON)");
                        Console.Write("  Choice: ");
                        string rChoice = Console.ReadLine();

                        string reportPath = null;

                        if (rChoice == "1")
                            reportPath = await reporter
                                .ExportTransactionsCsvAsync(
                                    freshPortfolio,
                                    currentUser.Username);
                        else if (rChoice == "2")
                            reportPath = await reporter
                                .ExportPortfolioSnapshotCsvAsync(
                                    freshPortfolio,
                                    currentUser.Username);
                        else if (rChoice == "3")
                            reportPath = await reporter
                                .ExportPortfolioJsonAsync(
                                    freshPortfolio,
                                    currentUser.Username);

                        if (reportPath != null)
                            ConsoleUI.PrintSuccess(
                                "Report saved successfully: " + reportPath);
                        else
                            ConsoleUI.PrintError("Invalid choice.");
                        break;

                    case "D":
                        ConsoleUI.PrintSection("DEPOSIT FUNDS");
                        decimal depositAmt = ConsoleUI
                            .PromptDecimal("Amount to deposit (USD)");
                        freshPortfolio.Wallet.Deposit(depositAmt);
                        await portfolioRepo.UpdateAsync(freshPortfolio);
                        ConsoleUI.PrintSuccess(string.Format(
                            "${0:F2} deposited. New balance: ${1:F2}",
                            depositAmt,
                            freshPortfolio.Wallet.Balance));
                        break;
                    case "TG":
                        ConsoleUI.PrintSection("Set your trading goal");

                        string goalName = ConsoleUI.Prompt("Goal name");
                        decimal targetAmount = ConsoleUI.PromptDecimal(
                            "Target amount (USD)");
                        DateTime targetDate = ConsoleUI.PromptDate(
                            "Target date");

                        var goalResult = await goalService.SetGoalAsync(
                            freshPortfolio,
                            goalName,
                            targetAmount,
                            targetDate);

                        if (goalResult.Item1)
                            ConsoleUI.PrintSuccess(goalResult.Item2);
                        else
                        {
                            ConsoleUI.PrintError(goalResult.Item2);
                            ConsoleUI.Pause();
                        }
                        break;

                    case "VG":
                        // Retrieve the active goal
                        TradingGoal activeGoal = await goalService
                            .GetActiveGoalAsync(freshPortfolio.Id);

                        // Current portfolio valuation
                        decimal currentVal = freshPortfolio.GetTotalValue();

                        // Check if current goal is complete
                        if (activeGoal != null)
                        {
                            var completionResult = await goalService
                                .CheckGoalCompletionAsync(freshPortfolio);

                            if (completionResult.Item1)
                                ConsoleUI.PrintSuccess(completionResult.Item2);
                        }

                        // Display progress visualization
                        ConsoleUI.PrintGoalProgress(activeGoal, currentVal);
                        break;

                    case "GH":
                        // Retrieve and display goal history
                        List<TradingGoal> allGoals = await goalService
                            .GetAllGoalsAsync(freshPortfolio.Id);
                        ConsoleUI.PrintGoalHistory(allGoals);
                        break;
                    case "X":
                        simulator.Stop();
                        ConsoleUI.PrintInfo(
                            "Logged out successfully. Goodbye, " +
                            currentUser.Username + "!");
                        running = false;
                        break;

                    default:
                        ConsoleUI.PrintError(
                            "Invalid option. Please try again.");
                        break;
                }
            }
        }
    }
}














