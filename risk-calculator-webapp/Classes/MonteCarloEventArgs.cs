using System;

namespace risk_calculator_webapp
{
    public class MonteCarloEventArgs : EventArgs
    {
        public MonteCarloSimulationResult Result { get; set; }

        public MonteCarloEventArgs()
        {
        }

        public MonteCarloEventArgs(MonteCarloSimulationResult result)
        {
            Result = result;
        }
    }

    // Result class for simulations
    [Serializable]
    public class MonteCarloSimulationResult
    {
        public int SimulationCount { get; set; }
        public decimal VaR95 { get; set; }
        public decimal VaR99 { get; set; }
        public decimal ExpectedReturn { get; set; }
        public long ExecutionTimeMs { get; set; }
        public DateTime Timestamp { get; set; }
    }
}