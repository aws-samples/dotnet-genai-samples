using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.UI;

namespace risk_calculator_webapp.Controls
{
    public partial class MonteCarloControl : System.Web.UI.UserControl
    {
        // Events for parent pages to handle
        public event EventHandler<MonteCarloEventArgs> SimulationCompleted;

        // Alternative event signature for framework compatibility
        public event EventHandler SimulationCompletedGeneric;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {

                // Initialize ViewState
                ViewState["SimulationHistory"] = new List<MonteCarloSimulationResult>();
                ViewState["LastSimulationCount"] = 0;

                // Set default values from SessionState if available
                if (Session["DefaultSimulationCount"] != null)
                {
                    txtSimulations.Text = Session["DefaultSimulationCount"].ToString();
                }
                else
                {
                    txtSimulations.Text = "1000";
                }

                LoadSimulationHistory();
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Store current simulation count in ViewState
            if (!string.IsNullOrEmpty(txtSimulations.Text))
            {
                if (int.TryParse(txtSimulations.Text, out int count))
                {
                    ViewState["LastSimulationCount"] = count;
                    hfLastSimulationCount.Value = count.ToString();
                }
            }
        }

        protected void btnRunSimulation_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    int simulations = int.Parse(txtSimulations.Text);

                    // Store in SessionState for cross-page persistence
                    Session["LastSimulationCount"] = simulations;

                    // Run simulation with timing
                    var stopwatch = Stopwatch.StartNew();
                    var monteCarloService = new MonteCarloService();
                    var results = monteCarloService.RunSimulation(simulations);
                    stopwatch.Stop();

                    // Create simulation result object
                    var simulationResult = new MonteCarloSimulationResult
                    {
                        SimulationCount = simulations,
                        VaR95 = results.VaR95,
                        VaR99 = results.VaR99,
                        ExpectedReturn = results.ExpectedReturn,
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        Timestamp = DateTime.Now
                    };

                    // Store in ViewState history
                    var history = ViewState["SimulationHistory"] as List<MonteCarloSimulationResult>;
                    if (history == null)
                    {
                        history = new List<MonteCarloSimulationResult>();
                    }
                    history.Insert(0, simulationResult); // Add to beginning

                    // Keep only last 10 simulations in history
                    if (history.Count > 10)
                    {
                        history = history.Take(10).ToList();
                    }

                    ViewState["SimulationHistory"] = history;

                    // Store in SessionState for cross-page access
                    Session["LastSimulationResults"] = results;
                    Session["LastSimulationTime"] = DateTime.Now;

                    // Display results
                    DisplayResults(simulationResult);

                    // Store in hidden field
                    hfSimulationHistory.Value = SerializeHistory(history);

                    // Raise event for parent page
                    SimulationCompleted?.Invoke(this, new MonteCarloEventArgs(simulationResult));
                }
                catch (Exception ex)
                {
                    ShowError($"An error occurred during simulation: {ex.Message}");
                }
            }
        }

        protected void btnClearHistory_Click(object sender, EventArgs e)
        {
            ViewState["SimulationHistory"] = new List<MonteCarloSimulationResult>();
            LoadSimulationHistory();
        }

        private void DisplayResults(MonteCarloSimulationResult result)
        {
            lblSimulationCount.Text = result.SimulationCount.ToString();
            lblSimulationsRun.Text = result.SimulationCount.ToString();
            lblVaR95.Text = string.Format("{0:F4}%", result.VaR95 * 100);
            lblVaR99.Text = string.Format("{0:F4}%", result.VaR99 * 100);
            lblExpectedReturn.Text = string.Format("{0:F4}%", result.ExpectedReturn * 100);
            lblSimulationTime.Text = $"{result.ExecutionTimeMs} ms";

            pnlResults.Visible = true;
            lblError.Visible = false;

            // Refresh history display
            LoadSimulationHistory();
        }

        private void LoadSimulationHistory()
        {
            var history = ViewState["SimulationHistory"] as List<MonteCarloSimulationResult>;
            tblHistoryBody.Controls.Clear();
            if (history == null || history.Count == 0) return;

            foreach (var result in history)
            {
                var row = new System.Web.UI.HtmlControls.HtmlGenericControl("tr");
                row.InnerHtml = string.Format(
                    "<td>{0}</td><td>{1}</td><td>{2:F4}%</td><td>{3:F4}%</td><td>{4:F4}%</td><td>{5}</td>",
                    result.Timestamp.ToString("HH:mm:ss"),
                    result.SimulationCount,
                    result.VaR95 * 100,
                    result.VaR99 * 100,
                    result.ExpectedReturn * 100,
                    result.ExecutionTimeMs);
                tblHistoryBody.Controls.Add(row);
            }
        }

        private string SerializeHistory(List<MonteCarloSimulationResult> history)
        {
            // Simple serialization for hidden field storage
            var items = new List<string>();
            foreach (var result in history)
            {
                items.Add($"{result.Timestamp:yyyy-MM-dd HH:mm:ss}|{result.SimulationCount}|{result.VaR95}|{result.VaR99}|{result.ExpectedReturn}|{result.ExecutionTimeMs}");
            }
            return string.Join("||", items);
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
            pnlResults.Visible = false;
        }

        // Public properties for parent pages to access
        public int LastSimulationCount
        {
            get
            {
                if (ViewState["LastSimulationCount"] != null)
                    return (int)ViewState["LastSimulationCount"];
                return 0;
            }
        }

        public List<MonteCarloSimulationResult> SimulationHistory
        {
            get { return ViewState["SimulationHistory"] as List<MonteCarloSimulationResult> ?? new List<MonteCarloSimulationResult>(); }
        }

        public bool HasResults
        {
            get { return pnlResults.Visible; }
        }
    }    

}