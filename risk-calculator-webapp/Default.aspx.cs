using System;
using System.Web.UI;

namespace risk_calculator_webapp
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                lblCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                lblSessionId.Text = Session.SessionID;
                
                // Initialize SessionState with default values
                if (Session["DefaultSymbol"] == null)
                {
                    Session["DefaultSymbol"] = "AAPL";
                }
                if (Session["DefaultAmount"] == null)
                {
                    Session["DefaultAmount"] = "100000";
                }
                if (Session["DefaultSimulationCount"] == null)
                {
                    Session["DefaultSimulationCount"] = "1000";
                }
                
                // Store page visit information in ViewState
                ViewState["PageVisits"] = 1;
                ViewState["FirstVisitTime"] = DateTime.Now;
            }
            else
            {
                // Increment page visits in ViewState
                int visits = (int)ViewState["PageVisits"];
                ViewState["PageVisits"] = visits + 1;
                
                // Update current time
                lblCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}
