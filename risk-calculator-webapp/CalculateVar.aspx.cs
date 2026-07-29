using System;
using System.Web.UI;
using risk_calculator_webapp.Controls;

namespace risk_calculator_webapp
{
    public partial class CalculateVar : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initialize SessionState tracking
                Session["SessionStartTime"] = DateTime.Now;
                Session["TotalCalculations"] = 0;
                
                UpdateStatistics();
            }
            else
            {
                UpdateStatistics();
            }
        }

        protected void VarCalcControl_VaRCalculated(object sender, EventArgs e)
        {
            // Handle VaR calculation event from the control
            var control = sender as VarCalculationControl;
            var result = control?.LastCalculationResult;

            if (result != null)
            {
                // Update session statistics
                int totalCalculations = (int)Session["TotalCalculations"];
                Session["TotalCalculations"] = totalCalculations + 1;
                Session["LastCalculationTime"] = DateTime.Now;
                Session["LastCalculationResult"] = result;

                // Update statistics display
                UpdateStatistics();
            }
        }

        protected void VarCalcControl_CalculationReset(object sender, EventArgs e)
        {
            // Handle calculation reset event
            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            // Update total calculations
            int totalCalculations = Session["TotalCalculations"] != null ? (int)Session["TotalCalculations"] : 0;
            lblTotalCalculations.Text = totalCalculations.ToString();
            
            // Update last calculation time
            if (Session["LastCalculationTime"] != null)
            {
                DateTime lastCalcTime = (DateTime)Session["LastCalculationTime"];
                lblLastCalculationTime.Text = lastCalcTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            else
            {
                lblLastCalculationTime.Text = "No calculations yet";
            }
            
            // Update session duration
            if (Session["SessionStartTime"] != null)
            {
                DateTime sessionStart = (DateTime)Session["SessionStartTime"];
                TimeSpan duration = DateTime.Now - sessionStart;
                lblSessionDuration.Text = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            }
        }
    }
}
