<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Portfolio.aspx.cs" Inherits="risk_calculator_webapp.Portfolio" %>
<%@ Register Src="~/Controls/NavigationControl.ascx" TagName="NavigationControl" TagPrefix="rc" %>
<%@ Register Src="~/Controls/PortfolioGridControl.ascx" TagName="PortfolioGridControl" TagPrefix="rc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Portfolio View - Risk Calculator</title>
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
                    <h1 style="margin: 0; font-size: 16px; font-weight: bold;">Portfolio Risk Analysis</h1>
                    <p style="margin: 5px 0 0 0; font-size: 11px;">See total portfolio risk, identify your riskiest holdings, and understand diversification</p>
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
                            <a href="Portfolio.aspx" class="sidebar-nav-link active">
                                Portfolio Risk Analysis
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="MonteCarlo.aspx" class="sidebar-nav-link">
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
                    <!-- Main Portfolio Grid - Full Width -->
                    <div class="card">
                        <div class="card-header">
                            <h2 class="card-title">Portfolio Holdings</h2>
                            <p class="card-subtitle">Current portfolio positions and risk analysis</p>
                        </div>
                        <div class="card-body">
                            <rc:PortfolioGridControl ID="portfolioGridControl" runat="server"
                                OnPortfolioDataLoaded="PortfolioGridControl_PortfolioDataLoaded" />
                        </div>
                    </div>

                    <!-- Replace CSS Grid with Table Layout -->
                    <table class="dashboard-table" cellpadding="0" cellspacing="0">
                        <tr>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">Portfolio Insights</h3>
                                        <p class="card-subtitle">Key metrics and statistics</p>
                                    </div>
                                    <div class="card-body">
                                        <div class="stat-card">
                                            <div class="stat-value"><asp:Label ID="lblHoldingsCount" runat="server" Text="0"></asp:Label></div>
                                            <div class="stat-label">Holdings</div>
                                        </div>

                                        <table class="insights-table" style="margin-top: 15px;">
                                            <tr>
                                                <td>Portfolio Last Updated:</td>
                                                <td><asp:Label ID="lblLastUpdated" runat="server"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>Highest Risk Holding:</td>
                                                <td><asp:Label ID="lblHighestRisk" runat="server"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>Lowest Risk Holding:</td>
                                                <td><asp:Label ID="lblLowestRisk" runat="server"></asp:Label></td>
                                            </tr>
                                        </table>
                                    </div>
                                </div>
                            </td>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">Quick Actions</h3>
                                    </div>
                                    <div class="card-body">
                                        <div class="btn-group clearfix">
                                            <asp:HyperLink ID="lnkCalculateVar" runat="server" NavigateUrl="~/CalculateVar.aspx" CssClass="btn btn-primary">
                                                Calculate VaR for Holdings
                                            </asp:HyperLink>
                                            <asp:HyperLink ID="lnkMonteCarlo" runat="server" NavigateUrl="~/MonteCarlo.aspx" CssClass="btn btn-secondary">
                                                Run Monte Carlo Simulation
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
                    <p>Portfolio Analysis Dashboard - Financial Risk Management System v2.1</p>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
