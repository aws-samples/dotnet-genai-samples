using System;

namespace risk_calculator_webapp
{
    // Simplified event arguments - kept for backward compatibility but not actively used

    // Result class for calculations
    [Serializable]
    public class VaRCalculationResult
    {
        public string Symbol { get; set; }
        public decimal Amount { get; set; }
        public decimal VaR { get; set; }
        public decimal CreditRisk { get; set; }
        public string RiskLevel { get; set; }
        public DateTime CalculatedAt { get; set; }

        public VaRCalculationResult()
        {
            CalculatedAt = DateTime.Now;
        }
    }
}