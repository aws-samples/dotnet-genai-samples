using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using risk_calculator_webapp.Controls;

namespace risk_calculator_webapp
{
    public partial class MonteCarlo : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initialize ViewState for simulation statistics
                ViewState["SimulationStatistics"] = new Dictionary<string, object>();
                ViewState["SimulationTimes"] = new List<long>();
                
                // Initialize SessionState for simulation tracking
                if (Session["TotalSimulations"] == null)
                {
                    Session["TotalSimulations"] = 0;
                }
                if (Session["SimulationHistory"] == null)
                {
                    Session["SimulationHistory"] = new List<MonteCarloSimulationResult>();
                }
                
                UpdateStatistics();
            }
            else
            {
                UpdateStatistics();
            }
        }

        protected void MonteCarloControl_SimulationCompleted(object sender, MonteCarloEventArgs e)
        {
            // Handle simulation completed event from the control
            var result = e.Result;
            
            // Update session statistics
            int totalSimulations = (int)Session["TotalSimulations"];
            Session["TotalSimulations"] = totalSimulations + 1;
            Session["LastSimulationTime"] = DateTime.Now;
            Session["LastSimulationResult"] = result;
            
            // Update simulation history in SessionState
            var history = Session["SimulationHistory"] as List<MonteCarloSimulationResult>;
            if (history == null)
            {
                history = new List<MonteCarloSimulationResult>();
            }
            history.Insert(0, result);
            
            // Keep only last 20 simulations in session history
            if (history.Count > 20)
            {
                history = history.Take(20).ToList();
            }
            Session["SimulationHistory"] = history;
            
            // Update ViewState with simulation times
            var simulationTimes = ViewState["SimulationTimes"] as List<long>;
            if (simulationTimes == null)
            {
                simulationTimes = new List<long>();
            }
            simulationTimes.Add(result.ExecutionTimeMs);
            ViewState["SimulationTimes"] = simulationTimes;
            
            // Update statistics display
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            // Update total simulations
            int totalSimulations = Session["TotalSimulations"] != null ? (int)Session["TotalSimulations"] : 0;
            lblTotalSimulations.Text = totalSimulations.ToString();
            
            // Update last simulation time
            if (Session["LastSimulationTime"] != null)
            {
                DateTime lastSimTime = (DateTime)Session["LastSimulationTime"];
                lblLastSimulationTime.Text = lastSimTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                lblLastSimulationTime.Text = "No simulations yet";
            }
            
            // Calculate and display simulation time statistics
            var simulationTimes = ViewState["SimulationTimes"] as List<long>;
            if (simulationTimes != null && simulationTimes.Count > 0)
            {
                double averageTime = simulationTimes.Average();
                long fastestTime = simulationTimes.Min();
                
                lblAverageSimulationTime.Text = $"{averageTime:F0} ms";
                lblFastestSimulation.Text = $"{fastestTime} ms";
            }
            else
            {
                lblAverageSimulationTime.Text = "N/A";
                lblFastestSimulation.Text = "N/A";
            }
        }
    }
}
