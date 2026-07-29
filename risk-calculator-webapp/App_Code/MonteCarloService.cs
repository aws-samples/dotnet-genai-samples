using System;
using System.Collections.Generic;
using System.Linq;

namespace risk_calculator_webapp
{
    public class MonteCarloService
    {
        private readonly RiskCalculator _riskCalculator;
        private readonly Random _random;
        
        public MonteCarloService()
        {
            _riskCalculator = new RiskCalculator();
            _random = new Random();
        }
        
        public MonteCarloResult RunSimulation(int numberOfSimulations)
        {
            var portfolioData = _riskCalculator.GetPortfolioData();
            var results = new List<decimal>();
            decimal totalPortfolioValue = portfolioData.Values.Sum();
            
            for (int i = 0; i < numberOfSimulations; i++)
            {
                decimal portfolioReturn = 0.0m;
                
                foreach (var item in portfolioData)
                {
                    decimal volatility = _riskCalculator.GetHistoricalVolatility(item.Key);
                    // Simple random walk using Box-Muller transform for normal distribution
                    decimal dailyReturn = GenerateNormalRandom(0.001m, volatility);
                    portfolioReturn += (item.Value / totalPortfolioValue) * dailyReturn;
                }
                
                results.Add(portfolioReturn);
            }
            
            // Calculate percentiles
            var sortedResults = results.OrderBy(x => x).ToList();
            int index95 = (int)(0.05 * results.Count);
            int index99 = (int)(0.01 * results.Count);
            
            return new MonteCarloResult
            {
                VaR95 = sortedResults[index95],
                VaR99 = sortedResults[index99],
                ExpectedReturn = results.Average()
            };
        }
        
        private decimal GenerateNormalRandom(decimal mean, decimal stdDev)
        {
            // Box-Muller transform for generating normal random numbers
            double u1 = _random.NextDouble();
            double u2 = _random.NextDouble();
            
            double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            
            return mean + (decimal)(z0 * (double)stdDev);
        }
    }

    [Serializable]
    public class MonteCarloResult
    {
        public decimal VaR95 { get; set; }
        public decimal VaR99 { get; set; }
        public decimal ExpectedReturn { get; set; }
    }
}
