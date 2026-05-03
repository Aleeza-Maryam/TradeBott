using System;

namespace TradeBot.Services
{
    public class RiskCalculationResult
    {
        public bool IsValid { get; set; } = true;
        public string ErrorMessage { get; set; }

        public decimal Balance { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal StopLoss { get; set; }
        public decimal RiskPercent { get; set; }
        public decimal RiskAmount { get; set; }
        public decimal PositionSize { get; set; }
        public decimal TakeProfit_1to2 { get; set; }
        public decimal TakeProfit_1to3 { get; set; }
        public string Side { get; set; }
    }

    public class RiskManagementService
    {
        /// <summary>
        /// Calculates recommended position size and take profit levels given balance, entry and stop loss.
        /// </summary>
        public RiskCalculationResult CalculateTradeRisk(decimal balance, decimal entryPrice, decimal stopLoss, decimal riskPercent = 2m, string side = "Long")
        {
            var result = new RiskCalculationResult
            {
                Balance = balance,
                EntryPrice = entryPrice,
                StopLoss = stopLoss,
                RiskPercent = riskPercent,
                Side = side
            };

            if (balance <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Balance must be greater than zero.";
                return result;
            }

            if (entryPrice <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Entry price must be greater than zero.";
                return result;
            }

            if (stopLoss <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Stop Loss must be greater than zero.";
                return result;
            }

            bool isLong = string.Equals(side, "Long", StringComparison.OrdinalIgnoreCase);
            bool isShort = string.Equals(side, "Short", StringComparison.OrdinalIgnoreCase);

            if (!isLong && !isShort)
            {
                // default to Long semantics if unexpected
                isLong = true;
            }

            // Safety checks
            if (isLong && stopLoss >= entryPrice)
            {
                result.IsValid = false;
                result.ErrorMessage = "For a Long trade the Stop Loss must be lower than the Entry Price.";
                return result;
            }

            if (isShort && stopLoss <= entryPrice)
            {
                result.IsValid = false;
                result.ErrorMessage = "For a Short trade the Stop Loss must be higher than the Entry Price.";
                return result;
            }

            decimal perUnitRisk = Math.Abs(entryPrice - stopLoss);
            if (perUnitRisk == 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Per-unit risk is zero. Check entry and stop loss values to avoid division by zero.";
                return result;
            }

            decimal riskAmount = balance * (riskPercent / 100m);

            decimal positionSize = riskAmount / perUnitRisk;

            // Direction multiplier: long -> positive TP, short -> negative (below) TP
            int direction = isLong ? 1 : -1;

            decimal tp_1to2 = entryPrice + (perUnitRisk * 2m * direction);
            decimal tp_1to3 = entryPrice + (perUnitRisk * 3m * direction);

            result.RiskAmount = Math.Round(riskAmount, 8);
            result.PositionSize = Math.Round(positionSize, 8);
            result.TakeProfit_1to2 = Math.Round(tp_1to2, 8);
            result.TakeProfit_1to3 = Math.Round(tp_1to3, 8);

            return result;
        }
    }
}
