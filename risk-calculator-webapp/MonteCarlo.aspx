<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MonteCarlo.aspx.cs" Inherits="risk_calculator_webapp.MonteCarlo" %>
<%@ Register Src="~/Controls/NavigationControl.ascx" TagName="NavigationControl" TagPrefix="rc" %>
<%@ Register Src="~/Controls/MonteCarloControl.ascx" TagName="MonteCarloControl" TagPrefix="rc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Monte Carlo Simulation - Risk Calculator</title>
    <link href="Content/Site.css" rel="stylesheet" type="text/css" />
    <!--[if IE 6]>
        <style type="text/css">
            .app-layout { width: 998px !important; }
            .png { behavior: url(iepngfix.htc); }
        </style>
    <![endif]-->
    <!--[if IE 7]>
        <style type="text/css">
            .app-layout { width: 998px !important; }
        </style>
    <![endif]-->
    <!--[if IE 8]>
        <style type="text/css">
            .app-layout { border-collapse: separate; }
        </style>
    <![endif]-->
</head>
<body>
    <form id="form1" runat="server">
        <!-- Classic Table-Based Layout for 2008-2012 Era -->
        <table class="app-layout clearfix" cellpadding="0" cellspacing="0">
            <colgroup>
                <col width="160">
                <col width="838">
            </colgroup>
            <tr>
                <td colspan="2" class="app-header">
                    <h1 style="margin: 0; font-size: 16px; font-weight: bold;">Investment Stress Testing</h1>
                    <p style="margin: 5px 0 0 0; font-size: 11px;">Run "what-if" scenarios to see how market crashes and volatility could affect your investments</p>
                </td>
            </tr>
            <tr>
                <td class="app-sidebar" width="160" valign="top">
                    <ul class="sidebar-nav">
                        <li class="sidebar-nav-item">
                            <a href="Default.aspx" class="sidebar-nav-link">
                                Dashboard
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="CalculateVar.aspx" class="sidebar-nav-link">
                                Investment Risk Assessment
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="Portfolio.aspx" class="sidebar-nav-link">
                                Portfolio Risk Analysis
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="MonteCarlo.aspx" class="sidebar-nav-link active">
                                Stress Testing
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="RiskAlerts.aspx" class="sidebar-nav-link">
                                Risk Monitoring
                            </a>
                        </li>
                    </ul>
                </td>
                <td class="app-main" width="838" valign="top">
                    <!-- Main Monte Carlo Control - Full Width -->
                    <div class="card">
                        <div class="card-header">
                            <h2 class="card-title">Monte Carlo Simulation Control</h2>
                            <p class="card-subtitle">Configure and run statistical risk simulations</p>
                        </div>
                        <div class="card-body">
                            <rc:MonteCarloControl ID="monteCarloControl" runat="server"
                                OnSimulationCompleted="MonteCarloControl_SimulationCompleted" />
                        </div>
                    </div>

                    <!-- Replace CSS Grid with Table Layout -->
                    <table class="dashboard-table" cellpadding="0" cellspacing="0">
                        <tr>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">Session Statistics</h3>
                                        <p class="card-subtitle">Performance metrics for this session</p>
                                    </div>
                                    <div class="card-body">
                                        <div class="stat-card">
                                            <div class="stat-value"><asp:Label ID="lblTotalSimulations" runat="server" Text="0"></asp:Label></div>
                                            <div class="stat-label">Total Simulations</div>
                                        </div>

                                        <table class="stats-table" style="margin-top: 15px;">
                                            <tr>
                                                <td>Last Simulation Time:</td>
                                                <td><asp:Label ID="lblLastSimulationTime" runat="server" Text="N/A"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>Average Simulation Time:</td>
                                                <td><asp:Label ID="lblAverageSimulationTime" runat="server" Text="N/A"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>Fastest Simulation:</td>
                                                <td><asp:Label ID="lblFastestSimulation" runat="server" Text="N/A"></asp:Label></td>
                                            </tr>
                                        </table>
                                    </div>
                                </div>
                            </td>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">How Stress Testing Works</h3>
                                    </div>
                                    <div class="card-body">
                                        <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                            <strong>Think of it as:</strong> Running thousands of "what if" scenarios to see how your investments might perform under different market conditions - from normal days to market crashes.
                                        </p>

                                        <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                            <strong>Example:</strong> Run 10,000 simulations of the next month. Results might show: "In 95% of scenarios, you won't lose more than $2,000, but in the worst 5%, you could lose up to $8,000."
                                        </p>

                                        <ul style="margin: 0; padding-left: 15px; line-height: 1.6;">
                                            <li style="margin-bottom: 6px;"><strong>Set Your Parameters:</strong> Input your investment details and risk assumptions</li>
                                            <li style="margin-bottom: 6px;"><strong>Choose Simulations:</strong> More simulations = more accurate results (10,000 is typical)</li>
                                            <li style="margin-bottom: 6px;"><strong>Configure Market Conditions:</strong> Set how volatile you expect markets to be</li>
                                            <li style="margin-bottom: 6px;"><strong>Analyze Results:</strong> See probability ranges and worst-case scenarios</li>
                                        </ul>

                                        <div class="btn-group clearfix" style="margin-top: 15px;">
                                            <asp:HyperLink ID="lnkPortfolio" runat="server" NavigateUrl="~/Portfolio.aspx" CssClass="btn btn-primary">
                                                View Portfolio Holdings
                                            </asp:HyperLink>
                                            <asp:HyperLink ID="lnkBack" runat="server" NavigateUrl="~/Default.aspx" CssClass="btn btn-secondary">
                                                ← Back to Dashboard
                                            </asp:HyperLink>
                                        </div>
                                    </div>
                                </div>
                            </td>
                        </tr>
                    </table>

                    <!-- Keep the legacy navigation control for compatibility -->
                    <div style="display: none;">
                        <rc:NavigationControl ID="navControl" runat="server" />
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="2" class="app-footer">
                    <p>Monte Carlo Simulation Dashboard - Financial Risk Management System v2.1</p>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
