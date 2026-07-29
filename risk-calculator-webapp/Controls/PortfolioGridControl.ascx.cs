using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace risk_calculator_webapp.Controls
{
    public partial class PortfolioGridControl : System.Web.UI.UserControl
    {
        public event EventHandler<PortfolioDataEventArgs> PortfolioDataLoaded;
        public event EventHandler PortfolioDataLoadedGeneric;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                ViewState["PortfolioData"] = new Dictionary<string, decimal>();
                ViewState["LastCalculationTime"] = DateTime.Now;
                LoadPortfolioData();
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            if (ViewState["PortfolioData"] != null)
            {
                var portfolioData = ViewState["PortfolioData"] as Dictionary<string, decimal>;
                hfPortfolioData.Value = SerializePortfolioData(portfolioData);
            }
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadPortfolioData();
        }

        private void LoadPortfolioData()
        {
            try
            {
                var riskCalculator = new RiskCalculator();
                var portfolioData = riskCalculator.GetPortfolioData();

                ViewState["PortfolioData"] = portfolioData;
                ViewState["LastCalculationTime"] = DateTime.Now;
                Session["PortfolioData"] = portfolioData;
                Session["PortfolioLastUpdated"] = DateTime.Now;

                decimal totalValue = 0;
                decimal totalVaR = 0;

                tblPortfolioBody.Controls.Clear();
                foreach (var item in portfolioData)
                {
                    decimal varValue = riskCalculator.CalculateVaR(item.Key, item.Value);
                    decimal riskPct = (varValue / item.Value) * 100;
                    string riskLevel = riskCalculator.GetRiskLevel(varValue / item.Value);

                    var row = new HtmlGenericControl("tr");
                    row.InnerHtml = string.Format(
                        "<td>{0}</td><td>{1:N2}</td><td>{2:N2}</td><td>{3:F2}%</td><td><span class=\"risk-level {4}\">{4}</span></td>",
                        HttpUtility.HtmlEncode(item.Key),
                        item.Value, varValue, riskPct,
                        HttpUtility.HtmlEncode(riskLevel));
                    tblPortfolioBody.Controls.Add(row);

                    totalValue += item.Value;
                    totalVaR += varValue;
                }

                decimal totalRiskPct = totalValue > 0 ? (totalVaR / totalValue) * 100 : 0;
                lblTotalValue.Text = string.Format("${0:N2}", totalValue);
                lblTotalVaR.Text = string.Format("${0:N2}", totalVaR);
                lblRiskRatio.Text = string.Format("{0:F2}%", totalRiskPct);
                lblLastUpdated.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                hfLastRefreshTime.Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                PortfolioDataLoaded?.Invoke(this, new PortfolioDataEventArgs
                {
                    TotalValue = totalValue,
                    TotalVaR = totalVaR,
                    RiskRatio = totalRiskPct,
                    Holdings = portfolioData,
                    LastUpdated = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading portfolio data: {ex.Message}");
            }
        }

        private string SerializePortfolioData(Dictionary<string, decimal> portfolioData)
        {
            var items = new List<string>();
            foreach (var item in portfolioData)
                items.Add($"{item.Key}:{item.Value}");
            return string.Join("|", items);
        }

        public Dictionary<string, decimal> PortfolioData
        {
            get { return ViewState["PortfolioData"] as Dictionary<string, decimal> ?? new Dictionary<string, decimal>(); }
        }

        public DateTime LastCalculationTime
        {
            get
            {
                if (ViewState["LastCalculationTime"] != null)
                    return (DateTime)ViewState["LastCalculationTime"];
                return DateTime.MinValue;
            }
        }

        public decimal TotalValue
        {
            get { return PortfolioData.Values.Sum(); }
        }

        public decimal TotalVaR
        {
            get
            {
                var riskCalculator = new RiskCalculator();
                return PortfolioData.Sum(item => riskCalculator.CalculateVaR(item.Key, item.Value));
            }
        }
    }
}
