using System;
using System.Collections.Generic;
using System.Web;

namespace risk_calculator_webapp
{
    public class RiskAlertService
    {
        private const string ALERTS_SESSION_KEY = "RiskAlerts";
        
        public void AddAlert(string alertMessage)
        {
            var alerts = GetAlerts();
            alerts.Add(alertMessage);
            SetAlerts(alerts);
        }
        
        public List<string> GetAlerts()
        {
            var alerts = HttpContext.Current.Session[ALERTS_SESSION_KEY] as List<string>;
            if (alerts == null)
            {
                alerts = new List<string>();
                SetAlerts(alerts);
            }
            return alerts;
        }
        
        public void ClearAlerts()
        {
            HttpContext.Current.Session[ALERTS_SESSION_KEY] = new List<string>();
        }
        
        private void SetAlerts(List<string> alerts)
        {
            HttpContext.Current.Session[ALERTS_SESSION_KEY] = alerts;
        }
    }
}
