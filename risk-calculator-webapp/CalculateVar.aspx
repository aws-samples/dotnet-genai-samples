<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CalculateVar.aspx.cs" Inherits="risk_calculator_webapp.CalculateVar" %>
<%@ Register Src="~/Controls/NavigationControl.ascx" TagName="NavigationControl" TagPrefix="rc" %>
<%@ Register Src="~/Controls/VarCalculationControl.ascx" TagName="VarCalculationControl" TagPrefix="rc" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>VaR Calculation - Risk Calculator</title>
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
                    <h1 style="margin: 0; font-size: 16px; font-weight: bold;">Investment Risk Assessment</h1>
                    <p style="margin: 5px 0 0 0; font-size: 11px;">Calculate potential losses on individual stocks, bonds, and other investments</p>
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
                            <a href="CalculateVar.aspx" class="sidebar-nav-link active">
                                Investment Risk Assessment
                            </a>
                        </li>
                        <li class="sidebar-nav-item">
                            <a href="Portfolio.aspx" class="sidebar-nav-link">
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
                    <!-- Main Calculation Card - Full Width -->
                    <div class="card">
                        <div class="card-header">
                            <h2 class="card-title">VaR Calculation Control</h2>
                            <p class="card-subtitle">Calculate Value at Risk using historical or parametric methods</p>
                        </div>
                        <div class="card-body">
                            <rc:VarCalculationControl ID="varCalcControl" runat="server"
                                OnVaRCalculated="VarCalcControl_VaRCalculated"
                                OnCalculationReset="VarCalcControl_CalculationReset" />
                        </div>
                    </div>

                    <!-- Replace CSS Grid with Table Layout -->
                    <table class="dashboard-table" cellpadding="0" cellspacing="0">
                        <tr>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">Session Statistics</h3>
                                        <p class="card-subtitle">Calculation performance metrics</p>
                                    </div>
                                    <div class="card-body">
                                        <div class="stat-card">
                                            <div class="stat-value"><asp:Label ID="lblTotalCalculations" runat="server" Text="0"></asp:Label></div>
                                            <div class="stat-label">Calculations</div>
                                        </div>

                                        <table class="stats-table" style="margin-top: 15px;">
                                            <tr>
                                                <td>Last Calculation Time:</td>
                                                <td><asp:Label ID="lblLastCalculationTime" runat="server" Text="N/A"></asp:Label></td>
                                            </tr>
                                            <tr>
                                                <td>Session Duration:</td>
                                                <td><asp:Label ID="lblSessionDuration" runat="server" Text="N/A"></asp:Label></td>
                                            </tr>
                                        </table>
                                    </div>
                                </div>
                            </td>
                            <td valign="top">
                                <div class="card">
                                    <div class="card-header">
                                        <h3 class="card-title">How Risk Assessment Works</h3>
                                    </div>
                                    <div class="card-body">
                                        <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                            <strong>Simple concept:</strong> This tool calculates the worst-case loss you could expect on an investment over a specific time period, with a chosen level of confidence.
                                        </p>

                                        <p style="margin-bottom: 12px; color: #666666; line-height: 1.4;">
                                            <strong>Example:</strong> A 95% confidence, 1-day risk assessment of $10,000 might show "-$500", meaning there's only a 5% chance you'll lose more than $500 in one day.
                                        </p>

                                        <ul style="margin: 0; padding-left: 15px; line-height: 1.6;">
                                            <li style="margin-bottom: 6px;"><strong>Historical Method:</strong> Based on how the investment actually performed in the past</li>
                                            <li style="margin-bottom: 6px;"><strong>Mathematical Model:</strong> Uses statistical assumptions about market behavior</li>
                                            <li style="margin-bottom: 6px;"><strong>95% Confidence:</strong> There's a 95% chance your loss will be less than calculated</li>
                                            <li style="margin-bottom: 6px;"><strong>Time Periods:</strong> Calculate risk for one day, one week, or one month</li>
                                        </ul>

                                        <div class="btn-group clearfix" style="margin-top: 15px;">
                                            <asp:HyperLink ID="lnkPortfolio" runat="server" NavigateUrl="~/Portfolio.aspx" CssClass="btn btn-primary">
                                                View Portfolio Holdings
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
                    <p>VaR Calculation Dashboard - Financial Risk Management System v2.1</p>
                </td>
            </tr>
        </table>
    </form>
</body>
</html>
