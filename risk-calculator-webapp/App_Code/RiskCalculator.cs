using System;
using System.Collections.Generic;

namespace risk_calculator_webapp
{
    public class RiskCalculator
    {
        private readonly Dictionary<string, decimal> _portfolioData;
        private readonly Dictionary<string, decimal> _volatilities;
        
        public RiskCalculator()
        {
            // Portfolio data - converted from Python dictionary
            _portfolioData = new Dictionary<string, decimal>
            {
                { "AAPL", 150000.00m },
                { "GOOGL", 200000.00m },
                { "MSFT", 175000.00m },
                { "AMZN", 180000.00m },
                { "TSLA", 120000.00m }
            };
            
            // Historical volatility data
            _volatilities = new Dictionary<string, decimal>
            {
                { "AAPL", 0.025m },
                { "GOOGL", 0.030m },
                { "MSFT", 0.022m },
                { "AMZN", 0.035m },
                { "TSLA", 0.055m }
            };
        }
        
        public decimal CalculateVaR(string symbol, decimal amount)
        {
            decimal volatility = GetHistoricalVolatility(symbol);
            decimal confidenceLevel = 1.645m; // 95% confidence Z-score
            return amount * volatility * confidenceLevel;
        }
        
        public decimal GetHistoricalVolatility(string symbol)
        {
            return _volatilities.ContainsKey(symbol) ? _volatilities[symbol] : 0.025m;
        }
        
        public decimal CalculateCreditRisk(string symbol)
        {
            // Simple credit risk scoring (1-10 scale) - converted from Python hash logic
            int symbolHash = symbol.GetHashCode();
            return 1 + (Math.Abs(symbolHash) % 90) / 10.0m;
        }
        
        public string GetRiskLevel(decimal riskRatio)
        {
            if (riskRatio < 0.02m)
                return "LOW";
            else if (riskRatio < 0.05m)
                return "MEDIUM";
            else
                return "HIGH";
        }
        
        public Dictionary<string, decimal> GetPortfolioData()
        {
            return new Dictionary<string, decimal>(_portfolioData);
        }
    }
}
