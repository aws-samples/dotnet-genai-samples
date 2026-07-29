using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace risk_calculator_webapp.Controls
{
    public partial class VarCalculationControl : System.Web.UI.UserControl
    {
        // Events for parent pages to handle - using standard EventHandler signatures
        public event EventHandler<EventArgs> VaRCalculated;
        public event EventHandler<EventArgs> CalculationReset;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initialize ViewState values
                ViewState["LastSymbol"] = string.Empty;
                ViewState["LastAmount"] = 0m;
                ViewState["CalculationHistory"] = new System.Collections.Generic.List<VaRCalculationResult>();

                // Set default values from SessionState if available
                if (Session["DefaultSymbol"] != null)
                {
                    txtSymbol.Text = Session["DefaultSymbol"].ToString();
                }
                if (Session["DefaultAmount"] != null)
                {
                    txtAmount.Text = Session["DefaultAmount"].ToString();
                }
            }
        }

        protected void Page_PreRender(object sender, EventArgs e)
        {
            // Store current values in ViewState for persistence
            if (!string.IsNullOrEmpty(txtSymbol.Text))
            {
                ViewState["LastSymbol"] = txtSymbol.Text;
            }
            if (!string.IsNullOrEmpty(txtAmount.Text))
            {
                if (decimal.TryParse(txtAmount.Text, out decimal amount))
                {
                    ViewState["LastAmount"] = amount;
                }
            }
        }

        protected void btnCalculate_Click(object sender, EventArgs e)
        {
            if (Page.IsValid)
            {
                try
                {
                    string symbol = txtSymbol.Text.Trim().ToUpper();
                    decimal amount = decimal.Parse(txtAmount.Text);

                    // Store in SessionState for cross-page persistence
                    Session["LastCalculatedSymbol"] = symbol;
                    Session["LastCalculatedAmount"] = amount;

                    // Calculate VaR using business logic
                    var riskCalculator = new RiskCalculator();
                    decimal var = riskCalculator.CalculateVaR(symbol, amount);
                    decimal creditRisk = riskCalculator.CalculateCreditRisk(symbol);
                    string riskLevel = riskCalculator.GetRiskLevel(var / amount);

                    // Create result object
                    var result = new VaRCalculationResult
                    {
                        Symbol = symbol,
                        Amount = amount,
                        VaR = var,
                        CreditRisk = creditRisk,
                        RiskLevel = riskLevel,
                        CalculatedAt = DateTime.Now
                    };

                    // Store in ViewState for this control's history
                    var history = ViewState["CalculationHistory"] as System.Collections.Generic.List<VaRCalculationResult>;
                    if (history == null)
                    {
                        history = new System.Collections.Generic.List<VaRCalculationResult>();
                    }
                    history.Add(result);
                    ViewState["CalculationHistory"] = history;

                    // Update calculation count in ViewState
                    int count = int.Parse(hfCalculationCount.Value);
                    count++;
                    hfCalculationCount.Value = count.ToString();

                    // Display results
                    DisplayResults(result);

                    // Store last calculation in hidden field
                    hfLastCalculation.Value = $"{symbol}|{amount}|{var}|{creditRisk}|{riskLevel}";

                    // Store result in ViewState for parent access
                    ViewState["LastCalculationResult"] = result;

                    // Raise event for parent page
                    VaRCalculated?.Invoke(this, EventArgs.Empty);

                    // Check for high risk and add alert
                    if (var / amount > 0.05m)
                    {
                        var alertService = new RiskAlertService();
                        alertService.AddAlert($"HIGH RISK: {symbol} VaR exceeds 5% threshold at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    }
                }
                catch (Exception ex)
                {
                    ShowError($"An error occurred during calculation: {ex.Message}");
                }
            }
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ResetForm();
            CalculationReset?.Invoke(this, EventArgs.Empty);
        }

        protected void btnNewCalculation_Click(object sender, EventArgs e)
        {
            ResetForm();
        }

        private void DisplayResults(VaRCalculationResult result)
        {
            lblSymbol.Text = result.Symbol;
            lblAmount.Text = string.Format("${0:N2}", result.Amount);
            lblVaR.Text = string.Format("${0:N2}", result.VaR);
            lblCreditRisk.Text = string.Format("{0:F1}/10", result.CreditRisk);
            lblRiskLevel.Text = result.RiskLevel;
            lblRiskLevel.CssClass = "risk-level " + result.RiskLevel.ToLower();

            pnlForm.Visible = false;
            pnlResults.Visible = true;
            lblError.Visible = false;
        }

        private void ResetForm()
        {
            pnlForm.Visible = true;
            pnlResults.Visible = false;
            lblError.Visible = false;

            // Restore from SessionState if available
            if (Session["DefaultSymbol"] != null)
            {
                txtSymbol.Text = Session["DefaultSymbol"].ToString();
            }
            else
            {
                txtSymbol.Text = "AAPL";
            }

            if (Session["DefaultAmount"] != null)
            {
                txtAmount.Text = Session["DefaultAmount"].ToString();
            }
            else
            {
                txtAmount.Text = "100000";
            }
        }

        private void ShowError(string message)
        {
            lblError.Text = message;
            lblError.Visible = true;
            pnlForm.Visible = true;
            pnlResults.Visible = false;
        }

        // Public properties for parent pages to access
        public string Symbol
        {
            get { return txtSymbol.Text; }
            set { txtSymbol.Text = value; }
        }

        public decimal Amount
        {
            get
            {
                return decimal.TryParse(txtAmount.Text, out decimal amount) ? amount : 0;
            }
            set { txtAmount.Text = value.ToString("F2"); }
        }

        public int CalculationCount
        {
            get { return int.Parse(hfCalculationCount.Value); }
        }

        public System.Collections.Generic.List<VaRCalculationResult> CalculationHistory
        {
            get { return ViewState["CalculationHistory"] as System.Collections.Generic.List<VaRCalculationResult> ?? new System.Collections.Generic.List<VaRCalculationResult>(); }
        }

        public VaRCalculationResult LastCalculationResult
        {
            get { return ViewState["LastCalculationResult"] as VaRCalculationResult; }
        }
    }

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