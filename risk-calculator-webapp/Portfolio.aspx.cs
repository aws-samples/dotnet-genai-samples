using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using risk_calculator_webapp.Controls;

namespace risk_calculator_webapp
{
    public partial class Portfolio : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initialize ViewState for portfolio insights
                ViewState["PortfolioInsights"] = new Dictionary<string, object>();
                ViewState["LastAnalysisTime"] = DateTime.Now;
                
                // Initialize SessionState for portfolio tracking
                if (Session["PortfolioViewCount"] == null)
                {
                    Session["PortfolioViewCount"] = 0;
                }
                
                UpdatePortfolioInsights();
            }
            else
            {
                UpdatePortfolioInsights();
            }
        }

        protected void PortfolioGridControl_PortfolioDataLoaded(object sender, PortfolioDataEventArgs e)
        {
            // Handle portfolio data loaded event from the control
            var portfolioData = e.Holdings;
            
            // Update session statistics
            int viewCount = (int)Session["PortfolioViewCount"];
            Session["PortfolioViewCount"] = viewCount + 1;
            Session["LastPortfolioView"] = DateTime.Now;
            Session["LastPortfolioData"] = portfolioData;
            
            // Store insights in ViewState
            var insights = new Dictionary<string, object>
            {
                ["TotalValue"] = e.TotalValue,
                ["TotalVaR"] = e.TotalVaR,
                ["RiskRatio"] = e.RiskRatio,
                ["HoldingsCount"] = portfolioData.Count,
                ["LastUpdated"] = e.LastUpdated
            };
            
            // Find highest and lowest risk holdings
            var riskCalculator = new RiskCalculator();
            var riskAnalysis = new List<(string Symbol, decimal RiskRatio)>();
            
            foreach (var holding in portfolioData)
            {
                decimal var = riskCalculator.CalculateVaR(holding.Key, holding.Value);
                decimal riskRatio = (var / holding.Value) * 100;
                riskAnalysis.Add((holding.Key, riskRatio));
            }
            
            if (riskAnalysis.Any())
            {
                var highestRisk = riskAnalysis.OrderByDescending(x => x.RiskRatio).First();
                var lowestRisk = riskAnalysis.OrderBy(x => x.RiskRatio).First();
                
                insights["HighestRiskHolding"] = $"{highestRisk.Symbol} ({highestRisk.RiskRatio:F2}%)";
                insights["LowestRiskHolding"] = $"{lowestRisk.Symbol} ({lowestRisk.RiskRatio:F2}%)";
            }
            
            ViewState["PortfolioInsights"] = insights;
            ViewState["LastAnalysisTime"] = DateTime.Now;
            
            // Update display
            UpdatePortfolioInsights();
        }

        private void UpdatePortfolioInsights()
        {
            var insights = ViewState["PortfolioInsights"] as Dictionary<string, object>;
            
            if (insights != null && insights.Count > 0)
            {
                lblLastUpdated.Text = insights.ContainsKey("LastUpdated") ? 
                    ((DateTime)insights["LastUpdated"]).ToString("yyyy-MM-dd HH:mm:ss") : 
                    "Never";
                
                lblHoldingsCount.Text = insights.ContainsKey("HoldingsCount") ? 
                    insights["HoldingsCount"].ToString() : "0";
                
                lblHighestRisk.Text = insights.ContainsKey("HighestRiskHolding") ? 
                    insights["HighestRiskHolding"].ToString() : "N/A";
                
                lblLowestRisk.Text = insights.ContainsKey("LowestRiskHolding") ? 
                    insights["LowestRiskHolding"].ToString() : "N/A";
            }
            else
            {
                lblLastUpdated.Text = "No data available";
                lblHoldingsCount.Text = "0";
                lblHighestRisk.Text = "N/A";
                lblLowestRisk.Text = "N/A";
            }
        }
    }
}
