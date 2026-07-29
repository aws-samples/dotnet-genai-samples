using System;
using System.Collections.Generic;

namespace risk_calculator_webapp
{
    // Event arguments class   
    public class PortfolioDataEventArgs : EventArgs
    {
        public decimal TotalValue { get; set; }
        public decimal TotalVaR { get; set; }
        public decimal RiskRatio { get; set; }

        public Dictionary<string, decimal> Holdings { get; set; }
        public DateTime LastUpdated { get; set; }

        public PortfolioDataEventArgs()
        {
            LastUpdated = DateTime.Now;
            Holdings = new Dictionary<string, decimal>();
        }
        public PortfolioDataEventArgs(decimal totalValue, decimal totalVaR, decimal riskRatio, Dictionary<string, decimal> holdings)
        {
            TotalValue = totalValue;
            TotalVaR = totalVaR;
            RiskRatio = riskRatio;
            Holdings = holdings;
            LastUpdated = DateTime.Now;
        }
    }
}